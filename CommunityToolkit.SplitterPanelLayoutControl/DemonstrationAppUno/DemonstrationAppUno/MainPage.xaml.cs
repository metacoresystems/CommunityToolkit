using CommunityToolkit.WinUI.UI.Controls.SplitterPanelLayout;
using DemonstrationAppUno.ViewModel;

namespace DemonstrationAppUno
{
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            this.Loaded += MainPage_Loaded;
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            SplitterPanelLayoutControl layoutControl = (SplitterPanelLayoutControl)FindName("splitterPanelLayout");
            DataContext = new PageViewModel(layoutControl);
        }
    }
}