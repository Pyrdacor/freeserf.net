using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Silk.NET.Window
{
    internal class Timer
    {
        private readonly Window window = null;
        private int interval = 0;
        private readonly List<TimerTask> runningTasks = new List<TimerTask>();

        private class TimerTask
        {
            private readonly CancellationTokenSource cancelTokenSource = new CancellationTokenSource();

            public void Cancel()
            {
                cancelTokenSource.Cancel();
            }

            public void Run(int interval)
            {
                var cancelled = cancelTokenSource.Token.WaitHandle.WaitOne(TimeSpan.FromMilliseconds(interval));

                if (cancelled)
                {
                    Cancelled?.Invoke(this);
                }
                else
                {
                    Finished?.Invoke(this);
                }
            }

            public event Action<TimerTask> Finished;
            public event Action<TimerTask> Cancelled;
        }

        public Timer(Window window)
        {
            this.window = window;
        }

        public int Interval
        {
            get => interval;
            set
            {
                if (interval == value)
                    return;

                if (Running)
                {
                    Stop();
                    interval = value;
                    Start();
                }
                else
                {
                    interval = value;
                }
            }
        }

        public bool AutoReset
        {
            get;
            set;
        } = true;

        public bool Running
        {
            get;
            private set;
        } = false;

        public void Start()
        {
            if (Running)
                Stop();

            Running = true;

            lock (runningTasks)
            {
                var newTask = new TimerTask();
                runningTasks.Add(newTask);
                newTask.Cancelled += NewTask_Cancelled;
                newTask.Finished += NewTask_Finished;
                Task.Run(() => newTask.Run(Interval));
            }
        }

        private void NewTask_Finished(TimerTask task)
        {
            lock (runningTasks)
            {
                window.InvokeTimerTask(Elapsed);
                runningTasks.Remove(task);

                if (AutoReset)
                    Start();
                else if (runningTasks.Count == 0)
                    Running = false;
            }
        }

        private void NewTask_Cancelled(TimerTask task)
        {
            lock (runningTasks)
            {
                runningTasks.Remove(task);

                if (runningTasks.Count == 0)
                    Running = false;
            }
        }

        public void Stop()
        {
            if (!Running)
                return;

            var tasksCopy = new List<TimerTask>(runningTasks);

            foreach (var runningTask in tasksCopy)
                runningTask.Cancel();

            Running = false;
        }

        public event EventHandler Elapsed;
    }
}
