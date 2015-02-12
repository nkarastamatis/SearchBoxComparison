using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Concurrency;

namespace SearchBoxComparison
{
    class Program
    {
        static void Main(string[] args)
        {
            var txtBox = new TextBox();
            var form = new Form() { };
            form.Controls.Add(txtBox);

            var textChanged = Observable.FromEventPattern(txtBox, "TextChanged")
                .Throttle(TimeSpan.FromSeconds(.5))
                .Select(_ => txtBox.Text)
                .Where(text => !String.IsNullOrWhiteSpace(text))
                .DistinctUntilChanged()
                .Select(text => Search(text))
                .Switch();

            //textChanged.Subscribe(
            //    onNext: text => Console.WriteLine(text));
            
            txtBox.TextChanged += CreateThrottledEventHandler(
                txtBox_TextChanged, 
                TimeSpan.FromSeconds(0));

            form.Activate();
            form.ShowDialog();
        }

        static string lastSearch;
        static async void txtBox_TextChanged(object sender, EventArgs e)
        {
            var text = ((TextBox)sender).Text;

            if (String.IsNullOrWhiteSpace(text))
                return;

            if (lastSearch == text)
                return;

            lastSearch = text;
            
            var result = await Search(text);
            

            Console.WriteLine(result);
        }
       
        private static EventHandler CreateThrottledEventHandler(
            EventHandler<EventArgs> handler,
            TimeSpan throttle)
        {
            bool throttling = false;
            return (s, e) =>
            {
                if (throttling) return;
                handler(s, e);
                throttling = true;
                Task.Delay(throttle).ContinueWith(_ => throttling = false);
            };
        }


        static async Task<string> Search(string text)
        {
            if (text == "long")
                await Task.Delay(10000);

            return String.Format("Results for {0}", text);
        }
    }
}
