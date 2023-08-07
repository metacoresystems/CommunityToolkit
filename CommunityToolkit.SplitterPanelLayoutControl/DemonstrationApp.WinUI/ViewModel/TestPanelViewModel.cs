using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;

namespace DemonstrationApp.WinUI.ViewModel
{
    /// <summary>
    /// Main viewmodel for a test panel, containing just a color and random number
    /// </summary>
    public class TestPanelViewModel
    {
        private long _randomNumber;

        public TestPanelViewModel()
        {
            Random random = new Random((int)DateTime.UtcNow.Ticks);

            _randomNumber = random.Next();
            RandomColor = new SolidColorBrush(Color.FromArgb(40, (byte)random.Next(255), (byte)random.Next(255), (byte)random.Next(255)));
        }

        public string RandomNumber => _randomNumber.ToString();

        public SolidColorBrush RandomColor { get; }
    }
}
