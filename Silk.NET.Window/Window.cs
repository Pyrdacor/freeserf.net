using Silk.NET.Core;
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
        Vector2D<int> normalPosition;
        Vector2D<int> normalSize;
        bool fullscreen = false;
        readonly WindowBorder normalBorder;

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
            normalBorder = options.WindowBorder;
            InitEvents();

            normalPosition = Position;
            normalSize = Size;

            WindowAreaChanged?.Invoke(Monitor, normalPosition, normalSize, fullscreen);

            window.Move += (position) =>
            {
                if (!fullscreen && WindowState != WindowState.Minimized)
                    normalPosition = position;

                if (WindowState != WindowState.Minimized)
                    WindowAreaChanged?.Invoke(Monitor, normalPosition, normalSize, fullscreen);
            };

            // The user may resize the window (if resizable). The size of a maximized
            // window is not remembered as the normal size.
            window.Resize += (size) =>
            {
                if (!fullscreen && WindowState == WindowState.Normal && size.X > 0 && size.Y > 0)
                {
                    normalSize = size;
                    WindowAreaChanged?.Invoke(Monitor, normalPosition, normalSize, fullscreen);
                }
            };
        }

        /// <summary>
        /// Limits the size of the window and keeps the given aspect ratio when the user resizes it.
        /// </summary>
        public unsafe void SetSizeLimits(Vector2D<int> minimumSize, Vector2D<int> maximumSize, int aspectNumerator, int aspectDenominator)
        {
            if (window.Native?.Glfw is not nint handle || handle == 0)
                return;

            var glfw = GLFW.Glfw.GetApi();
            var windowHandle = (GLFW.WindowHandle*)handle;

            var size = window.Size;

            glfw.SetWindowSizeLimits(windowHandle, minimumSize.X, minimumSize.Y, maximumSize.X, maximumSize.Y);
            glfw.SetWindowAspectRatio(windowHandle, aspectNumerator, aspectDenominator);

            // Applying the aspect ratio may change the size by a pixel because of rounding.
            if (window.Size != size)
                window.Size = size;
        }

        /// <summary>
        /// Centers the window on its monitor if the window (including the title bar
        /// and the border) is not completely visible on the monitor.
        /// </summary>
        public void CenterOnMonitorIfNotVisible()
        {
            var bounds = Monitor?.Bounds;

            if (bounds == null || fullscreen)
                return;

            var border = BorderSize;
            var frameOrigin = normalPosition - border.Origin;
            var frameSize = normalSize + border.Origin + border.Size;
            var monitorOrigin = bounds.Value.Origin;
            var monitorSize = bounds.Value.Size;

            if (frameOrigin.X >= monitorOrigin.X && frameOrigin.Y >= monitorOrigin.Y &&
                frameOrigin.X + frameSize.X <= monitorOrigin.X + monitorSize.X &&
                frameOrigin.Y + frameSize.Y <= monitorOrigin.Y + monitorSize.Y)
                return;

            normalPosition = new Vector2D<int>(
                monitorOrigin.X + Math.Max(0, (monitorSize.X - frameSize.X) / 2) + border.Origin.X,
                monitorOrigin.Y + Math.Max(0, (monitorSize.Y - frameSize.Y) / 2) + border.Origin.Y);

            window.Position = normalPosition;

            WindowAreaChanged?.Invoke(Monitor, normalPosition, normalSize, fullscreen);
        }

        public Vector2D<int> Position
        {
            get => window.Position;
            set
            {
                if (!fullscreen && WindowState != WindowState.Minimized)
                    normalPosition = value;

                window.Position = value;

                WindowAreaChanged?.Invoke(Monitor, normalPosition, normalSize, fullscreen);
            }
        }

        public Vector2D<int> Size
        {
            get => window.Size;
            set
            {
                if (!fullscreen && WindowState != WindowState.Minimized)
                    normalSize = value;

                window.Size = value;

                WindowAreaChanged?.Invoke(Monitor, normalPosition, normalSize, fullscreen);
            }
        }

        public Rectangle<int> BorderSize => window.BorderSize;

        public WindowState WindowState
        {
            get => window.WindowState;
            set
            {
                if (value == WindowState.Maximized || value == WindowState.Fullscreen)
                {
                    var monitor = window.Monitor;

                    window.WindowState = WindowState.Fullscreen;
                    window.WindowBorder = WindowBorder.Fixed;
                    fullscreen = true;

                    window.Size = monitor?.Bounds.Size ?? window.Size;
                    window.Position = monitor?.Bounds.Origin ?? Vector2D<int>.Zero;

                    WindowAreaChanged?.Invoke(Monitor, normalPosition, normalSize, fullscreen);
                }
                else if (value == WindowState.Minimized)
                {
                    window.WindowState = WindowState.Minimized;
                }
                else
                {
                    window.WindowState = WindowState.Normal;
                    window.WindowBorder = normalBorder;
                    fullscreen = false;

                    EnsureWindowOnMonitor();
                }
            }
        }

        /// <summary>
        /// Moves the window so that it is completely visible (including the title bar).
        /// </summary>
        public void EnsureWindowOnMonitor()
        {
            var bounds = Monitor?.Bounds;
            Vector2D<int> upperLeft = bounds?.Origin ?? new(0, 0);
            int? newX = null;
            int? newY = null;

            if (normalPosition.X - BorderSize.Origin.X < upperLeft.X)
            {
                newX = upperLeft.X + BorderSize.Origin.X;
            }
            else if (bounds != null && normalPosition.X + normalSize.X >= upperLeft.X + bounds.Value.Size.X)
            {
                newX = Math.Max(upperLeft.X + BorderSize.Origin.X, upperLeft.X + bounds.Value.Size.X - normalSize.X - BorderSize.Origin.X - BorderSize.Size.X);
            }

            if (normalPosition.Y - BorderSize.Origin.Y < upperLeft.Y)
            {
                newY = upperLeft.Y + BorderSize.Origin.Y;
            }
            else if (bounds != null && normalPosition.Y + normalSize.Y >= upperLeft.Y + bounds.Value.Size.Y)
            {
                newY = Math.Max(upperLeft.Y + BorderSize.Origin.Y, upperLeft.Y + bounds.Value.Size.Y - normalSize.Y - BorderSize.Origin.Y - BorderSize.Size.Y);
            }

            normalPosition = new(newX ?? normalPosition.X, newY ?? normalPosition.Y);

            window.Position = normalPosition;
            window.Size = normalSize;

            WindowAreaChanged?.Invoke(Monitor, normalPosition, normalSize, fullscreen);
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
                    mouse.Cursor.CursorMode = CursorMode.Hidden;
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

        public IMonitor Monitor => window.Monitor;

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

        public event Action<IMonitor, Vector2D<int>, Vector2D<int>, bool> WindowAreaChanged;

        public void Run() => window?.Run();

        public void Close() => window?.Close();

        public void MakeCurrent() => window?.MakeCurrent();

        public void SwapBuffers() => window?.SwapBuffers();

        public void SetWindowIcon(ref RawImage icon) => WindowExtensions.SetWindowIcon(window, ref icon);

        private void InitEvents()
        {
            Load += () =>
            {
                var input = window.CreateInput();
                InitKeyboardEvents(input);
                InitMouseEvents(input);
                InitGamepad(input);
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


        #region Gamepad Events

        private static MouseButtons _pressedButtons = MouseButtons.None;

        // Steam Deck Feeling
        private const float BaseSpeed = 18f;
        private const float AccelExponent = 1.45f;
        private const float Deadzone = 0.12f;

        private const float TriggerDeadzone = 0.05f; 
        private const float TriggerScrollStrength = 6.0f;

        private void InitGamepad(IInputContext input)
        {
            var pad = input.Gamepads.FirstOrDefault(g => g.IsConnected);

            if (pad == null)
                return;

            pad.ButtonDown += ButtonDown;
            pad.ButtonUp += ButtonUp;
            pad.ThumbstickMoved += ThumbstickMoved;
            pad.TriggerMoved += TriggerMoved;
        }

        private void TriggerMoved(IGamepad pad, Trigger trigger)
        {
            // Trigger.Value ist 0.0 bis 1.0
            float value = trigger.Position;

            if (value < TriggerDeadzone)
                return;

            // Scrollrichtung bestimmen
            float scrollDelta = 0f;

            if (trigger.Index == 0) // LEFT TRIGGER
            {
                // Scroll nach unten
                scrollDelta = -(value * TriggerScrollStrength);
            }
            else if (trigger.Index == 1) // RIGHT TRIGGER
            {
                // Scroll nach oben
                scrollDelta = +(value * TriggerScrollStrength);
            }

            if (scrollDelta != 0f)
            {
                OnMouseWheel(ConvertMousePosition(CursorPosition), scrollDelta);
            }
        }

        private void ButtonDown(IGamepad pad, Button button)
        {
            switch (button.Name)
            {
                case ButtonName.A:
                    Press(MouseButtons.Left);
                    break;

                case ButtonName.B:
                    Press(MouseButtons.Right);
                    break;

                case ButtonName.X:
                    OnMouseWheel(ConvertMousePosition(CursorPosition), -1f);
                    break;

                case ButtonName.Y:
                    OnMouseWheel(ConvertMousePosition(CursorPosition), 1f);
                    break;

                case ButtonName.RightBumper:
                    OnMouseWheel(ConvertMousePosition(CursorPosition), 1f);
                    break;

                case ButtonName.LeftBumper:
                    OnMouseWheel(ConvertMousePosition(CursorPosition), -1f);
                    break;
            }
        }


        private void ButtonUp(IGamepad pad, Button button)
        {
            switch (button.Name)
            {
                case ButtonName.A:
                    Release(MouseButtons.Left, fireClick: true);
                    break;

                case ButtonName.B:
                    Release(MouseButtons.Right, fireClick: true);
                    break;

                case ButtonName.X:
                    Release(MouseButtons.Middle, fireClick: true);
                    break;
            }
        }

        private void Press(MouseButtons button)
        {
            if ((_pressedButtons & button) != 0)
                return;

            _pressedButtons |= button;
            OnMouseDown(ConvertMousePosition(CursorPosition), _pressedButtons);
        }

        private void Release(MouseButtons button, bool fireClick)
        {
            if ((_pressedButtons & button) == 0)
                return;

            _pressedButtons &= ~button;
            OnMouseUp(ConvertMousePosition(CursorPosition), _pressedButtons);

            if (fireClick)
                OnClick(ConvertMousePosition(CursorPosition), button);
        }

        private void ThumbstickMoved(IGamepad pad, Thumbstick stick)
        {            
            // Stick 0 = linker Stick
            if (stick.Index == pad.Thumbsticks[0].Index)
            {
                float x = pad.Thumbsticks[0].X;
                float y = pad.Thumbsticks[0].Y;

                float magnitude = MathF.Sqrt(x * x + y * y);
                if (magnitude < Deadzone)
                    return;

                float normalized = (magnitude - Deadzone) / (1f - Deadzone);
                normalized = Math.Clamp(normalized, 0f, 1f);

                float accel = MathF.Pow(normalized, AccelExponent);

                float dx = x * BaseSpeed * accel;
                float dy = y * BaseSpeed * accel;

                var oldPos = CursorPosition;
                CursorPosition += new Vector2(dx, dy);

                var newPos = CursorPosition;
                var deltaPrecise = newPos - oldPos;
                var newPosInt = ConvertMousePosition(CursorPosition);
                var deltaInt = new Vector2D<int>((int)deltaPrecise.X, (int)deltaPrecise.Y);

                OnMouseMove(newPosInt, _pressedButtons);
                OnMouseMovePrecise(newPos, _pressedButtons);
                OnMouseMoveDelta(newPosInt, _pressedButtons, deltaInt);
                OnMouseMoveDeltaPrecise(newPos, _pressedButtons, deltaPrecise);
            }

            // Stick 1 = rechter Stick → Scroll
            if (stick.Index == pad.Thumbsticks[1].Index)
            {
                var y = pad.Thumbsticks[1].Y;

                if (Math.Abs(y) > 0.2f)
                    OnMouseWheel(ConvertMousePosition(CursorPosition), (float)(-y * 5.0));
            }
        }
        
        #endregion
    }
}
