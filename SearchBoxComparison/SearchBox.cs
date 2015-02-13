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
        public SearchBox()
        {
            TextChanged += SearchBox_TextChanged;
            ThrottleTimer = new System.Windows.Forms.Timer();
            ThrottleTimer.Stop();
            ThrottleTimer.Interval = 500;
            ThrottleTimer.Tick += ThrottleTimer_Tick;
        }

        private void SearchBox_TextChanged(object sender, EventArgs e)
        {
            // Using this event handler to reset the timer so the Search
            // only occurs if the user stopped typing. 
            // The function has usless objects passed in, but you have to 
            // use this signiture to register for the event. 

            ThrottleTimer.Stop();
            ThrottleTimer.Start();
        }

        void ThrottleTimer_Tick(object sender, EventArgs e)
        {
            // Using this event handler to fire off a Task to 
            // search using the text in the TextBox.

            var text = this.Text;

            if (String.IsNullOrWhiteSpace(text))
                return;

            // If we are in here with the same text as before
            // we dont want to ask for the same results and
            // waste resources.
            if (_lastSearch == text)
                return;

            // Set the lastSearch for checking the next time around.
            _lastSearch = text;

            TaskHandler.Add(text);
        }

        string _lastSearch;
        TaskHandler TaskHandler = new TaskHandler();
        public System.Windows.Forms.Timer ThrottleTimer { get; set; }
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
}
