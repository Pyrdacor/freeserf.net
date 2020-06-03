/*
 * Viewer.cs - Abstract viewer
 *
 * Copyright (C) 2018-2020  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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

        public Viewer ActiveViewer => Gui.ActiveViewer;

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

        public virtual void Destroy(bool destroyInterface)
        {
            if (!initialized)
                return;

            GameManager.Instance.DeleteHandler(this);

            if (destroyInterface)
                MainInterface.Destroy();

            initialized = false;
        }
    }
}
