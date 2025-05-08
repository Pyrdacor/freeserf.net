using Silk.NET.Core.Contexts;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using System;
using System.Linq;
using System.Numerics;

namespace Silk.NET.Window
{
    public class Window : IGLContextSource
    {
        private static WindowOptions DefaultOptions = new
        (
            true,
            new Vector2D<int>(-1, -1),
            new Vector2D<int>(1024, 768),
            60.0,
            60.0,
            GraphicsAPI.Default,
            "",
            WindowState.Normal,
            WindowBorder.Resizable,
            true,
            false,
            new VideoMode(),
            24
        );

        readonly IWindow window = null;
        IMouse mouse = null;
        bool cursorVisible = true;

        private static WindowOptions CreateOptions(string title, Vector2D<int> position, Vector2D<int> size)
        {
            var options = DefaultOptions;

            options.Title = title;
            options.Size = size;
            options.Position = position;

            return options;
        }

        public Window()
            : this(DefaultOptions)
        {

        }

        public Window(string title, Vector2D<int> position, Vector2D<int> size)
            : this(CreateOptions(title, position, size))
        {

        }

        public Window(string title, Vector2D<int> size)
            : this(title, Vector2D<int>.Zero, size)
        {

        }

        public Window(WindowOptions options)
        {
            window = Windowing.Window.Create(options);
            InitEvents();
        }

        public Vector2D<int> Position
        {
            get => window.Position;
            set => window.Position = value;
        }

        public Vector2D<int> Size
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

        public Vector2 CursorPosition
        {
            get => mouse == null ? Vector2.Zero : mouse.Position;
            set
            {
                if (mouse != null)
                    mouse.Position = value;
            }
        }

        public bool IsMouseButtonPressed(MouseButton button) => mouse.IsButtonPressed(button);

        public Rectangle<int> ClientRectangle => new(Position, Size);

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

        public event Action<Vector2D<int>> Resize
        {
            add { window.Resize += value; }
            remove { window.Resize -= value; }
        }

        public event Action<WindowState> StateChanged
        {
            add { window.StateChanged += value; }
            remove { window.StateChanged -= value; }
        }

        public void Run() => window?.Run();

        public void Close() => window?.Close();

        public void MakeCurrent() => window?.MakeCurrent();

        public void SwapBuffers() => window?.SwapBuffers();

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

        private static KeyModifiers GetModifiers(IKeyboard keyboard)
        {
            var modifiers = (KeyModifiers)0;

            if (keyboard.IsKeyPressed(Key.ShiftLeft) || keyboard.IsKeyPressed(Key.ShiftRight))
                modifiers |= KeyModifiers.Shift;
            if (keyboard.IsKeyPressed(Key.ControlLeft) || keyboard.IsKeyPressed(Key.ControlRight))
                modifiers |= KeyModifiers.Control;
            if (keyboard.IsKeyPressed(Key.AltLeft) || keyboard.IsKeyPressed(Key.AltRight))
                modifiers |= KeyModifiers.Alt;

            return modifiers;
        }

        private void OnKeyDown(IKeyboard keyboard, Key key, int code)
        {
            if (key == Key.Unknown)
                return;

            OnKeyDown(key, GetModifiers(keyboard));
        }

        private void OnKeyUp(IKeyboard keyboard, Key key, int code)
        {
            if (key == Key.Unknown)
                return;

            OnKeyUp(key, GetModifiers(keyboard));
        }

        private void OnKeyChar(IKeyboard keyboard, char character)
        {
            OnKeyChar(character, GetModifiers(keyboard));
        }

        protected virtual void OnKeyDown(Key key, KeyModifiers modifiers)
        {
            KeyDown?.Invoke(key, modifiers);
        }

        protected virtual void OnKeyUp(Key key, KeyModifiers modifiers)
        {
            KeyUp?.Invoke(key, modifiers);
        }

        protected virtual void OnKeyChar(char character, KeyModifiers modifiers)
        {
            KeyChar?.Invoke(character, modifiers);
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
        /// Second argument: Key modifiers
        /// </summary>
        public event Action<char, KeyModifiers> KeyChar;

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
        private Vector2 lastMousePosition = Vector2.Zero;

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

        public IGLContext GLContext => window.GLContext;

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

        private static Vector2D<int> ConvertMousePosition(Vector2 position)
        {
            return new Vector2D<int>((int)Math.Round(position.X), (int)Math.Round(position.Y));
        }

        private void OnMouseDown(IMouse mouse, MouseButton mouseButton)
        {
            OnMouseDown(ConvertMousePosition(mouse.Position), ConvertMouseButton(mouseButton));
        }

        private void OnMouseUp(IMouse mouse, MouseButton mouseButton)
        {
            OnMouseUp(ConvertMousePosition(mouse.Position), ConvertMouseButton(mouseButton));
        }

        private void OnMouseClick(IMouse mouse, MouseButton mouseButton, Vector2 position)
        {
            OnClick(ConvertMousePosition(position), ConvertMouseButton(mouseButton));
        }

        private void OnMouseDoubleClick(IMouse mouse, MouseButton mouseButton, Vector2 position)
        {
            OnDoubleClick(ConvertMousePosition(position), ConvertMouseButton(mouseButton));
        }

        private void OnMouseMove(IMouse mouse, Vector2 position)
        {
            var roundedPosition = ConvertMousePosition(position);
            var buttons = GetMouseButtons(mouse);
            var delta = new Vector2(position.X - lastMousePosition.X, position.Y - lastMousePosition.Y);
            var roundedLastPosition = ConvertMousePosition(lastMousePosition);
            var roundedDelta = new Vector2D<int>(roundedPosition.X - roundedLastPosition.X, roundedPosition.Y - roundedLastPosition.Y);

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

        protected virtual void OnMouseDown(Vector2D<int> position, MouseButtons button)
        {
            MouseDown?.Invoke(position, button);
        }

        protected virtual void OnMouseUp(Vector2D<int> position, MouseButtons button)
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

        protected virtual void OnMouseMove(Vector2D<int> position, MouseButtons buttons)
        {
            MouseMove?.Invoke(position, buttons);
        }

        protected virtual void OnMouseMovePrecise(Vector2 position, MouseButtons buttons)
        {
            MouseMovePrecise?.Invoke(position, buttons);
        }

        protected virtual void OnMouseMoveDelta(Vector2D<int> position, MouseButtons buttons, Vector2D<int> delta)
        {
            MouseMoveDelta?.Invoke(position, buttons, delta);
        }

        protected virtual void OnMouseMoveDeltaPrecise(Vector2 position, MouseButtons buttons, Vector2 delta)
        {
            MouseMoveDeltaPrecise?.Invoke(position, buttons, delta);
        }

        protected virtual void OnClick(Vector2D<int> position, MouseButtons button)
        {
            Click?.Invoke(position, button);
        }

        protected virtual void OnDoubleClick(Vector2D<int> position, MouseButtons button)
        {
            DoubleClick?.Invoke(position, button);
        }

        protected virtual void OnMouseWheel(Vector2D<int> position, float delta)
        {
            MouseWheel?.Invoke(position, delta);
        }

        /// <summary>
        /// Called when a mouse button is pressed down.
        /// 
        /// First argument: Mouse position
        /// Second argument: Pressed button
        /// </summary>
        public event Action<Vector2D<int>, MouseButtons> MouseDown;

        /// <summary>
        /// Called when a mouse button is released.
        /// 
        /// First argument: Mouse position
        /// Second argument: Pressed button
        /// </summary>
        public event Action<Vector2D<int>, MouseButtons> MouseUp;

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
        public event Action<Vector2D<int>, MouseButtons> MouseMove;

        /// <summary>
        /// Called when the mouse is moved.
        /// 
        /// First argument: Mouse position
        /// Second argument: Pressed button
        /// </summary>
        public event Action<Vector2, MouseButtons> MouseMovePrecise;

        /// <summary>
        /// Called when the mouse is moved.
        /// 
        /// First argument: Mouse position
        /// Second argument: Pressed button
        /// Third argument: Position delta
        /// </summary>
        public event Action<Vector2D<int>, MouseButtons, Vector2D<int>> MouseMoveDelta;

        /// <summary>
        /// Called when the mouse is moved.
        /// 
        /// First argument: Mouse position
        /// Second argument: Pressed button
        /// Third argument: Position delta
        /// </summary>
        public event Action<Vector2, MouseButtons, Vector2> MouseMoveDeltaPrecise;

        /// <summary>
        /// Called when a mouse click is performed.
        /// 
        /// First argument: Mouse position
        /// Second argument: Pressed button
        /// </summary>
        public event Action<Vector2D<int>, MouseButtons> Click;

        /// <summary>
        /// Called when a mouse double click is performed.
        /// 
        /// First argument: Mouse position
        /// Second argument: Pressed button
        /// </summary>
        public event Action<Vector2D<int>, MouseButtons> DoubleClick;

        /// <summary>
        /// Called when the main mouse wheel is scrolled.
        /// 
        /// First argument: Mouse position
        /// Second argument: Scroll delta
        /// </summary>
        public event Action<Vector2D<int>, float> MouseWheel;

        #endregion
    }
}
