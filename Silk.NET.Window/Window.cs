using Silk.NET.Input;
using Silk.NET.Input.Common;
using Silk.NET.Windowing.Common;
using System;
using System.Drawing;
using System.Linq;

namespace Silk.NET.Window
{
    public class Window
    {
        private static WindowOptions DefaultOptions = new WindowOptions
        (
            true,
            true,
            new Point(-1, -1),
            new Size(1024, 768),
            60.0,
            60.0,
            GraphicsAPI.Default,
            "",
            WindowState.Normal,
            WindowBorder.Resizable,
            VSyncMode.Adaptive,
            10,
            false,
            new VideoMode(),
            24
        );

        readonly IWindow window = null;
        IMouse mouse = null;
        bool cursorVisible = true;

        private static WindowOptions CreateOptions(string title, Point position, Size size)
        {
            var options = DefaultOptions;

            options.Title = title;
            options.Size = size;
            options.Position = position;

            return options;
        }

        public Window()
        {
            window = Windowing.Window.Create(DefaultOptions);
            InitEvents();
        }

        public Window(string title, Point position, Size size)
            : this(CreateOptions(title, position, size))
        {

        }

        public Window(string title, Size size)
            : this(title, Point.Empty, size)
        {

        }

        public Window(WindowOptions options)
        {
            window = Windowing.Window.Create(options);
            InitEvents();
        }

        public Point Position
        {
            get => window.Position;
            set => window.Position = value;
        }

        public Size Size
        {
            get => window.Size;
            set => window.Size = value;
        }

        public WindowState WindowState
        {
            get => window.WindowState;
            set
            {
                window.WindowState = value;

                if (mouse != null && !CursorVisible)
                {
                    mouse.Cursor.CursorMode = WindowState == WindowState.Fullscreen ? CursorMode.Disabled : CursorMode.Hidden;
                }
            }
        }
        public CursorMode CursorMode
        {
            get => mouse == null ? CursorMode.Disabled : mouse.Cursor.CursorMode;
            set
            {
                if (mouse?.Cursor != null)
                    mouse.Cursor.CursorMode = value;
            }
        }

        public bool CursorVisible
        {
            get => mouse == null || cursorVisible;
            set
            {
                if (mouse == null)
                {
                    cursorVisible = false;
                    return;
                }

                if (cursorVisible == value)
                    return;

                cursorVisible = value;

                if (cursorVisible)
                {
                    mouse.Cursor.CursorMode = CursorMode.Normal;
                }
                else
                {
                    mouse.Cursor.CursorMode = WindowState == WindowState.Fullscreen ? CursorMode.Disabled : CursorMode.Hidden;
                }
            }
        }

        public PointF CursorPosition
        {
            get => mouse == null ? PointF.Empty : mouse.Position;
            set
            {
                if (mouse != null)
                    mouse.Position = value;
            }
        }

        public bool IsMouseButtonPressed(MouseButton button) => mouse.IsButtonPressed(button);

        public Rectangle ClientRectangle => new Rectangle(Position, Size);

        public bool Initialized { get; private set; } = false;

        public event Action Load
        {
            add { window.Load += value; }
            remove { window.Load -= value; }
        }

        public event Action<double> Update
        {
            add { window.Update += value; }
            remove { window.Update -= value; }
        }

        public event Action<double> Render
        {
            add { window.Render += value; }
            remove { window.Render -= value; }
        }

        public event Action Closing
        {
            add { window.Closing += value; }
            remove { window.Closing -= value; }
        }

        public event Action<Size> Resize
        {
            add { window.Resize += value; }
            remove { window.Resize -= value; }
        }

        public event Action<WindowState> StateChanged
        {
            add { window.StateChanged += value; }
            remove { window.StateChanged -= value; }
        }

        public void Run()
        {
            window?.Run();
        }

        public void Close()
        {
            window?.Close();
        }

        public void MakeCurrent()
        {
            try
            {
                window?.MakeCurrent();
            }
            catch
            {
                Console.WriteLine("Warning: MakeCurrent failed.");
            }
        }

        public void SwapBuffers()
        {
            window?.SwapBuffers();
        }

        private void InitEvents()
        {
            Load += () =>
            {
                var input = window.CreateInput();
                InitKeyboardEvents(input);
                InitMouseEvents(input);
                Initialized = true;
            };
        }


        #region Keyboard Events

        private void InitKeyboardEvents(IInputContext input)
        {
            var keyboard = input.Keyboards.FirstOrDefault(k => k.IsConnected);

            if (keyboard != null)
            {
                keyboard.KeyDown += OnKeyDown;
                keyboard.KeyUp += OnKeyUp;
                keyboard.KeyChar += OnKeyChar;
            }
        }

        private const int KeyModifierMask = 0x07;

        private void OnKeyDown(IKeyboard keyboard, Key key, int modifiers)
        {
            OnKeyDown(key, (KeyModifiers)(modifiers & KeyModifierMask));
        }

        private void OnKeyUp(IKeyboard keyboard, Key key, int modifiers)
        {
            OnKeyUp(key, (KeyModifiers)(modifiers & KeyModifierMask));
        }

        private void OnKeyChar(IKeyboard keyboard, char character)
        {
            OnKeyChar(character);
        }

        protected virtual void OnKeyDown(Key key, KeyModifiers modifiers)
        {
            KeyDown?.Invoke(key, modifiers);
        }

        protected virtual void OnKeyUp(Key key, KeyModifiers modifiers)
        {
            KeyUp?.Invoke(key, modifiers);
        }

        protected virtual void OnKeyChar(char character)
        {
            KeyChar?.Invoke(character);
        }

        /// <summary>
        /// Called when a key is pressed down.
        /// 
        /// First argument: Key code
        /// Second argument: Key modifiers
        /// </summary>
        public event Action<Key, KeyModifiers> KeyDown;

        /// <summary>
        /// Called when a key is released.
        /// 
        /// First argument: Key code
        /// Second argument: Key modifiers
        /// </summary>
        public event Action<Key, KeyModifiers> KeyUp;

        /// <summary>
        /// Called when a printable character is entered.
        /// 
        /// First argument: Character
        /// </summary>
        public event Action<char> KeyChar;

        #endregion


        #region Mouse Events

        private void InitMouseEvents(IInputContext input)
        {
            mouse = input.Mice.FirstOrDefault(m => m.IsConnected);

            if (mouse != null)
            {
                lastMousePosition = mouse.Position;

                mouse.DoubleClickTime = DoubleClickTime;
                mouse.DoubleClickRange = DoubleClickRange;

                mouse.MouseDown += OnMouseDown;
                mouse.MouseUp += OnMouseUp;
                mouse.Click += OnMouseClick;
                mouse.DoubleClick += OnMouseDoubleClick;
                mouse.MouseMove += OnMouseMove;
                mouse.Scroll += OnMouseScroll;
            }
        }

        private int doubleClickRange = 4;
        private int doubleClickTime = 200;
        private PointF lastMousePosition = PointF.Empty;

        /// <summary>
        /// Maximum time in milliseconds for which two subsequent
        /// mouse clicks count as a double click.
        /// 
        /// Default: 200
        /// </summary>
        public int DoubleClickTime
        {
            get => doubleClickTime;
            set
            {
                doubleClickTime = value;

                if (mouse != null)
                    mouse.DoubleClickTime = value;
            }
        }

        /// <summary>
        /// Maximum size around the first click for the second click
        /// to count as a double click.
        /// 
        /// Default: 4x4 (2 pixel in each direction)
        /// </summary>
        public int DoubleClickRange
        {
            get => doubleClickRange;
            set
            {
                doubleClickRange = value;

                if (mouse != null)
                    mouse.DoubleClickRange = value;
            }
        }

        private static MouseButtons GetMouseButtons(IMouse mouse)
        {
            var buttons = MouseButtons.None;

            if (mouse.IsButtonPressed(MouseButton.Left))
                buttons |= MouseButtons.Left;
            if (mouse.IsButtonPressed(MouseButton.Right))
                buttons |= MouseButtons.Right;
            if (mouse.IsButtonPressed(MouseButton.Middle))
                buttons |= MouseButtons.Middle;

            return buttons;
        }

        private static MouseButtons ConvertMouseButton(MouseButton button)
        {
            return (MouseButtons)(1 << (int)button);
        }

        private static Point ConvertMousePosition(PointF position)
        {
            return new Point((int)Math.Round(position.X), (int)Math.Round(position.Y));
        }

        private void OnMouseDown(IMouse mouse, MouseButton mouseButton)
        {
            OnMouseDown(ConvertMousePosition(mouse.Position), ConvertMouseButton(mouseButton));
        }

        private void OnMouseUp(IMouse mouse, MouseButton mouseButton)
        {
            OnMouseUp(ConvertMousePosition(mouse.Position), ConvertMouseButton(mouseButton));
        }

        private void OnMouseClick(IMouse mouse, MouseButton mouseButton)
        {
            OnClick(ConvertMousePosition(mouse.Position), ConvertMouseButton(mouseButton));
        }

        private void OnMouseDoubleClick(IMouse mouse, MouseButton mouseButton)
        {
            OnDoubleClick(ConvertMousePosition(mouse.Position), ConvertMouseButton(mouseButton));
        }

        private void OnMouseMove(IMouse mouse, PointF position)
        {
            var roundedPosition = ConvertMousePosition(position);
            var buttons = GetMouseButtons(mouse);
            var delta = new PointF(position.X - lastMousePosition.X, position.Y - lastMousePosition.Y);
            var roundedLastPosition = ConvertMousePosition(lastMousePosition);
            var roundedDelta = new Point(roundedPosition.X - roundedLastPosition.X, roundedPosition.Y - roundedLastPosition.Y);

            OnMouseMove(roundedPosition, buttons);
            OnMouseMovePrecise(mouse.Position, buttons);
            OnMouseMoveDelta(roundedPosition, buttons, roundedDelta);
            OnMouseMoveDeltaPrecise(mouse.Position, buttons, delta);

            var clientRectangle = ClientRectangle;

            bool nowInside = clientRectangle.Contains(roundedPosition);
            bool lastInside = clientRectangle.Contains(roundedLastPosition);

            if (nowInside && !lastInside)
                OnMouseEnter();
            else if (lastInside && !nowInside)
                OnMouseLeave();

            lastMousePosition = position;
        }

        private void OnMouseScroll(IMouse mouse, ScrollWheel wheel)
        {
            // we can not determine which wheel was scrolled
            // therefore we explicitely check the value of the first wheel
            if (mouse.ScrollWheels.Count > 0)
            {
                float delta = mouse.ScrollWheels.First().Y;

                if (delta != 0.0f)
                {
                    OnMouseWheel(ConvertMousePosition(mouse.Position), delta);
                }
            }
        }

        protected virtual void OnMouseDown(Point position, MouseButtons button)
        {
            MouseDown?.Invoke(position, button);
        }

        protected virtual void OnMouseUp(Point position, MouseButtons button)
        {
            MouseUp?.Invoke(position, button);
        }

        protected virtual void OnMouseEnter()
        {
            MouseEnter?.Invoke();
        }

        protected virtual void OnMouseLeave()
        {
            MouseLeave?.Invoke();
        }

        protected virtual void OnMouseMove(Point position, MouseButtons buttons)
        {
            MouseMove?.Invoke(position, buttons);
        }

        protected virtual void OnMouseMovePrecise(PointF position, MouseButtons buttons)
        {
            MouseMovePrecise?.Invoke(position, buttons);
        }

        protected virtual void OnMouseMoveDelta(Point position, MouseButtons buttons, Point delta)
        {
            MouseMoveDelta?.Invoke(position, buttons, delta);
        }

        protected virtual void OnMouseMoveDeltaPrecise(PointF position, MouseButtons buttons, PointF delta)
        {
            MouseMoveDeltaPrecise?.Invoke(position, buttons, delta);
        }

        protected virtual void OnClick(Point position, MouseButtons button)
        {
            Click?.Invoke(position, button);
        }

        protected virtual void OnDoubleClick(Point position, MouseButtons button)
        {
            DoubleClick?.Invoke(position, button);
        }

        protected virtual void OnMouseWheel(Point position, float delta)
        {
            MouseWheel?.Invoke(position, delta);
        }

        /// <summary>
        /// Called when a mouse button is pressed down.
        /// 
        /// First argument: Mouse position
        /// Second argument: Pressed button
        /// </summary>
        public event Action<Point, MouseButtons> MouseDown;

        /// <summary>
        /// Called when a mouse button is released.
        /// 
        /// First argument: Mouse position
        /// Second argument: Pressed button
        /// </summary>
        public event Action<Point, MouseButtons> MouseUp;

        /// <summary>
        /// Called when the mouse is entering the window.
        /// </summary>
        public event Action MouseEnter;

        /// <summary>
        /// Called when the mouse is leaving the window.
        /// </summary>
        public event Action MouseLeave;

        /// <summary>
        /// Called when the mouse is moved.
        /// 
        /// First argument: Mouse position
        /// Second argument: Pressed button
        /// </summary>
        public event Action<Point, MouseButtons> MouseMove;

        /// <summary>
        /// Called when the mouse is moved.
        /// 
        /// First argument: Mouse position
        /// Second argument: Pressed button
        /// </summary>
        public event Action<PointF, MouseButtons> MouseMovePrecise;

        /// <summary>
        /// Called when the mouse is moved.
        /// 
        /// First argument: Mouse position
        /// Second argument: Pressed button
        /// Third argument: Position delta
        /// </summary>
        public event Action<Point, MouseButtons, Point> MouseMoveDelta;

        /// <summary>
        /// Called when the mouse is moved.
        /// 
        /// First argument: Mouse position
        /// Second argument: Pressed button
        /// Third argument: Position delta
        /// </summary>
        public event Action<PointF, MouseButtons, PointF> MouseMoveDeltaPrecise;

        /// <summary>
        /// Called when a mouse click is performed.
        /// 
        /// First argument: Mouse position
        /// Second argument: Pressed button
        /// </summary>
        public event Action<Point, MouseButtons> Click;

        /// <summary>
        /// Called when a mouse double click is performed.
        /// 
        /// First argument: Mouse position
        /// Second argument: Pressed button
        /// </summary>
        public event Action<Point, MouseButtons> DoubleClick;

        /// <summary>
        /// Called when the main mouse wheel is scrolled.
        /// 
        /// First argument: Mouse position
        /// Second argument: Scroll delta
        /// </summary>
        public event Action<Point, float> MouseWheel;

        #endregion
    }
}
