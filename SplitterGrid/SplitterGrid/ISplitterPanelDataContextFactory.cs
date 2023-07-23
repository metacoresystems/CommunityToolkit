using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.UI.Xaml;

namespace SplitterGrid
{
    /// <summary>
    /// A factory that creates a data context for a panel
    /// </summary>
    public interface ISplitterPanelDataContextFactory
    {
        IEnumerable<(string, Type, DataTemplate)> GetSupportedDataContexts();

        object CreateDataContext(Type type);
    }
}
