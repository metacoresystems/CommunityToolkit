using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplitterGrid
{
    public enum SplitterMode
    {
        // The splitter grid is currently hosting panel content without any horizontal or vertical splitters
        ContentHost,
        // The splitter grid is not hosting a panel itself but currently has two child splitter grids and a horizontal grid splitter
        Horizontal,
        // The splitter grid is not hosting a panel itself but currently has two child splitter grids and a vertical grid splitter
        Vertical
    }

    public class SplitterInfo : ObservableObject
    {
        private SplitterMode _mode;

        private double _firstChildGridLength;
        private double _secondChildGridLength;

        /// <summary>
        /// The current mode of the splitter grid
        /// </summary>
        public SplitterMode Mode
        {
            get => _mode;
            set => SetProperty(ref _mode, value);
        }

        public double FirstChildGridLength
        {
            get => _firstChildGridLength;
            set => SetProperty(ref _firstChildGridLength, value);
        }

        public double SecondChildGridLength
        {
            get => _secondChildGridLength;
            set => SetProperty(ref _secondChildGridLength, value);
        }
    }
}
