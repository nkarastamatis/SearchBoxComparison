using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SearchBoxComparison
{
    class Program
    {
        static void Main(string[] args)
        {
            var txtBox = new ReactiveSearchBox() 
            { 
                BorderStyle = BorderStyle.None 
            };
            var searchBox = new SearchBox()
            {
                BorderStyle = BorderStyle.None,
                Left = txtBox.Right + 5
            };

            var form = new Form() { };
            form.Controls.Add(txtBox);
            form.Controls.Add(searchBox);
            form.ShowDialog();
        }
    }
}
