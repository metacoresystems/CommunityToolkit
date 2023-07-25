using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplitterGrid.Test
{
    /// <summary>
    /// The data template selector for the test panels
    /// </summary>
    public class TestPanelDataTemplateSelector : DataTemplateSelector
    {
        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            return (DataTemplate)App.Current.Resources["testPanelDataTemplate"];
        }
    }
}
