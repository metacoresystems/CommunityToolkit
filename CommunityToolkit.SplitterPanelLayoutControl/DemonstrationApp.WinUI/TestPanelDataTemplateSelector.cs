using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DemonstrationApp.WinUI
{
    /// <summary>
    /// The data template selector for the test panels
    /// </summary>
    public class TestPanelDataTemplateSelector : DataTemplateSelector
    {
        private static readonly DataTemplate TestPanelDataTemplate = (DataTemplate)App.Current.Resources["testPanelDataTemplate"];

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            return TestPanelDataTemplate;
        }

        protected override DataTemplate SelectTemplateCore(object item)
        {
            return TestPanelDataTemplate;
        }
    }
}
