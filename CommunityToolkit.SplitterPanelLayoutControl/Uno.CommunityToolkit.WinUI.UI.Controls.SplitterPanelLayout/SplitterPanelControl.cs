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
    public class SplitterPanelControl : Grid
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
        private Grid _designModeOverlayGrid;
        private Border _designModeOverlayBorder;

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

        /// <summary>
        /// Toggles the visual UI of the panel to indicate whether the panel is
        /// considered drop active or not
        /// </summary>
        internal void SetDropActive(bool active)
        {
            if (IsSplitterActive) return;

            IsDropActive = active;

            if (active)
            {
                _designModeOverlayBorder.BorderBrush = SelectionHighlightColor;
                return;
            }

            _designModeOverlayBorder.BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
        }

        private Grid CreateDragPreviewControl(Size size)
        {
            Grid grid = new Grid();

            grid.Background = Colors.Black;
            grid.Opacity = 0.5;
            grid.Width = size.Width;
            grid.Height = size.Height;
            grid.HorizontalAlignment = HorizontalAlignment.Left;
            grid.VerticalAlignment = VerticalAlignment.Top;
            grid.IsHitTestVisible = false;

            return grid;
        }

        private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            // Nothing to do if not in design mode
            if (!DesignMode) return;

            SplitterPanelLayoutControl parentLayout = GetParentLayout();
            var pointerProperties = e.GetCurrentPoint(this).Properties;

            // In any circumstance, cannot drag if not in contact (mouse button pressed, touch point down etc.)
            if (!e.Pointer.IsInContact || !pointerProperties.IsLeftButtonPressed) return;

            // Ensure pointer is captured
            if (CapturePointer(e.Pointer) && _dragPreviewGrid == null)
            {
                // Get the start point with respect to the parent layout
                Point currentPoint = e.GetCurrentPoint(parentLayout).Position;
                _dragStart = currentPoint;

                SplitterUtilities.SetCurrentCursor(CoreCursorType.SizeAll);

                // Create a shadow visual for the drag preview
                // Note: Not clear at the moment how we could create a clone of the panel visual, so we
                // use this simpler drag preview for now
                _dragPreviewGrid = CreateDragPreviewControl(new Size(ActualWidth, ActualHeight));

                GetParentLayout().AppendDragPreview(_dragPreviewGrid);

                _dragPreviewGrid.RenderTransform = new Microsoft.UI.Xaml.Media.TranslateTransform()
                {
                    X = currentPoint.X,
                    Y = currentPoint.Y
                };
            }

            e.Handled = true;
        }

        private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (!DesignMode) return;

            if (_dragPreviewGrid == null) return;

            SplitterPanelLayoutControl parentLayout = GetParentLayout();
            Point currentPoint = e.GetCurrentPoint(parentLayout).Position;

            _dragPreviewGrid.RenderTransform = new Microsoft.UI.Xaml.Media.TranslateTransform()
            {
                X = currentPoint.X,
                Y = currentPoint.Y
            };

            parentLayout.OnCapturedPointerMove(this, e);

            e.Handled = true;
        }

        private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (!DesignMode) return;
            if (_dragPreviewGrid == null) return;

            if (PointerCaptures.Any(p => e.Pointer.PointerId == p.PointerId))
            {
                _dragStart = null;

                ReleasePointerCapture(e.Pointer);

                SplitterPanelLayoutControl parentLayout = GetParentLayout();

                parentLayout.OnCapturedPointerReleased(this, e);
                parentLayout.RemoveDragPreview(_dragPreviewGrid);
                _dragPreviewGrid = null;

                SplitterUtilities.SetCurrentCursor(CoreCursorType.Arrow);
            }
        }

        private void FindContentHostSplitterPanels(List<SplitterPanelControl> inactiveSplitterGrids, SplitterPanelControl splitterGrid)
        {
            if (!splitterGrid.IsSplitterActive)
                inactiveSplitterGrids.Add(splitterGrid);

            foreach (var child in splitterGrid.Children)
            {
                if (child is SplitterPanelControl splitterGridControl)
                {
                    FindContentHostSplitterPanels(inactiveSplitterGrids, splitterGridControl);
                }
            }
        }

        /// <summary>
        /// Get the splitter panels that are content hosts (i.e. the 'leaf node' splitter panels)
        /// </summary>
        public IEnumerable<SplitterPanelControl> GetContentHostSplitterPanels()
        {
            var splitterGridControls = new List<SplitterPanelControl>();

            FindContentHostSplitterPanels(splitterGridControls, this);

            return splitterGridControls;
        }

        private void ShowDesignModePanelOverlay()
        {
            _designModeOverlayGrid.Visibility = Visibility.Visible;
            _designModeOverlayBorder.Visibility = Visibility.Visible;
        }

        private void HideDesignModePanelOverlay()
        {
            _designModeOverlayGrid.Visibility = Visibility.Collapsed;
            _designModeOverlayBorder.Visibility = Visibility.Collapsed;
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

        private void CreateSplitterContextMenu()
        {
            ISplitterPanelDataContextFactory dataContextFactory = GetParentLayout()?.DataContextFactory;
            if (dataContextFactory == null) return;

            // For splitting
            // Available panels are determined by the injected splitter panel data context factory
            var menuFlyout = new MenuFlyout();
            var setPanelMenuItem = new MenuFlyoutSubItem() { Text = "Set Panel Content" };
            menuFlyout.Items.Add(setPanelMenuItem);

            foreach (var supportedDataContext in dataContextFactory.GetSupportedDataContexts())
            {
                var menuFlyoutItem = new MenuFlyoutItem() { Text = supportedDataContext.Key };
                menuFlyoutItem.Click += (s, e) =>
                {
                    DataContext = dataContextFactory.CreateDataContext(supportedDataContext.Value);
                    var dataTemplateSelector = GetParentLayout()?.DataTemplateSelector;

                    // Note: Would use a DataTemplateSelector here, but there seems to be a bug in
                    // Uno's repainting of the content control when the template selector is changed
                    // We therefore for now call the data template selector directly
                    _contentControl.ContentTemplateSelector = dataTemplateSelector;

                    var dataTemplate = dataTemplateSelector?.SelectTemplate(DataContext, this);
                    _contentControl.ContentTemplate = dataTemplate;
                };

                setPanelMenuItem.Items.Add(menuFlyoutItem);
            }

            menuFlyout.Items.Add(new MenuFlyoutSeparator());

            var addPanelMenuSubItem = new MenuFlyoutSubItem() { Text = "Add Panel" };
            menuFlyout.Items.Add(addPanelMenuSubItem);

            void CreateAddPanelSubMenuItem(string description, SplitterPanelPosition position)
            {
                var menuFlyoutItem = new MenuFlyoutItem()
                {
                    Text = description,
                };
                menuFlyoutItem.Click += (s, e) =>
                {
                    if (!IsSplitterActive) GetParentLayout()?.AddSplitterPanel(position);
                };

                addPanelMenuSubItem.Items.Add(menuFlyoutItem);
            }

            CreateAddPanelSubMenuItem("Top", SplitterPanelPosition.Top);
            CreateAddPanelSubMenuItem("Bottom", SplitterPanelPosition.Bottom);
            CreateAddPanelSubMenuItem("Left", SplitterPanelPosition.Left);
            CreateAddPanelSubMenuItem("Right", SplitterPanelPosition.Right);

            var splitPanelSubMenuItem = new MenuFlyoutSubItem() { Text = "Split Panel" };

            void CreateSplitPanelSubMenuItem(string description, SplitterMode mode)
            {
                var splitMenuItem = new MenuFlyoutItem()
                {
                    Text = description
                };

                splitMenuItem.Click += (s, e) =>
                {
                    if (!IsSplitterActive) SplitterMode = mode;
                };

                splitPanelSubMenuItem.Items.Add(splitMenuItem);
            }

            CreateSplitPanelSubMenuItem("Horizontal", SplitterMode.Horizontal);
            CreateSplitPanelSubMenuItem("Vertical", SplitterMode.Vertical);

            menuFlyout.Items.Add(splitPanelSubMenuItem);
            menuFlyout.Items.Add(new MenuFlyoutSeparator());

            var removePanelMenuItem = new MenuFlyoutItem()
            {
                Text = "Remove Panel",
            };
            removePanelMenuItem.Click += (s, e) =>
            {
                if (!IsSplitterActive) RemovePanel();
            };

            // Set the panel context menu
            _designModeOverlayGrid.ContextFlyout = menuFlyout;
        }

        private void DestroySplitterContextMenu()
        {
            // Unassign the context menu
            if (_designModeOverlayGrid != null) _designModeOverlayGrid.ContextFlyout = null;
        }

        private void CreateContentHostUI()
        {
            // If any UI elements dedicated to being a splitter host, remove and unsubscribe them
            if (_gridSplitter != null)
            {
                _gridSplitter.PointerPressed -= OnGridSplitterPointerPressed;
                _gridSplitter = null;
            }

            _firstChildSplitterPanelControl = null;
            _secondChildSplitterPanelControl = null;

            // The splitter grid is to become a content host, so we need to set the panel
            // up with content control and design overlays
            Children.Clear();

            // We never have any rows or columns, just a flat grid with single cell
            RowDefinitions.Clear();
            ColumnDefinitions.Clear();

            // The content control stretches to fill the entire grid
            _contentControl = new ContentControl()
            {
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch
            };

            // The design mode overlay is a semi-transparent overlay that covers the entire panel
            // and shows to the user that the panel is in design mode
            _designModeOverlayGrid = new Grid()
            {
                Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Black),
                Opacity = 0.3,
                Visibility = Visibility.Collapsed
            };

            // The border is capable of highlighting to show that the panel is currently a drop target
            _designModeOverlayBorder = new Border()
            {
                Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent),
                IsHitTestVisible = false,
                BorderThickness = new Thickness(2),
                BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent)
            };

            // Add in correct Z-order (bottom to top)
            Children.Add(_contentControl);
            Children.Add(_designModeOverlayGrid);
            Children.Add(_designModeOverlayBorder);
        }

        private void CreateSplitterUI(SplitterMode splitterMode)
        {
            ISplitterPanelDataContextFactory dataContextFactory = GetParentLayout()?.DataContextFactory;
            if (dataContextFactory == null) return;

            // Obtain and preserve the data context and content template of the currently
            // hosted content
            object currentDataContext = DataContext;

            // Blank on the current hosted content
            DataContext = null;

            // If any UI elements dedicated to being a content host, remove and unsubscribe them
            _contentControl = null;
            _designModeOverlayGrid = null;
            _designModeOverlayBorder = null;

            Children.Clear();

            // The grid splitter is to become a host for two child splitter grids, either in
            // horizontal or vertical orientation
            if (splitterMode == SplitterMode.Horizontal)
            {
                CreateHorizontalSplit(currentDataContext);
            }
            else
            {
                CreateVerticalSplit(currentDataContext);
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

        /// <summary>
        /// Recursively sets the data template selector of all grid splitters in the panel,
        /// including on child splitter grid controls
        /// </summary>
        /// <param name="dataTemplateSelector">The data template selector that the splitter grid control should use</param>
        public void SetDataTemplateSelector(DataTemplateSelector dataTemplateSelector)
        {
            if (_contentControl != null)
            {
                // Note: pending a fix to the redraw mechanism when we change the content
                // template selector, we call it directly here on the current data context
                _contentControl.ContentTemplateSelector = dataTemplateSelector;
                _contentControl.ContentTemplate = dataTemplateSelector?.SelectTemplate(DataContext);
            }

            _firstChildSplitterPanelControl?.SetDataTemplateSelector(dataTemplateSelector);
            _secondChildSplitterPanelControl?.SetDataTemplateSelector(dataTemplateSelector);
        }

        private SplitterPanelControl CreateChildSplitterPanelControl(object dataContext)
        {
            var splitterPanelControl = new SplitterPanelControl();

            // Add the splitter grid control to the panel as a child
            Children.Add(splitterPanelControl);

            // Set the data context and data template selector
            var contentTemplateSelector = GetParentLayout()?.DataTemplateSelector;
            splitterPanelControl._contentControl.ContentTemplateSelector = contentTemplateSelector;
            splitterPanelControl._contentControl.ContentTemplate = contentTemplateSelector?.SelectTemplate(dataContext, this);
            splitterPanelControl.DataContext = dataContext;

            // Synchronize the design mode state
            splitterPanelControl.ToggleDesignMode(DesignMode);

            return splitterPanelControl;
        }

        private void CreateHorizontalSplit(object dataContext)
        {
            RowDefinition CreateRowDefinition(string gridLengthPropertyName)
            {
                var rowDefinition = new RowDefinition();

                rowDefinition.SetBinding(RowDefinition.HeightProperty,
                    new Microsoft.UI.Xaml.Data.Binding()
                    {
                        Source = this,
                        Path = new PropertyPath($"{gridLengthPropertyName}"),
                        Mode = Microsoft.UI.Xaml.Data.BindingMode.TwoWay,
                        Converter = new GridLengthConverter()
                    });

                return rowDefinition;
            }

            RowDefinitions.Clear();

            // Create the row definitions for the horizontal split, one for each child
            RowDefinitions.Add(CreateRowDefinition(nameof(FirstChildProportionalSize)));
            RowDefinitions.Add(CreateRowDefinition(nameof(SecondChildProportionalSize)));

            // Add the first child splitter to the first row
            _firstChildSplitterPanelControl = CreateChildSplitterPanelControl(dataContext);

            // And finally the second splitter control to the second row
            _secondChildSplitterPanelControl = CreateChildSplitterPanelControl(null);
            Grid.SetRow(_secondChildSplitterPanelControl, 1);

            double gridSplitterThickness = GetParentLayout()?.GridSplitterThickness ?? DefaultSplitterThickness;
            _secondChildSplitterPanelControl.Margin = new Thickness(0, gridSplitterThickness, 0, 0);

            // And the grid splitter to the second row
            _gridSplitter = new GridSplitter()
            {
                MinHeight = gridSplitterThickness,
                Height = gridSplitterThickness,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Top,
                ResizeDirection = GridSplitter.GridResizeDirection.Rows,
                Element = null
            };

            // Handle the pointer pressed event to prevent panels from responding
            // and starting a drag operation
            _gridSplitter.PointerPressed += OnGridSplitterPointerPressed;

            Children.Add(_gridSplitter);
            Grid.SetRow(_gridSplitter, 1);
        }

        private void CreateVerticalSplit(object dataContext)
        {
            ColumnDefinition CreateColumnDefinition(string gridColumnPropertyName)
            {
                var columnDefinition = new ColumnDefinition();

                columnDefinition.SetBinding(ColumnDefinition.WidthProperty,
                    new Microsoft.UI.Xaml.Data.Binding()
                    {
                        Source = this,
                        Path = new PropertyPath($"{gridColumnPropertyName}"),
                        Mode = Microsoft.UI.Xaml.Data.BindingMode.TwoWay,
                        Converter = new GridLengthConverter()
                    });

                return columnDefinition;
            }

            ColumnDefinitions.Clear();

            // We now need two columns in the splitter grid, and we place the splitter
            // in the middle assigned to the second column
            ColumnDefinitions.Add(CreateColumnDefinition(nameof(FirstChildProportionalSize)));
            ColumnDefinitions.Add(CreateColumnDefinition(nameof(SecondChildProportionalSize)));

            _firstChildSplitterPanelControl = CreateChildSplitterPanelControl(dataContext);

            // And finally the second splitter control to the second column
            _secondChildSplitterPanelControl = CreateChildSplitterPanelControl(null);
            Grid.SetColumn(_secondChildSplitterPanelControl, 1);

            double gridSplitterThickness = GetParentLayout()?.GridSplitterThickness ?? DefaultSplitterThickness;
            _secondChildSplitterPanelControl.Margin = new Thickness(gridSplitterThickness, 0, 0, 0);

            // Add the grid splitter last to ensure it is on top of the other controls
            _gridSplitter = new GridSplitter()
            {
                MinWidth = gridSplitterThickness,
                Width = gridSplitterThickness,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Stretch,
                ResizeDirection = GridSplitter.GridResizeDirection.Columns,
                Element = null
            };

            // Handle the pointer pressed event to prevent panels from responding
            // and starting a drag operation
            _gridSplitter.PointerPressed += OnGridSplitterPointerPressed;

            // And the grid splitter to the second column
            Children.Add(_gridSplitter);
            Grid.SetColumn(_gridSplitter, 1);
        }

        private void ReplaceChildSplitterControl(ref SplitterPanelControl currentSplitterGridControl, SplitterPanelControl newSplitterGridControl, double proportion)
        {
            int replaceIndex = Children.IndexOf(currentSplitterGridControl);
            if (replaceIndex == -1) return;

            if (replaceIndex == Children.Count - 1)
            {
                // The grid splitter is the last child, so just remove it
                Children.Add(newSplitterGridControl);
                Children.Remove(currentSplitterGridControl);
            }
            else
            {
                // The grid splitter is not the last child, so we need to insert the new splitter grid control
                // at the same index as the old one
                Children.Insert(replaceIndex, newSplitterGridControl);
                Children.Remove(currentSplitterGridControl);
            }

            currentSplitterGridControl = newSplitterGridControl;

            FirstChildProportionalSize = 1.0 - proportion;
            SecondChildProportionalSize = proportion;

            currentSplitterGridControl.ToggleDesignMode(DesignMode);
        }

        /// <summary>
        /// Replaces the first child grid splitter with the new splitter grid control
        /// </summary>
        /// <param name="newFirstChildSplitterGridControl"></param>
        public void ReplaceFirstChildGridSplitter(SplitterPanelControl newFirstChildSplitterGridControl, double proportion = 0.5)
        {
            ReplaceChildSplitterControl(ref _firstChildSplitterPanelControl, newFirstChildSplitterGridControl, proportion);
        }

        /// <summary>
        /// Replaces the second child grid splitter with the new splitter grid control
        /// </summary>
        /// <param name="newSecondChildSplitterGridControl"></param>
        public void ReplaceSecondChildGridSplitter(SplitterPanelControl newSecondChildSplitterGridControl, double proportion = 0.5)
        {
            ReplaceChildSplitterControl(ref _secondChildSplitterPanelControl, newSecondChildSplitterGridControl, proportion);

            double gridSplitterThickness = GetParentLayout()?.GridSplitterThickness ?? DefaultSplitterThickness;

            // Ensure row or column settings
            if (SplitterMode == SplitterMode.Horizontal)
            {
                Grid.SetRow(_secondChildSplitterPanelControl, 1);
                _secondChildSplitterPanelControl.Margin = new Thickness(0, gridSplitterThickness, 0, 0);
            }
            else if (SplitterMode == SplitterMode.Vertical)
            {
                Grid.SetColumn(_secondChildSplitterPanelControl, 1);
                _secondChildSplitterPanelControl.Margin = new Thickness(gridSplitterThickness, 0, 0, 0);
            }
        }

        private void OnGridSplitterPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            // Ensure handled to prevent the grid splitter from stealing focus
            e.Handled = true;
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

        private void RemoveChildPanel(SplitterPanelControl childSplitterGridControl)
        {
            SplitterPanelControl remainingGridControl = Children.OfType<SplitterPanelControl>().FirstOrDefault(c => c != childSplitterGridControl);

            if (remainingGridControl != null)
            {
                RowDefinitions.Clear();
                ColumnDefinitions.Clear();

                // Remove the now redundant grid splitter
                _gridSplitter.PointerPressed -= OnGridSplitterPointerPressed;
                Children.Remove(_gridSplitter);

                _gridSplitter = null;

                if (!remainingGridControl.IsSplitterActive)
                {
                    object remainingDataContext = remainingGridControl.DataContext;

                    // Ensure we now become a content host panel
                    SetSplitterMode(SplitterMode.ContentHost);

                    // And set the data context and content template back to this grid control
                    DataContext = remainingDataContext;
                }
                else
                {
                    // The remaining child splitter grid control is itself split, so we merge all
                    // its children into this grid control's children to make this splitter grid
                    // control effectively replace it
                    MergeInPanelElements(remainingGridControl);
                }
            }

            // Should be in design mode otherwise we would not have been asked to remove a child
            ToggleDesignMode(true);
        }

        internal SplitterPanelControl FirstChildSplitterPanelControl => _firstChildSplitterPanelControl;

        internal SplitterPanelControl SecondChildSplitterPanelControl => _secondChildSplitterPanelControl;

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
