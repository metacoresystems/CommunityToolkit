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
        /// <summary>
        /// Returns the supported data contexts keyed by user facing descriptive name
        /// </summary>
        Dictionary<string, Type> GetSupportedDataContexts();

        /// <summary>
        /// For the given data context type, returns a default data context instance for the type
        /// </summary>
        /// <param name="type">The data context type</param>
        /// <returns>A default instance of the data context type</returns>
        object CreateDataContext(Type type);
    }
}
