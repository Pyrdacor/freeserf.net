/*
 * Event.cs - User and system events handling
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
        SpecialClick, // Left + right button
        Drag,
        KeyPressed
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
        public bool Done // Can not be reset
        {
            get => done;
            set
            {
                if (value)
                    done = true;
            }
        }

        public EventArgs(Type type, int x, int y, int dx, int dy, Button button = Button.None)
        {
            Type = type;
            X = x;
            Y = y;
            Dx = dx;
            Dy = dy;
            Button = button;
        }
    }

    public delegate bool EventHandler(object sender, EventArgs args);

    public interface IEventHandlers
    {
        event EventHandler Click;
        event EventHandler DoubleClick;
        event EventHandler SpecialClick;
        event EventHandler Drag;
        event EventHandler KeyPress;
    }
}
