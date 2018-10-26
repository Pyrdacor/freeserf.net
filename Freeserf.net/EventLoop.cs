/*
 * EventLoop.cs - User and system events handling
 *
 * Copyright (C) 2012-2017  Jon Lund Steffensen <jonlst@gmail.com>
 * Copyright (C) 2018       Robert Schneckenhaus <robert.schneckenhaus@web.de>
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

namespace Freeserf.Event
{
    public enum Type
    {
        Click,
        DoubleClick,
        Drag,
        KeyPressed,
        Resize,
        Update,
        Draw
    }

    public enum Button
    {
        None = 0,
        Left,
        Middle,
        Right
    }

    public class EventArgs : System.EventArgs
    {
        bool done = false;

        public int X { get; private set; }
        public int Y { get; private set; }
        public int Dx { get; private set; }
        public int Dy { get; private set; }
        public Type Type { get; private set; }
        public Button Button { get; private set; }
        public object Object { get; private set; }
        public bool Done // Can not be reset
        {
            get => done;
            set
            {
                if (value)
                    done = true;
            }
        }

        internal EventArgs(Type type, int x, int y, int dx, int dy, Button button = Button.None, object obj = null)
        {
            Type = type;
            X = x;
            Y = y;
            Dx = dx;
            Dy = dy;
            Button = button;
            Object = obj;
        }
    }

    public delegate bool EventHandler(object sender, EventArgs args);

    public abstract class EventLoop
    {
        protected static EventLoop instance = null;

        public static EventLoop Instance => instance;

        public event EventHandler Click;
        public event EventHandler DoubleClick;
        public event EventHandler Drag;
        public event EventHandler KeyPress;
        public event EventHandler Resize;
        public event EventHandler Update;
        public event EventHandler Draw;

        bool RunHandler(EventHandler handler, EventArgs args)
        {
            handler?.Invoke(this, args);

            return args.Done;
        }

        protected bool NotifyClick(int x, int y, Button button)
        {
            return RunHandler(Click, new EventArgs(Type.Click, x, y, 0, 0, button));
        }

        protected bool NotifyDoubleClick(int x, int y, Button button)
        {
            return RunHandler(DoubleClick, new EventArgs(Type.DoubleClick, x, y, 0, 0, button));
        }

        protected bool NotifyDrag(int x, int y, int dx, int dy, Button button)
        {
            return RunHandler(Drag, new EventArgs(Type.Drag, x, y, dx, dy, button));
        }

        protected bool NotifyKeyPressed(byte key, byte modifier)
        {
            return RunHandler(KeyPress, new EventArgs(Type.KeyPressed, 0, 0, key, modifier));
        }

        protected bool NotifyResize(uint width, uint height)
        {
            return RunHandler(Resize, new EventArgs(Type.Resize, 0, 0, (int)width, (int)height));
        }

        protected bool NotifyUpdate()
        {
            return RunHandler(Update, new EventArgs(Type.Update, 0, 0, 0, 0));
        }

        protected bool NotifyDraw(Frame frame)
        {
            return RunHandler(Draw, new EventArgs(Type.Draw, 0, 0, 0, 0, Button.None, frame));
        }
    }
}
