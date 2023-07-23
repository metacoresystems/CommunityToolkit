using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;

namespace SplitterGrid
{
    public class SplitterPanelContainerInfo : ObservableObject
    {
        private readonly SplitterPanelContainerInfo _parentContainerInfo;
        private SplitterInfo _splitterInfo;

        public SplitterPanelContainerInfo(SplitterPanelContainerInfo panelContainerInfo)
        {
            // Allowed to be null for top level containers
            _parentContainerInfo = panelContainerInfo;
        }

        public bool IsTopLevel => _parentContainerInfo == null;

        public bool IsSplitterActive => SplitterInfo.Mode != SplitterMode.ContentHost;

        public void SetSplitterMode(SplitterMode splitterMode)
        {
            // Start always with a 50% split
            SplitterInfo = new SplitterInfo
            {
                Mode = splitterMode,
                FirstChildGridLength = 0.5,
                SecondChildGridLength = 0.5
            };
        }

        public SplitterInfo SplitterInfo
        {
            get => _splitterInfo;
            private set
            {
                if (_splitterInfo != value)
                {
                    _splitterInfo = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsSplitterActive));
                }
            }
        }
    }
}
