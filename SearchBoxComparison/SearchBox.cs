using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections.Concurrent;

namespace SearchBoxComparison
{
    internal class SearchBox : TextBox
    {
        string _lastSearch;
        TaskHandler TaskHandler = new TaskHandler();
        public SearchBox()
        {
            TextChanged += new ThrottledEventHandler(
                txtBox_TextChanged, 
                TimeSpan.FromSeconds(.5));
        }

        private async void txtBox_TextChanged(object sender, EventArgs e)
        {
            var text = ((TextBox)sender).Text;

            if (String.IsNullOrWhiteSpace(text))
                return;

            if (_lastSearch == text)
                return;

            _lastSearch = text;

            TaskHandler.Add(text);

        }
    }

    internal class TaskHandler
    {
        ConcurrentQueue<Task<string>> Tasks { get; set; }
        Task LastTask { get; set; }
        CancellationTokenSource CurrentSource { get; set; }
        CancellationTokenSource HandleSource { get; set; }
        public TaskHandler()
        {
            Tasks = new ConcurrentQueue<Task<string>>();
            CurrentSource = new CancellationTokenSource();
        }

        internal void Add(string text)
        {
            if (HandleSource != null)
                HandleSource.Cancel();

            var task = Task.Run(() => Service.Search(text), CurrentSource.Token);
            Add(task);
        }

        internal void Add(Task<string> task)
        {
            Tasks.Enqueue(task);
            LastTask = task;
            Restart();
        }

        private void Restart()
        {
            if (HandleSource != null)
            {
                HandleSource = null;
            }

            HandleSource = new CancellationTokenSource();
            Task.Run(() => Handle(HandleSource), HandleSource.Token);
        }

        private void Handle(CancellationTokenSource source)
        {
            Console.WriteLine("Started");
            while (true)
            {
                if (source.IsCancellationRequested)
                    return;

                var tasks = Tasks.ToArray();
                var index = Task.WaitAny(tasks);
                if (index > -1)
                {
                    Task<string> result = null;
                    while (!source.IsCancellationRequested && Tasks.TryDequeue(out result) && !tasks[index].Equals(result)) { }
                    if (source.IsCancellationRequested)
                        return;
                    if (tasks[index].IsCanceled)
                        return;

                    if (tasks[index].Equals(LastTask))
                    {
                        CurrentSource.Cancel();
                        CurrentSource.Dispose();
                        CurrentSource = new CancellationTokenSource();
                        Console.WriteLine(tasks[index].Result);
                        return;
                    }

                    Console.WriteLine(tasks[index].Result);
                }
            }
        }
    }

    internal class ThrottledEventHandler
    {
        private readonly EventHandler<EventArgs> _innerHandler;
        private readonly EventHandler _outerHandler;
        private readonly System.Windows.Forms.Timer _throttleTimer;

        private readonly object _throttleLock = new object();
        private Action _delayedHandler = null;

        public ThrottledEventHandler(EventHandler<EventArgs> handler, TimeSpan delay)
        {
            _innerHandler = handler;
            _outerHandler = HandleIncomingEvent;
            _throttleTimer = new System.Windows.Forms.Timer() { Interval = (int)delay.TotalMilliseconds };
            _throttleTimer.Tick += Timer_Tick;
        }

        private void HandleIncomingEvent(object sender, EventArgs args)
        {
            lock (_throttleLock)
            {
                if (_throttleTimer.Enabled)
                {
                    _delayedHandler = () => SendEventToHandler(sender, args);
                }
                else
                {
                    SendEventToHandler(sender, args);
                }
            }
        }

        private void SendEventToHandler(object sender, EventArgs args)
        {
            if (_innerHandler != null)
            {
                _innerHandler(sender, args);
                _throttleTimer.Start();
            }
        }

        private void Timer_Tick(object sender, EventArgs args)
        {
            lock (_throttleLock)
            {
                _throttleTimer.Stop();
                if (_delayedHandler != null)
                {
                    _delayedHandler();
                    _delayedHandler = null;
                }
            }
        }

        public static implicit operator EventHandler(ThrottledEventHandler throttledHandler)
        {
            return throttledHandler._outerHandler;
        }
    }
}
