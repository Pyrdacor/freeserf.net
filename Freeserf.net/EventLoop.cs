using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
