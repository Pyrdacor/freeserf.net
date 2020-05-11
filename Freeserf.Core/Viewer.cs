/*
 * Viewer.cs - Viewers (local/remote, player/spectator)
 *
 * Copyright (C) 2018-2019  Robert Schneckenhaus <robert.schneckenhaus@web.de>
 *
 * This file is part of freeserf.net. freeserf.net is based on freeserf.
 *
 * freeserf.net is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * freeserf.net is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with freeserf.net. If not, see <http://www.gnu.org/licenses/>.
 */

using Freeserf.UI;
using System;

namespace Freeserf
{
    // TODO: spectators should be able to select the player they are watching

    public abstract class Viewer : GameManager.IHandler
    {
        public static Viewer CreateLocalPlayer(Render.IRenderView renderView, Audio.IAudioInterface audioInterface, Viewer previousViewer, Gui gui)
        {
            return new LocalPlayerViewer(renderView, audioInterface, previousViewer, gui);
        }

        // Server must also receive events from clients (with the clients player index)
        // Server should have multiple interfaces
        public static Viewer CreateServerPlayer(Render.IRenderView renderView, Audio.IAudioInterface audioInterface, Viewer previousViewer, Gui gui)
        {
            return new ServerViewer(renderView, audioInterface, previousViewer, gui);
        }

        // Client must also receive events from server (with the other clients player index)
        public static Viewer CreateClientPlayer(Render.IRenderView renderView, Audio.IAudioInterface audioInterface, Viewer previousViewer, Gui gui)
        {
            return new ClientViewer(renderView, audioInterface, previousViewer, gui);
        }

        public static Viewer CreateLocalSpectator(Render.IRenderView renderView, Audio.IAudioInterface audioInterface, Viewer previousViewer, Gui gui)
        {
            return new LocalSpectatorViewer(renderView, audioInterface, previousViewer, gui);
        }

        public static Viewer CreateRemoteSpectator(Render.IRenderView renderView, Audio.IAudioInterface audioInterface, Viewer previousViewer, bool resticted, Gui gui)
        {
            throw new NotSupportedException("Not supported yet.");
        }

        public static Viewer Create(Type type, Render.IRenderView renderView, Audio.IAudioInterface audioInterface, Viewer previousViewer, Gui gui)
        {
            switch (type)
            {
                default:
                case Type.LocalPlayer:
                    return CreateLocalPlayer(renderView, audioInterface, previousViewer, gui);
                case Type.LocalSpectator:
                    return CreateLocalSpectator(renderView, audioInterface, previousViewer, gui);
                case Type.Server:
                    return CreateServerPlayer(renderView, audioInterface, previousViewer, gui);
                case Type.Client:
                    return CreateClientPlayer(renderView, audioInterface, previousViewer, gui);
                case Type.RemoteSpectator:
                    return CreateRemoteSpectator(renderView, audioInterface, previousViewer, false, gui);
                case Type.RestrictedRemoteSpectator:
                    return CreateRemoteSpectator(renderView, audioInterface, previousViewer, true, gui);
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

        public Viewer ChangeTo(Type type)
        {
            if (ViewerType == type)
                return this;

            var viewer = Create(type, MainInterface.RenderView, MainInterface.AudioInterface, this, Gui);

            Gui.SetViewer(viewer);

            return viewer;
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
            MainInterface.Destroy();

            initialized = false;
        }
    }

    internal class LocalPlayerViewer : LocalSpectatorViewer
    {
        public override Access AccessRights => Access.Player;
        internal override Interface MainInterface { get; }

        protected LocalPlayerViewer(Render.IRenderView renderView, Viewer previousViewer, Gui gui, Type type)
            : base(renderView, previousViewer, gui, type)
        {

        }

        public LocalPlayerViewer(Render.IRenderView renderView, Audio.IAudioInterface audioInterface, Viewer previousViewer, Gui gui)
            : this(renderView, previousViewer, gui, Type.LocalPlayer)
        {
            if (previousViewer == null)
            {
                Init();
                MainInterface = new Interface(renderView, audioInterface, this);
                MainInterface.OpenGameInit();
            }
            else
            {
                if (previousViewer.MainInterface.GetType() != typeof(Interface))
                    MainInterface = new Interface(renderView, audioInterface, this);
                else
                    MainInterface = previousViewer.MainInterface;

                MainInterface.Viewer = this;
            }
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

        }

        public LocalSpectatorViewer(Render.IRenderView renderView, Audio.IAudioInterface audioInterface, Viewer previousViewer, Gui gui)
            : this(renderView, previousViewer, gui, Type.LocalSpectator)
        {
            if (previousViewer == null)
            {
                Init();
                MainInterface = new Interface(renderView, audioInterface, this);
                MainInterface.OpenGameInit();
            }
            else
            {
                if (previousViewer.MainInterface.GetType() != typeof(Interface))
                    MainInterface = new Interface(renderView, audioInterface, this);
                else
                    MainInterface = previousViewer.MainInterface;

                MainInterface.Viewer = this;
            }
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

            if (music != null)
                music.PlayTrack((int)Audio.Audio.TypeMidi.Track0);

            MainInterface.SetGame(game);
            MainInterface.SetPlayer(0);
        }

        public override void OnEndGame(Game game)
        {
            MainInterface.SetGame(null);

            if (!(this is LocalPlayerViewer))
                Gui.SetViewer(Viewer.CreateLocalPlayer(MainInterface.RenderView, MainInterface.AudioInterface, this, Gui));

        }
    }

    internal class ServerViewer : LocalPlayerViewer
    {
        readonly Network.ILocalServer server = null;
        internal override Interface MainInterface { get; }

        public ServerViewer(Render.IRenderView renderView, Audio.IAudioInterface audioInterface, Viewer previousViewer, Gui gui)
            : base(renderView, previousViewer, gui, Type.Server)
        {
            // Note: It is ok if the only clients are spectators, but running a server without any connected client makes no sense.
            // Note: All clients must be setup at game start. Clients can not join during the game.
            // Note: There may be more than 3 clients because of spectators!
            server = previousViewer.MainInterface.Server;

            Init();
            MainInterface = new ServerInterface(renderView, audioInterface, this, server);
        }

        public override void OnNewGame(Game game)
        {
            base.OnNewGame(game);

            // TODO: neccessary?
            foreach (var client in server.Clients)
            {
                client.SendGameStateUpdate(game);
                client.SendMapStateUpdate(game.Map);

                for (uint i = 0; i < game.PlayerCount; ++i)
                    client.SendPlayerStateUpdate(game.GetPlayer(i));
            }
        }

        public override void OnEndGame(Game game)
        {
            base.OnEndGame(game);

            foreach (var client in server.Clients)
            {
                client.SendDisconnect();
            }

            server.Close();
        }

        public override void Update()
        {
            foreach (var client in server.Clients)
            {
                client.SendHeartbeat();
            }

            base.Update();
        }
    }

    internal class ClientViewer : RemoteSpectatorViewer
    {
        readonly Network.ILocalClient client = null;
        public override Access AccessRights => Access.Player;

        public ClientViewer(Render.IRenderView renderView, Audio.IAudioInterface audioInterface, Viewer previousViewer, Gui gui)
            : base(renderView, audioInterface, previousViewer, gui, Type.Client)
        {
            client = previousViewer.MainInterface.Client;
        }

        public override void OnEndGame(Game game)
        {
            base.OnEndGame(game);

            client.SendDisconnect();
        }

        public override void Update()
        {
            client.SendHeartbeat();

            base.Update();
        }
    }

    internal class RemoteSpectatorViewer : Viewer
    {
        public override Access AccessRights => ViewerType == Type.RestrictedRemoteSpectator ? Access.RestrictedSpectator : Access.Spectator;
        public override bool Ingame => MainInterface.Ingame;
        internal override Interface MainInterface { get; }

        protected RemoteSpectatorViewer(Render.IRenderView renderView, Audio.IAudioInterface audioInterface, Viewer previousViewer, Gui gui, Type type)
            : base(renderView, gui, type)
        {
            Init();
            MainInterface = new RemoteInterface(renderView, audioInterface, this);
        }

        public RemoteSpectatorViewer(Render.IRenderView renderView, Audio.IAudioInterface audioInterface, Viewer previousViewer, Gui gui, bool restricted)
            : this(renderView, audioInterface, previousViewer, gui, restricted ? Type.RestrictedRemoteSpectator : Type.RemoteSpectator)
        {

        }

        public override void Update()
        {
            var remoteInterface = MainInterface as RemoteInterface;

            if (remoteInterface.Game != null && remoteInterface.Player != null)
            {
                remoteInterface.GetMapUpdate();
                remoteInterface.GetGameUpdate();

                if (AccessRights == Access.Spectator)
                {
                    for (uint i = 0; i < remoteInterface.Game.PlayerCount; ++i)
                        remoteInterface.GetPlayerUpdate(i);
                }
                else
                {
                    remoteInterface.GetPlayerUpdate(remoteInterface.Player.Index);
                }

                remoteInterface.Update();
            }
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

            if (music != null)
                music.PlayTrack((int)Audio.Audio.TypeMidi.Track0);

            MainInterface.SetGame(game);
            MainInterface.SetPlayer(0);
        }

        public override void OnEndGame(Game game)
        {
            MainInterface.SetGame(null);

            Gui.SetViewer(Viewer.CreateLocalPlayer(MainInterface.RenderView, MainInterface.AudioInterface, this, Gui));
        }
    }
}
