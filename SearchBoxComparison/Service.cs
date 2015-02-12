using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SearchBoxComparison
{
    internal static class Service
    {
        internal static async Task<string> Search(string text)
        {
            if (text == "long" || text == "long2")
                await Task.Delay(20000);

            if (text == "error")
                throw new Exception("Search error.");

            return String.Format("Results for {0}", text);
        }
    }
}
