using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

using Windows.Foundation;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Core;

using CommunityToolkit.WinUI.UI.Controls;
using CommunityToolkit.WinUI.UI.Controls.SplitterPanelLayout.Utilities;
using CommunityToolkit.WinUI.UI.Controls.SplitterPanelLayout.Converters;

namespace CommunityToolkit.WinUI.UI.Controls.SplitterPanelLayout
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

    /// <summary>
    /// The core splitter grid control panel, which can either host two content hosts and a splitter
    /// or be a content host itself
    /// </summary>
    public partial class SplitterPanelControl : Grid
    {
        public static readonly DependencyProperty FirstChildProportionalSizeProperty = DependencyProperty.Register(
            nameof(FirstChildProportionalSize),
            typeof(double?),
            typeof(SplitterPanelControl),
            new PropertyMetadata(null));

        public static readonly DependencyProperty SecondChildProportionalSizeProperty = DependencyProperty.Register(
            nameof(SecondChildProportionalSize),
            typeof(double?),
            typeof(SplitterPanelControl),
            new PropertyMetadata(null));

        public static readonly DependencyProperty SplitterModeProperty = DependencyProperty.Register(
            nameof(SplitterMode),
            typeof(SplitterMode),
            typeof(SplitterPanelControl),
            // By default, the splitter mode is a content host
            new PropertyMetadata(SplitterMode.ContentHost, OnSplitterModeChanged));

        private static void OnSplitterModeChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
        {
            SplitterPanelControl splitterPanelControl = (SplitterPanelControl)dependencyObject;
            splitterPanelControl.SetSplitterMode((SplitterMode)args.NewValue);
        }

        private readonly double DefaultSplitterThickness = 5.0;

        // For when this panel is being dragged to another location
        private Point _dragStart;
        private Grid _dragPreviewGrid;

        // For when a content host
        private ContentControl _contentControl;

        // For when a split grid host
        private GridSplitter _gridSplitter;
        private SplitterPanelControl _firstChildSplitterPanelControl;
        private SplitterPanelControl _secondChildSplitterPanelControl;

        private static readonly SolidColorBrush SelectionHighlightColor = new TextBox().SelectionHighlightColor;

        public SplitterPanelControl()
        {
            // We start as a content host always
            SetSplitterMode(SplitterMode.ContentHost);
        }

        /// <summary>
        /// Ensures the splitter panel control is torn down on closing
        /// Note: This is not ideal - would really like to be able to override
        /// dispose but this is not possible with the underlying superclass
        /// </summary>
        public void Reset()
        {
            ToggleDesignMode(false);

            _firstChildSplitterPanelControl?.Reset();
            _secondChildSplitterPanelControl?.Reset();

            _firstChildSplitterPanelControl = null;
            _secondChildSplitterPanelControl = null;
        }

        private SplitterPanelLayoutControl GetParentLayout()
        {
            return this.FindAscendant<SplitterPanelLayoutControl>();
        }

        /// <summary>
        /// Recursively obtains the splitter panel info for this panel and all child panels
        /// </summary>
        /// <returns></returns>
        internal SplitterPanelInfo ToSplitterPanelInfo()
        {
            var splitterPanelInfo = new SplitterPanelInfo();

            splitterPanelInfo.FirstChildProportionalSize = FirstChildProportionalSize;
            splitterPanelInfo.SecondChildProportionalSize = SecondChildProportionalSize;
            splitterPanelInfo.SplitterMode = SplitterMode;
            splitterPanelInfo.FirstChildSplitterPanelInfo = _firstChildSplitterPanelControl?.ToSplitterPanelInfo();
            splitterPanelInfo.SecondChildSplitterPanelInfo = _secondChildSplitterPanelControl?.ToSplitterPanelInfo();
            splitterPanelInfo.DataContext = DataContext;

            return splitterPanelInfo;
        }

        /// <summary>
        /// Recursively creates a splitter panel control from the splitter panel info
        /// </summary>
        /// <param name="splitterPanelInfo"></param>
        /// <returns>The splitter panel control</returns>
        internal static SplitterPanelControl FromSplitterPanelInfo(Grid parentGrid, SplitterPanelInfo splitterPanelInfo)
        {
            var splitterPanelControl = new SplitterPanelControl();
            parentGrid.Children.Add(splitterPanelControl);

            splitterPanelControl.SplitterMode = splitterPanelInfo.SplitterMode;

            if (splitterPanelInfo.FirstChildSplitterPanelInfo != null)
            {
                splitterPanelControl.ReplaceFirstChildGridSplitter(SplitterPanelControl.FromSplitterPanelInfo(splitterPanelControl, splitterPanelInfo.FirstChildSplitterPanelInfo));
            }

            if (splitterPanelInfo.SecondChildSplitterPanelInfo != null)
            {
                splitterPanelControl.ReplaceSecondChildGridSplitter(SplitterPanelControl.FromSplitterPanelInfo(splitterPanelControl, splitterPanelInfo.SecondChildSplitterPanelInfo));
            }

            splitterPanelControl.FirstChildProportionalSize = splitterPanelInfo.FirstChildProportionalSize;
            splitterPanelControl.SecondChildProportionalSize = splitterPanelInfo.SecondChildProportionalSize;

            if (splitterPanelInfo.DataContext != null)
            {
                splitterPanelControl.DataContext = splitterPanelInfo.DataContext;

                var dataTemplateSelector = splitterPanelControl.GetParentLayout()?.DataTemplateSelector;

                // Note: Would use a DataTemplateSelector here, but there seems to be a bug in
                // Uno's repainting of the content control when the template selector is changed
                // We therefore for now call the data template selector directly
                splitterPanelControl._contentControl.ContentTemplateSelector = dataTemplateSelector;

                var dataTemplate = dataTemplateSelector?.SelectTemplate(splitterPanelControl.DataContext, splitterPanelControl);
                splitterPanelControl._contentControl.ContentTemplate = dataTemplate;
            }

            return splitterPanelControl;
        }

        internal void ToggleDesignMode(bool enabled)
        {
            // Design overlay is only present in content hosts
            if (SplitterMode == SplitterMode.ContentHost)
            {
                if (enabled)
                {
                    ShowDesignModePanelOverlay();
                    CreateSplitterContextMenu();

                    // No need to allow drag repositioning if this is the only top level splitter
                    if (!IsTopLevel)
                    {
                        PointerPressed += OnPointerPressed;
                        PointerReleased += OnPointerReleased;
                        PointerMoved += OnPointerMoved;
                    }
                }
                else
                {
                    if (!IsTopLevel)
                    {
                        PointerPressed -= OnPointerPressed;
                        PointerReleased -= OnPointerReleased;
                        PointerMoved -= OnPointerMoved;
                    }

                    DestroySplitterContextMenu();
                    HideDesignModePanelOverlay();
                }
            }

            if (_gridSplitter == null) return;

            _gridSplitter.IsEnabled = enabled;

            var childGridSplitters = Children.OfType<SplitterPanelControl>().ToList();

            foreach (var childGridSplitter in childGridSplitters)
            {
                childGridSplitter.ToggleDesignMode(enabled);
            }
        }

        /// <summary>
        /// Switches the splitter grid between being a content host or a splitter host
        /// </summary>
        /// <param name="splitterMode"></param>
        private void SetSplitterMode(SplitterMode splitterMode)
        {
            if (splitterMode == SplitterMode.ContentHost)
            {
                CreateContentHostUI();
            }
            else
            {
                CreateSplitterUI(splitterMode);
            }
        }

        /// <summary>
        /// Recursively sets the thickness of all grid splitters in the panel,
        /// including on child splitter grid controls
        /// </summary>
        /// <param name="thickness"></param>
        public void SetGridSplitterThickness(double thickness)
        {
            // If a content host, we don't have a grid splitter
            if (SplitterMode == SplitterMode.ContentHost) return;

            if (SplitterMode == SplitterMode.Horizontal)
            {
                _gridSplitter.Height = thickness;
            }
            else if (SplitterMode == SplitterMode.Vertical)
            {
                _gridSplitter.Width = thickness;
            }

            _firstChildSplitterPanelControl?.SetGridSplitterThickness(thickness);
            _secondChildSplitterPanelControl?.SetGridSplitterThickness(thickness);
        }

        private void RemovePanel()
        {
            if (IsSplitterActive || IsTopLevel) return;

            // Remove this panel from the parent splitter grid and then 'unsplit' the parent splitter
            if (Parent is SplitterPanelControl parentSplitterGridControl)
            {
                parentSplitterGridControl.RemoveChildPanel(this);
            }
        }

        private void MergeInPanelElements(SplitterPanelControl remainingGridControl)
        {
            Children.Clear();

            // The remaining grid control is itself split, so we need to remove the child
            // splitter and merge the remaining grid control into this grid control

            // Deep copy the children and assign to the members for this grid control
            foreach (var remainingChildControl in remainingGridControl.Children)
            {
                Children.Add(remainingChildControl);

                if (remainingChildControl is GridSplitter gridSplitter) _gridSplitter = gridSplitter;
                if (remainingChildControl is ContentControl contentControl) _contentControl = contentControl;
                if (remainingChildControl is ContentControl settingsContentControl) _designModeSettingsContentControl = settingsContentControl;
                if (remainingChildControl is Grid designOverlayModeGrid) _designModeOverlayGrid = designOverlayModeGrid;
                if (remainingChildControl is Border designOverlayModeBorder) _designModeOverlayBorder = designOverlayModeBorder;
            }

            // Recreate the split
            if (remainingGridControl.SplitterMode == SplitterMode.Horizontal)
            {
                SetSplitterMode(SplitterMode.Horizontal);

                foreach (var rowDefinition in remainingGridControl.RowDefinitions)
                {
                    RowDefinitions.Add(rowDefinition);
                }
            }
            else if (remainingGridControl.SplitterMode == SplitterMode.Vertical)
            {
                SetSplitterMode(SplitterMode.Vertical);

                foreach (var columnDefinition in remainingGridControl.ColumnDefinitions)
                {
                    ColumnDefinitions.Add(columnDefinition);
                }
            }
        }

        /// <summary>
        /// Indicates whether the grid splitter is in design mode, showing the panel
        /// overlays
        /// </summary>
        public bool DesignMode => GetParentLayout()?.DesignMode ?? false;

        /// <summary>
        /// Indicates whether the panel is currently the active drop panel should
        /// the user drop another panel on it
        /// </summary>
        public bool IsDropActive { get; private set; }

        protected bool IsSplitterActive => SplitterMode == SplitterMode.Horizontal || SplitterMode == SplitterMode.Vertical;

        protected bool IsTopLevel => Parent is SplitterPanelLayoutControl;

        public double? FirstChildProportionalSize
        {
            get => (double?)GetValue(FirstChildProportionalSizeProperty);
            set => SetValue(FirstChildProportionalSizeProperty, value);
        }

        public double? SecondChildProportionalSize
        {
            get => (double?)GetValue(SecondChildProportionalSizeProperty);
            set => SetValue(SecondChildProportionalSizeProperty, value);
        }

        public SplitterMode SplitterMode
        {
            get => (SplitterMode)GetValue(SplitterModeProperty);
            set => SetValue(SplitterModeProperty, value);
        }
    }
}
