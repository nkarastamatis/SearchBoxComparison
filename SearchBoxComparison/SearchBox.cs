using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SearchBoxComparison
{
    internal class SearchBox : TextBox
    {
        string _lastSearch;

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

            var result = await Service.Search(text);


            Console.WriteLine(result);
        }
    }

    public class ThrottledEventHandler
    {
        private readonly EventHandler<EventArgs> _innerHandler;
        private readonly EventHandler _outerHandler;
        private readonly Timer _throttleTimer;

        private readonly object _throttleLock = new object();
        private Action _delayedHandler = null;

        public ThrottledEventHandler(EventHandler<EventArgs> handler, TimeSpan delay)
        {
            _innerHandler = handler;
            _outerHandler = HandleIncomingEvent;
            _throttleTimer = new Timer() { Interval = (int)delay.TotalMilliseconds };
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
