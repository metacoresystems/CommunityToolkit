using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.UI.Xaml;

using CommunityToolkit.WinUI.UI.Controls.SplitterPanelLayout;
using DemonstrationApp.WinUI.ViewModel;

namespace DemonstrationApp.WinUI
{
    /// <summary>
    /// A factory that creates a data context for a panel
    /// </summary>
    public class TestPanelDataContextFactory : ISplitterPanelDataContextFactory
    {
        public Dictionary<string, Type> GetSupportedDataContexts()
        {
            var supportedDataContexts = new Dictionary<string, Type>();
            supportedDataContexts.Add("Random Number", typeof(TestPanelViewModel));

            return supportedDataContexts;
        }

        public object CreateDataContext(Type type)
        {
            return new TestPanelViewModel();
        }
    }
}
