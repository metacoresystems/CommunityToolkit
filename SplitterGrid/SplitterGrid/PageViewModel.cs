using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SplitterGrid
{
    public class PageViewModel : ObservableObject
    {
        private bool _isDesignMode;

        private readonly RelayCommand _editLayoutCommand;
        private readonly RelayCommand _serializeLayoutCommand;

        public PageViewModel(SplitterPanelLayoutControl splitterPanelLayoutControl)
        {
            _editLayoutCommand = new RelayCommand(() => IsDesignMode = !IsDesignMode);
            _serializeLayoutCommand = new RelayCommand(() =>
            {
                var splitterPanelInfo = splitterPanelLayoutControl.GetSplitterPanelInfo();
                Console.WriteLine(splitterPanelInfo);
            });
        }

        public bool IsDesignMode
        { 
            get => _isDesignMode;
            set => SetProperty(ref _isDesignMode, value);
        }

        public RelayCommand EditLayoutCommand => _editLayoutCommand;

        public RelayCommand SerializeLayoutCommand => _serializeLayoutCommand;
    }
}
