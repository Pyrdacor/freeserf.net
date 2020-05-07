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
        KeyPressed,
        SystemKeyPressed,
        StopDrag
    }

    public enum Button
    {
        None = 0,
        Left,
        Middle,
        Right
    }

    public enum SystemKey
    {
        Unknown = -1,
        Escape,
        PageUp,
        PageDown,
        Up,
        Down,
        Left,
        Right
    }

    public static class SystemKeys
    {
        public const char Backspace = (char)8;
        public const char Delete = (char)24;
        public const char Return = '\n';
        public const char Tab = '\t';
    }

    public class EventArgs : System.EventArgs
    {
        bool done = false;

        public int X { get; internal set; }
        public int Y { get; internal set; }
        public int Dx { get; internal set; }
        public int Dy { get; internal set; }
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
        public EventArgs UntransformedArgs { get; private set; }

        public EventArgs(Type type, int x, int y, int dx, int dy, Button button = Button.None, EventArgs untransformed = null)
        {
            Type = type;
            X = x;
            Y = y;
            Dx = dx;
            Dy = dy;
            Button = button;
            UntransformedArgs = untransformed;
        }

        public static EventArgs Transform(EventArgs args, int transformedX, int transformedY, int transformedDx, int transformedDy)
        {
            return new EventArgs(args.Type, transformedX, transformedY, transformedDx, transformedDy, args.Button, args);
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
        event EventHandler SystemKeyPress;
        event EventHandler StopDrag;
    }
}
