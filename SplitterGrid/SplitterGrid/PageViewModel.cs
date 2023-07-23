using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplitterGrid
{
    public class PageViewModel : ObservableObject
    {
        private bool _isDesignMode;

        private readonly RelayCommand _editLayoutCommand;

        public PageViewModel()
        {
            _editLayoutCommand = new RelayCommand(() => IsDesignMode = !IsDesignMode);
        }

        public bool IsDesignMode
        { 
            get => _isDesignMode;
            set => SetProperty(ref _isDesignMode, value);
        }

        public RelayCommand EditLayoutCommand => _editLayoutCommand;
    }
}
