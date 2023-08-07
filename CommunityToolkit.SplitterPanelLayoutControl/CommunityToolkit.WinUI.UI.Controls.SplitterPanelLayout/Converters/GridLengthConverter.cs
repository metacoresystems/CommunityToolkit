using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommunityToolkit.WinUI.UI.Controls.SplitterPanelLayout.Converters
{
    public class GridLengthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is double doubleValue)
            {
                return new GridLength(doubleValue, GridUnitType.Star);
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is GridLength gridLength && gridLength.IsStar)
            {
                return gridLength.Value;
            }

            return value;
        }
    }
}
