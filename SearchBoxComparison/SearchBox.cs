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

            if (_cancellationTokenSource != null)
                _cancellationTokenSource.Cancel();

            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;

            var task = Task.Run(() => Service.Search(text), token);

            task.ContinueWith(UpdateSearchResults,
                token,
                TaskContinuationOptions.NotOnCanceled,
                TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void UpdateSearchResults(Task<string> task)
        {
            Console.WriteLine(task.Result);
        }

        string _lastSearch;
        CancellationTokenSource _cancellationTokenSource;
        public System.Windows.Forms.Timer ThrottleTimer { get; set; }
    }
}
