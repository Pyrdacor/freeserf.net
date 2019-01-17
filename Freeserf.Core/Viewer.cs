using System;
using System.Collections.Generic;
using System.Text;
using Freeserf.Event;

namespace Freeserf
{
    // TODO: spectators should be able to select the player they are watching

    public abstract class Viewer : GameManager.IHandler
    {
        public static Viewer CreateLocalPlayer(Render.IRenderView renderView, Viewer previousViewer, Gui gui)
        {
            return new LocalPlayerViewer(renderView, previousViewer, gui);
        }

        // Server must also receive events from clients (with the clients player index)
        // Server should have multiple interfaces
        public static Viewer CreateServerPlayer(Render.IRenderView renderView, Viewer previousViewer, Gui gui)
        {
            throw new NotSupportedException("Not supported yet.");
        }

        // Client must also receive events from server (with the other clients player index)
        public static Viewer CreateClientPlayer(Render.IRenderView renderView, Viewer previousViewer, Gui gui)
        {
            throw new NotSupportedException("Not supported yet.");
        }

        public static Viewer CreateLocalSpectator(Render.IRenderView renderView, Viewer previousViewer, Gui gui)
        {
            return new LocalSpectatorViewer(renderView, previousViewer, gui);
        }

        public static Viewer CreateRemoteSpectator(Render.IRenderView renderView, Viewer previousViewer, bool resticted, Gui gui)
        {
            throw new NotSupportedException("Not supported yet.");
        }

        public static Viewer Create(Type type, Render.IRenderView renderView, Viewer previousViewer, Gui gui)
        {
            switch (type)
            {
                default:
                case Type.LocalPlayer:
                    return CreateLocalPlayer(renderView, previousViewer, gui);
                case Type.LocalSpectator:
                    return CreateLocalSpectator(renderView, previousViewer, gui);
                case Type.Server:
                    return CreateServerPlayer(renderView, previousViewer, gui);
                case Type.Client:
                    return CreateClientPlayer(renderView, previousViewer, gui);
                case Type.RemoteSpectator:
                    return CreateRemoteSpectator(renderView, previousViewer, false, gui);
                case Type.RestrictedRemoteSpectator:
                    return CreateRemoteSpectator(renderView, previousViewer, true, gui);
            }
        }

        public enum Access
        {
            Player,
            Spectator,
            RestrictedSpectator
        }

        public enum Type
        {
            LocalPlayer,
            LocalSpectator,
            Server,
            Client,
            RemoteSpectator,
            RestrictedRemoteSpectator
        }

        protected bool initialized = false;

        public abstract Access AccessRights { get; }
        public abstract bool Ingame { get; }
        public Render.IRenderView RenderView { get; }
        internal abstract Interface MainInterface { get; }
        protected Gui Gui { get; }
        public Type ViewerType { get; }

        protected Viewer(Render.IRenderView renderView, Gui gui, Type type)
        {
            RenderView = renderView;
            Gui = gui;
            ViewerType = type;
        }

        public void ChangeTo(Type type)
        {
            if (ViewerType == type)
                return;

            Gui.SetViewer(Create(type, MainInterface.RenderView, this, Gui));
        }

        public abstract bool SendEvent(Event.EventArgs args);
        public abstract void DrawCursor(int x, int y);
        public abstract void Update();
        public abstract void Draw();
        public abstract void OnNewGame(Game game);
        public abstract void OnEndGame(Game game);

        public virtual void Init()
        {
            if (initialized)
                return;

            GameManager.Instance.AddHandler(this);

            initialized = true;
        }

        public virtual void Destroy()
        {
            if (!initialized)
                return;

            GameManager.Instance.DeleteHandler(this);

            initialized = false;
        }
    }

    internal class LocalPlayerViewer : LocalSpectatorViewer
    {
        public override Access AccessRights => Access.Player;

        protected LocalPlayerViewer(Render.IRenderView renderView, Viewer previousViewer, Gui gui, Type type)
            : base(renderView, previousViewer, gui, type)
        {

        }

        public LocalPlayerViewer(Render.IRenderView renderView, Viewer previousViewer, Gui gui)
            : this(renderView, previousViewer, gui, Type.LocalPlayer)
        {

        }
    }

    internal class LocalSpectatorViewer : Viewer
    {
        public override Access AccessRights => Access.Spectator;
        public override bool Ingame => MainInterface.Ingame;
        internal override Interface MainInterface { get; }

        protected LocalSpectatorViewer(Render.IRenderView renderView, Viewer previousViewer, Gui gui, Type type)
            : base(renderView, gui, type)
        {
            if (previousViewer == null)
            {
                Init();
                MainInterface = new Interface(renderView, this);
                MainInterface.OpenGameInit();
            }
            else
            {
                MainInterface = previousViewer.MainInterface;
                MainInterface.Viewer = this;
            }
        }

        public LocalSpectatorViewer(Render.IRenderView renderView, Viewer previousViewer, Gui gui)
            : this(renderView, previousViewer, gui, Type.LocalSpectator)
        {

        }

        public override void Update()
        {
            MainInterface.Update();
        }

        public override void Draw()
        {
            MainInterface.Draw();
        }

        public override void DrawCursor(int x, int y)
        {
            MainInterface.DrawCursor(x, y);
        }

        public override bool SendEvent(Event.EventArgs args)
        {
            if (!args.Done)
                args.Done = MainInterface.HandleEvent(args);

            return args.Done;
        }

        public override void OnNewGame(Game game)
        {
            var music = MainInterface.Audio?.GetMusicPlayer();

            if (music != null && music.Enabled)
                music.PlayTrack((int)Audio.TypeMidi.Track0);

            MainInterface.SetGame(game);
            MainInterface.SetPlayer(0);
        }

        public override void OnEndGame(Game game)
        {
            MainInterface.SetGame(null);

            if (!(this is LocalPlayerViewer))
                Gui.SetViewer(Viewer.CreateLocalPlayer(MainInterface.RenderView, this, Gui));
                
        }
    }

    internal class ServerViewer : LocalPlayerViewer
    {
        public ServerViewer(Render.IRenderView renderView, Viewer previousViewer, Gui gui)
            : base(renderView, previousViewer, gui, Type.Server)
        {

        }

        // TODO
    }

    internal class ClientViewer : RemoteSpectatorViewer
    {
        public override Access AccessRights => Access.Player;

        public ClientViewer(Render.IRenderView renderView, Viewer previousViewer, Gui gui)
            : base(renderView, previousViewer, gui, Type.Client)
        {

        }

        // TODO
    }

    internal class RemoteSpectatorViewer : Viewer
    {
        public override Access AccessRights => ViewerType == Type.RestrictedRemoteSpectator ? Access.RestrictedSpectator : Access.Spectator;
        public override bool Ingame => MainInterface.Ingame;
        internal override Interface MainInterface { get; }

        protected RemoteSpectatorViewer(Render.IRenderView renderView, Viewer previousViewer, Gui gui, Type type)
            : base(renderView, gui, type)
        {
            if (previousViewer == null)
            {
                Init();
                MainInterface = new RemoteInterface(renderView, this);
                MainInterface.OpenGameInit(); // TODO
            }
            else
            {
                MainInterface = previousViewer.MainInterface;
                MainInterface.Viewer = this;
            }
        }

        public RemoteSpectatorViewer(Render.IRenderView renderView, Viewer previousViewer, Gui gui, bool restricted)
            : this(renderView, previousViewer, gui, restricted ? Type.RestrictedRemoteSpectator : Type.RemoteSpectator)
        {

        }

        public override void Update()
        {
            var remoteInterface = MainInterface as RemoteInterface;

            remoteInterface.GetMapUpdate();
            remoteInterface.GetGameUpdate();

            if (AccessRights == Access.Spectator)
            {
                for (uint i = 0; i < remoteInterface.Game.GetPlayerCount(); ++i)
                    remoteInterface.GetPlayerUpdate(i);
            }
            else
            {
                remoteInterface.GetPlayerUpdate(remoteInterface.GetPlayer().Index);
            }

            remoteInterface.Update();
        }

        public override void Draw()
        {
            MainInterface.Draw();
        }

        public override void DrawCursor(int x, int y)
        {
            MainInterface.DrawCursor(x, y);
        }

        public override bool SendEvent(Event.EventArgs args)
        {
            if (!args.Done)
                args.Done = MainInterface.HandleEvent(args);

            return args.Done;
        }

        public override void OnNewGame(Game game)
        {
            var music = MainInterface.Audio?.GetMusicPlayer();

            if (music != null && music.Enabled)
                music.PlayTrack((int)Audio.TypeMidi.Track0);

            MainInterface.SetGame(game);
            MainInterface.SetPlayer(0);
        }

        public override void OnEndGame(Game game)
        {
            MainInterface.SetGame(null);

            Gui.SetViewer(Viewer.CreateLocalPlayer(MainInterface.RenderView, this, Gui));
        }
    }
}
