using CommunityToolkit.WinUI.UI.Controls.SplitterPanelLayout;
using DemonstrationApp.WinUI.ViewModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DemonstrationApp.WinUI
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            
            if (Content is FrameworkElement frameworkElement)
            {
                frameworkElement.Loaded += FrameworkElement_Loaded;
            }
        }

        private void FrameworkElement_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement frameworkElement)
            {
                SplitterPanelLayoutControl layoutControl = (SplitterPanelLayoutControl)frameworkElement.FindName("splitterPanelLayout");
                frameworkElement.DataContext = new PageViewModel(layoutControl);
            }
        }
    }
}
