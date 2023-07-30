using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SplitterGrid
{
    /// <summary>
    /// Basic test page viewmodel
    /// </summary>
    public class PageViewModel : ObservableObject
    {
        private bool _isDesignMode;

        private readonly RelayCommand _editLayoutCommand;
        private readonly RelayCommand _serializeLayoutCommand;
        private readonly RelayCommand _deserializeLayoutCommand;
        private readonly RelayCommand _clearLayoutCommand;

        private SplitterPanelInfo _lastSavedSplitterPanelInfo;

        public PageViewModel(SplitterPanelLayoutControl splitterPanelLayoutControl)
        {
            _editLayoutCommand = new RelayCommand(() => IsDesignMode = !IsDesignMode);
            _serializeLayoutCommand = new RelayCommand(() =>
            {
                _lastSavedSplitterPanelInfo = splitterPanelLayoutControl.SaveLayout();
            });
            _deserializeLayoutCommand = new RelayCommand(() =>
            {
                if (_lastSavedSplitterPanelInfo == null) return;
                splitterPanelLayoutControl.LoadLayout(_lastSavedSplitterPanelInfo);
            });
            _clearLayoutCommand = new RelayCommand(() =>
            {
                splitterPanelLayoutControl.ClearLayout();
            });
        }

        /// <summary>
        /// Indicates whether the layout is in design mode
        /// </summary>
        public bool IsDesignMode
        { 
            get => _isDesignMode;
            set => SetProperty(ref _isDesignMode, value);
        }

        /// <summary>
        /// Edit the page layout
        /// </summary>
        public RelayCommand EditLayoutCommand => _editLayoutCommand;

        /// <summary>
        /// Saves the layout to a <see cref="SplitterPanelInfo"/> object
        /// </summary>
        public RelayCommand SerializeLayoutCommand => _serializeLayoutCommand;

        /// <summary>
        /// Loads the layout from a <see cref="SplitterPanelInfo"/> object
        /// </summary>
        public RelayCommand DeserializeLayoutCommand => _deserializeLayoutCommand;

        /// <summary>
        /// Clears the current layout
        /// </summary>
        public RelayCommand ClearLayoutCommand => _clearLayoutCommand;
    }
}
