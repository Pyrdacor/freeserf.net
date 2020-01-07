﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Silk.NET.Input;
using Silk.NET.Input.Common;
using Silk.NET.Windowing.Common;
using Silk.NET.Windowing.Desktop;

namespace Silk.NET.Window
{
    public class Window : GlfwWindow
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
            false
        );

        private static WindowOptions CreateOptions(string title, Point position, Size size)
        {
            var options = DefaultOptions;

            options.Title = title;
            options.Size = size;
            options.Position = position;

            return options;
        }

        private readonly List<Delegate> timerTasks = new List<Delegate>();

        public Window()
            : base(DefaultOptions)
        {
            InitEvents();
        }

        public Window(string title, Point position, Size size)
            : base(CreateOptions(title, position, size))
        {
            InitEvents();
        }

        public Window(string title, Size size)
            : this(title, Point.Empty, size)
        {

        }

        public Window(WindowOptions options)
            : base(options)
        {
            InitEvents();
        }

        public Rectangle ClientRectangle => new Rectangle(Position, Size);

        public bool Initialized { get; private set; } = false;

        private void InitEvents()
        {
            Load += () =>
            {
                Update += Window_TimerUpdate;
                var input = this.GetInput();
                InitKeyboardEvents(input);
                InitMouseEvents(input);
                Initialized = true;
            };
        }


        #region Timers

        internal void InvokeTimerTask(Delegate task)
        {
            lock (timerTasks)
            {
                timerTasks.Add(task);
            }
        }

        private void Window_TimerUpdate(double obj)
        {
            var exceptions = new List<Exception>();
            List<Delegate> tasksCopy;

            lock (timerTasks)
            {
                tasksCopy = new List<Delegate>(timerTasks);
            }

            foreach (var timerTask in tasksCopy)
            {
                try
                {
                    timerTask.DynamicInvoke(this, EventArgs.Empty);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }

            lock (timerTasks)
            {
                timerTasks.Clear();
            }

            if (exceptions.Count > 0)
                throw exceptions[0];
        }

        #endregion


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
            var mouse = input.Mice.FirstOrDefault(m => m.IsConnected);

            if (mouse != null)
            {
                doubleClickTimer = new Timer(this);
                doubleClickTimer.Interval = doubleClickTime;
                doubleClickTimer.AutoReset = false;
                doubleClickTimer.Elapsed += DoubleClickTimer_Elapsed;
                lastMousePosition = mouse.Position;
                ScrollWheel? firstWheel = mouse.ScrollWheels.Count > 0 ? mouse.ScrollWheels.First() : (ScrollWheel?)null;
                lastMouseWheelPosition = firstWheel != null ? firstWheel.Value.Y : 0.0f;

                Closing += doubleClickTimer.Stop; // stop double click timer when window is closing

                mouse.MouseDown += OnMouseDown;
                mouse.MouseUp += OnMouseUp;
                mouse.MouseMove += OnMouseMove;
                mouse.Scroll += OnMouseScroll;
            }
        }

        private Rectangle doubleClickArea = new Rectangle(0, 0, 4, 4);
        private int doubleClickTime = 200;
        private Timer doubleClickTimer = null;
        private bool doubleClickTimerStopped = false;
        private MouseButtons firstClickButton = MouseButtons.None;
        private bool isFirstClick = true;
        private PointF lastMousePosition = PointF.Empty;
        private float lastMouseWheelPosition = 0.0f;        

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
                doubleClickTimer.Interval = value;
            }
        }

        /// <summary>
        /// Maximum size around the first click for the second click
        /// to count as a double click.
        /// 
        /// Default: 4x4 (2 pixel in each direction)
        /// </summary>
        public Size DoubleClickSize
        {
            get => doubleClickArea.Size;
            set
            {
                doubleClickArea.Size = value;
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

        private void AdjustDoubleClickPosition(PointF clickPosition)
        {
            doubleClickArea.Location = new Point(
                (int)Math.Round(clickPosition.X) - doubleClickArea.Width / 2,
                (int)Math.Round(clickPosition.Y) - doubleClickArea.Height / 2
            );
        }

        private Point GetFirstClickPosition()
        {
            return new Point(
                doubleClickArea.X + doubleClickArea.Width / 2,
                doubleClickArea.Y + doubleClickArea.Height / 2
            );
        }

        protected void CancelDoubleClick()
        {
            if (doubleClickTimer.Running)
            {
                doubleClickTimerStopped = true;
                doubleClickTimer.Stop();
                OnClick(GetFirstClickPosition(), firstClickButton);                
            }

            isFirstClick = true;
        }

        private void DoubleClickTimer_Elapsed(object sender, EventArgs e)
        {
            if (doubleClickTimerStopped)
            {
                doubleClickTimerStopped = false;
                return;
            }

            isFirstClick = true;
            OnClick(GetFirstClickPosition(), firstClickButton);
        }

        private void OnMouseDown(IMouse mouse, MouseButton mouseButton)
        {
            var position = ConvertMousePosition(mouse.Position);
            var button = ConvertMouseButton(mouseButton);

            OnMouseDown(position, button);

            if (isFirstClick || firstClickButton != button)
            {
                doubleClickTimerStopped = true;
                doubleClickTimer.Stop();
                FirstClick(position, button);
            }
            else
            {
                if (doubleClickTimer.Running)
                {
                    doubleClickTimerStopped = true;
                    doubleClickTimer.Stop();

                    if (doubleClickArea.Contains(position))
                    {
                        isFirstClick = true;
                        OnDoubleClick(position, button);
                    }
                    else
                    {
                        OnClick(GetFirstClickPosition(), firstClickButton);
                        FirstClick(position, button);
                    }
                }                    
            }
        }

        private void FirstClick(Point position, MouseButtons button)
        {
            isFirstClick = false;
            firstClickButton = button;
            AdjustDoubleClickPosition(position);
            doubleClickTimerStopped = false;
            doubleClickTimer.Start();
        }

        private void OnMouseUp(IMouse mouse, MouseButton mouseButton)
        {
            OnMouseUp(ConvertMousePosition(mouse.Position), ConvertMouseButton(mouseButton));
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
                float firstWheelPosition = mouse.ScrollWheels.First().Y;

                if (lastMouseWheelPosition != firstWheelPosition)
                {
                    float delta = firstWheelPosition - lastMouseWheelPosition;
                    OnMouseWheel(ConvertMousePosition(mouse.Position), delta);
                    lastMouseWheelPosition = firstWheelPosition;
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
