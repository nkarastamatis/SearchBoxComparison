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
    internal class ReactiveSearchBox : TextBox
    {
        public ReactiveSearchBox()
        {
            SearchResults
                .Subscribe(
                    onNext: result => Console.WriteLine("Reactive: {0}", result));
        }

        public IObservable<string> SearchResults
        {
            get
            {
                return Observable.FromEventPattern(this, "TextChanged")
                    .Throttle(TimeSpan.FromSeconds(.5))
                    .Select(_ => this.Text)
                    .Where(text => !String.IsNullOrWhiteSpace(text))
                    .DistinctUntilChanged()
                    .Select(text => Service.Search(text))
                    .Switch()
                    .Catch(new Func<Exception, IObservable<string>>(onError));
            }
        }

        private IObservable<string> onError(Exception ex)
        {
            Console.WriteLine(ex.Message);
            return SearchResults;
        }

    }
}
