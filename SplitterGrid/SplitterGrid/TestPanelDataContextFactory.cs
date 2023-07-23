using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.UI.Xaml;

namespace SplitterGrid
{
    public class TestPanelDataContextFactory : ISplitterPanelDataContextFactory
    {
        public IEnumerable<(string, Type, DataTemplate)> GetSupportedDataContexts()
        {
            var dataTemplate = (DataTemplate)Application.Current.Resources["testPanelDataTemplate"];

            Console.WriteLine(dataTemplate?.ToString() ?? "null");

            yield return ("Random Number", typeof(TestPanelViewModel), dataTemplate);
        }

        public object CreateDataContext(Type type)
        {
            return new TestPanelViewModel();
        }
    }
}
