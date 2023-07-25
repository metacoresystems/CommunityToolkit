using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using SplitterGrid.Converters;
using Windows.Foundation;
using Windows.UI.Composition;
using Windows.UI.Core;

namespace SplitterGrid
{
    /// <summary>
    /// The core splitter grid control panel, which can be nested within other splitter grids
    /// </summary>
    public class SplitterGridControl : Grid
    {
        private readonly SplitterPanelContainerInfo _containerInfo;

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
        private SplitterGridControl _firstChildSplitterGrid;
        private SplitterGridControl _secondChildSplitterGrid;

        /// <summary>
        /// Creates a splitter grid control as a top level grid splitter (no parent)
        /// </summary>
        public SplitterGridControl()
        {
            _containerInfo = new SplitterPanelContainerInfo(null);
            SetSplitterMode(SplitterMode.ContentHost);    // Always start as a content host
        }

        /// <summary>
        /// Creates a splitter grid control as a nest grid splitter (has a parent)
        /// </summary>
        /// <param name="parentContainerInfo">The parent container info</param>
        public SplitterGridControl(SplitterPanelContainerInfo parentContainerInfo)
        {
            if (parentContainerInfo == null) throw new ArgumentNullException(nameof(parentContainerInfo));

            _containerInfo = new SplitterPanelContainerInfo(parentContainerInfo);
            SetSplitterMode(SplitterMode.ContentHost);    // Always start as a content host
        }

        private SplitterGridLayout GetParentLayout()
        {
            FrameworkElement parent = Parent as FrameworkElement;

            while (parent != null)
            {
                if (parent is SplitterGridLayout splitterGridLayout)
                {
                    return splitterGridLayout;
                }

                parent = parent.Parent as FrameworkElement;
            }

            return null;
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

        /// <summary>
        /// Toggles the visual UI of the panel to indicate whether the panel is
        /// considered drop active or not
        /// </summary>
        public void SetDropActive(bool active)
        {
            if (_containerInfo.IsSplitterActive) return;

            IsDropActive = active;

            if (active)
            {
                _designModeOverlayBorder.BorderBrush = new TextBox().SelectionHighlightColor;
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

        private void SplitterGridControl_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            // Nothing to do if not in design mode
            if (!DesignMode) return;

            SplitterGridLayout parentLayout = GetParentLayout();
            var pointerProperties = e.GetCurrentPoint(this).Properties;

            // In any circumstance, cannot drag if not in contact (mouse button pressed, touch point down etc.)
            if (!e.Pointer.IsInContact || !pointerProperties.IsLeftButtonPressed) return;

            // Ensure pointer is captured
            if (CapturePointer(e.Pointer))
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

        private void SplitterGridControl_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (!DesignMode) return;

            if (_dragPreviewGrid == null) return;

            SplitterGridLayout parentLayout = GetParentLayout();
            Point currentPoint = e.GetCurrentPoint(parentLayout).Position;

            _dragPreviewGrid.RenderTransform = new Microsoft.UI.Xaml.Media.TranslateTransform()
            {
                X = currentPoint.X,
                Y = currentPoint.Y
            };

            parentLayout.OnCapturedPointerMove(this, e);

            e.Handled = true;
        }

        private void SplitterGridControl_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (!DesignMode) return;
            if (_dragPreviewGrid == null) return;

            if (PointerCaptures.Contains(e.Pointer))
            {
                _dragStart = null;

                ReleasePointerCapture(e.Pointer);

                SplitterGridLayout parentLayout = GetParentLayout();

                parentLayout.OnCapturedPointerReleased(this, e);
                parentLayout.RemoveDragPreview(_dragPreviewGrid);
                _dragPreviewGrid = null;

                SplitterUtilities.SetCurrentCursor(CoreCursorType.Arrow);
            }
        }

        private void FindSplitterLeafGrids(List<SplitterGridControl> inactiveSplitterGrids, SplitterGridControl splitterGrid)
        {
            if (!splitterGrid._containerInfo.IsSplitterActive)
                inactiveSplitterGrids.Add(splitterGrid);

            foreach (var child in splitterGrid.Children)
            {
                if (child is SplitterGridControl splitterGridControl)
                {
                    FindSplitterLeafGrids(inactiveSplitterGrids, splitterGridControl);
                }
            }
        }

        public IEnumerable<SplitterGridControl> GetLeafSplitterGrids()
        {
            var splitterGridControls = new List<SplitterGridControl>();

            FindSplitterLeafGrids(splitterGridControls, this);

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

        /// <summary>
        /// Shows or hides the UI overlays for when the panel is in design mode
        /// </summary>
        public void ToggleDesignMode(bool enabled)
        {
            if (enabled)
            {
                if (!_containerInfo.IsSplitterActive)
                {
                    ShowDesignModePanelOverlay();
                    CreateSplitterContextMenu();

                    // No need to allow drag repositioning if this is the only top level splitter
                    if (!_containerInfo.IsTopLevel)
                    {
                        PointerPressed += SplitterGridControl_PointerPressed;
                        PointerReleased += SplitterGridControl_PointerReleased;
                        PointerMoved += SplitterGridControl_PointerMoved;
                    }
                }
            }
            else
            {
                if (!_containerInfo.IsSplitterActive)
                {
                    if (!_containerInfo.IsTopLevel)
                    {
                        PointerPressed -= SplitterGridControl_PointerPressed;
                        PointerReleased -= SplitterGridControl_PointerReleased;
                        PointerMoved -= SplitterGridControl_PointerMoved;
                    }

                    DestroySplitterContextMenu();
                    HideDesignModePanelOverlay();
                }
            }

            if (_gridSplitter == null) return;

            _gridSplitter.IsEnabled = enabled;

            var childGridSplitters = Children.OfType<SplitterGridControl>().ToList();

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
                setPanelMenuItem.Items.Add(new MenuFlyoutItem()
                {
                    Text = supportedDataContext.Key,
                    Command = new RelayCommand(() =>
                    {
                        DataContext = dataContextFactory.CreateDataContext(supportedDataContext.Value);
                        _contentControl.ContentTemplateSelector = GetParentLayout()?.DataTemplateSelector;
                    },
                    () => !_containerInfo.IsSplitterActive)
                });
            }

            menuFlyout.Items.Add(new MenuFlyoutSeparator());

            var addPanelMenuSubItem = new MenuFlyoutSubItem() { Text = "Add Panel" };
            menuFlyout.Items.Add(addPanelMenuSubItem);

            void CreateAddPanelSubMenuItem(string description, SplitterEdgeKind edgeKind)
            {
                addPanelMenuSubItem.Items.Add(new MenuFlyoutItem()
                {
                    Text = description,
                    Command = new RelayCommand(() =>
                    {
                        GetParentLayout()?.AddSplitterEdge(edgeKind);
                    }),
                });
            }

            CreateAddPanelSubMenuItem("Top", SplitterEdgeKind.Top);
            CreateAddPanelSubMenuItem("Bottom", SplitterEdgeKind.Bottom);
            CreateAddPanelSubMenuItem("Left", SplitterEdgeKind.Left);
            CreateAddPanelSubMenuItem("Right", SplitterEdgeKind.Right);

            var splitPanelSubMenuItem = new MenuFlyoutSubItem() { Text = "Split Panel" };

            void CreateSplitPanelSubMenuItem(string description, SplitterMode mode)
            {
                var splitMenuItem = new MenuFlyoutItem()
                {
                    Text = description,
                    Command = new RelayCommand(() => SetSplitterMode(mode), () => !_containerInfo.IsSplitterActive)
                };

                splitPanelSubMenuItem.Items.Add(splitMenuItem);
            }

            CreateSplitPanelSubMenuItem("Horizontal", SplitterMode.Horizontal);
            CreateSplitPanelSubMenuItem("Vertical", SplitterMode.Vertical);

            menuFlyout.Items.Add(splitPanelSubMenuItem);
            menuFlyout.Items.Add(new MenuFlyoutSeparator());
            menuFlyout.Items.Add(new MenuFlyoutItem()
            {
                Text = "Remove Panel",
                Command = new RelayCommand(() => RemovePanel(), () => !_containerInfo.IsSplitterActive)
            });

            // Set the panel context menu
            _designModeOverlayGrid.ContextFlyout = menuFlyout;
        }

        private void DestroySplitterContextMenu()
        {
            // Unassign the context menu
            _designModeOverlayGrid.ContextFlyout = null;
        }

        private void CreateContentHostUI()
        {
            // If any UI elements dedicated to being a splitter host, remove and unsubscribe them
            if (_gridSplitter != null)
            {
                _gridSplitter.PointerPressed -= OnGridSplitterPointerPressed;
                _gridSplitter = null;
            }

            _firstChildSplitterGrid = null;
            _secondChildSplitterGrid = null;

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

        public void SetSplitterMode(SplitterMode splitterMode)
        {
            if (splitterMode == SplitterMode.ContentHost)
            {
                CreateContentHostUI();
            }
            else
            {
                CreateSplitterUI(splitterMode);
            }

            // Update the serializable container info
            _containerInfo.SetSplitterMode(splitterMode);
        }

        /// <summary>
        /// Recursively sets the thickness of all grid splitters in the panel,
        /// including on child splitter grid controls
        /// </summary>
        /// <param name="thickness"></param>
        public void SetGridSplitterThickness(double thickness)
        {
            // If a content host, we don't have a grid splitter
            if (_containerInfo.SplitterInfo.Mode == SplitterMode.ContentHost) return;

            if (_containerInfo.SplitterInfo.Mode == SplitterMode.Horizontal)
            {
                _gridSplitter.Height = thickness;
            }
            else if (_containerInfo.SplitterInfo.Mode == SplitterMode.Vertical)
            {
                _gridSplitter.Width = thickness;
            }

            _firstChildSplitterGrid?.SetGridSplitterThickness(thickness);
            _secondChildSplitterGrid?.SetGridSplitterThickness(thickness);
        }

        /// <summary>
        /// Recursively sets the data template selector of all grid splitters in the panel,
        /// including on child splitter grid controls
        /// </summary>
        /// <param name="dataTemplateSelector">The data template selector that the splitter grid control should use</param>
        public void SetGridSplitterDataTemplateSelector(DataTemplateSelector dataTemplateSelector)
        {
            if (_contentControl != null) _contentControl.ContentTemplateSelector = dataTemplateSelector;
            _firstChildSplitterGrid?.SetGridSplitterDataTemplateSelector(dataTemplateSelector);
            _secondChildSplitterGrid?.SetGridSplitterDataTemplateSelector(dataTemplateSelector);
        }

        private SplitterGridControl CreateChildSplitterControl(object dataContext)
        {
            var splitterGridControl = new SplitterGridControl(_containerInfo);

            splitterGridControl.DataContext = dataContext;
            splitterGridControl._contentControl.ContentTemplateSelector = GetParentLayout()?.DataTemplateSelector;

            Children.Add(splitterGridControl);

            splitterGridControl.ToggleDesignMode(DesignMode);

            return splitterGridControl;
        }

        private void CreateHorizontalSplit(object dataContext)
        {
            RowDefinition CreateRowDefinition(string gridLengthPropertyName)
            {
                var rowDefinition = new RowDefinition();

                rowDefinition.SetBinding(RowDefinition.HeightProperty,
                    new Microsoft.UI.Xaml.Data.Binding()
                    {
                        Source = _containerInfo,
                        Path = new PropertyPath($"{nameof(SplitterPanelContainerInfo.SplitterInfo)}.{gridLengthPropertyName}"),
                        Mode = Microsoft.UI.Xaml.Data.BindingMode.TwoWay,
                        Converter = new GridLengthConverter()
                    });

                return rowDefinition;
            }

            // Create the row definitions for the horizontal split, one for each child
            RowDefinitions.Add(CreateRowDefinition(nameof(SplitterInfo.FirstChildGridLength)));
            RowDefinitions.Add(CreateRowDefinition(nameof(SplitterInfo.SecondChildGridLength)));

            // Add the first child splitter to the first row
            _firstChildSplitterGrid = CreateChildSplitterControl(dataContext);

            // And finally the second splitter control to the second row
            _secondChildSplitterGrid = CreateChildSplitterControl(null);

            Grid.SetRow(_secondChildSplitterGrid, 1);
            Children.Add(_secondChildSplitterGrid);

            double gridSplitterThickness = GetParentLayout()?.GridSplitterThickness ?? DefaultSplitterThickness;
            _secondChildSplitterGrid.Margin = new Thickness(0, gridSplitterThickness, 0, 0);

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

            Children.Insert(1, _gridSplitter);
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
                        Source = _containerInfo,
                        Path = new PropertyPath($"{nameof(SplitterPanelContainerInfo.SplitterInfo)}.{gridColumnPropertyName}"),
                        Mode = Microsoft.UI.Xaml.Data.BindingMode.TwoWay,
                        Converter = new GridLengthConverter()
                    });

                return columnDefinition;
            }

            // We now need two columns in the splitter grid, and we place the splitter
            // in the middle assigned to the second column
            ColumnDefinitions.Add(CreateColumnDefinition(nameof(SplitterInfo.FirstChildGridLength)));
            ColumnDefinitions.Add(CreateColumnDefinition(nameof(SplitterInfo.SecondChildGridLength)));

            _firstChildSplitterGrid = CreateChildSplitterControl(dataContext);

            // And finally the second splitter control to the second column
            _secondChildSplitterGrid = CreateChildSplitterControl(null);

            Grid.SetColumn(_secondChildSplitterGrid, 1);

            double gridSplitterThickness = GetParentLayout()?.GridSplitterThickness ?? DefaultSplitterThickness;
            _secondChildSplitterGrid.Margin = new Thickness(gridSplitterThickness, 0, 0, 0);

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
            Children.Insert(1, _gridSplitter);
            Grid.SetColumn(_gridSplitter, 1);
        }

        private void ReplaceChildSplitterControl(ref SplitterGridControl currentSplitterGridControl, SplitterGridControl newSplitterGridControl, double proportion)
        {
            if (currentSplitterGridControl == null) return;

            int replaceIndex = Children.IndexOf(currentSplitterGridControl);
            if (replaceIndex == -1) return;

            if (replaceIndex == Children.Count - 1)
            {
                // The grid splitter is the last child, so just remove it
                Children.RemoveAt(replaceIndex);
                Children.Add(newSplitterGridControl);
            }
            else
            {
                // The grid splitter is not the last child, so we need to insert the new splitter grid control
                // at the same index as the old one
                Children.RemoveAt(replaceIndex);
                Children.Insert(replaceIndex, newSplitterGridControl);
            }

            currentSplitterGridControl = newSplitterGridControl;

            _containerInfo.SplitterInfo.FirstChildGridLength = 1.0 - proportion;
            _containerInfo.SplitterInfo.SecondChildGridLength = proportion;

            currentSplitterGridControl.ToggleDesignMode(DesignMode);
        }

        /// <summary>
        /// Replaces the first child grid splitter with the new splitter grid control
        /// </summary>
        /// <param name="newFirstChildSplitterGridControl"></param>
        public void ReplaceFirstChildGridSplitter(SplitterGridControl newFirstChildSplitterGridControl, double proportion = 0.5)
        {
            ReplaceChildSplitterControl(ref _firstChildSplitterGrid, newFirstChildSplitterGridControl, proportion);
        }

        /// <summary>
        /// Replaces the second child grid splitter with the new splitter grid control
        /// </summary>
        /// <param name="newSecondChildSplitterGridControl"></param>
        public void ReplaceSecondChildGridSplitter(SplitterGridControl newSecondChildSplitterGridControl, double proportion = 0.5)
        {
            ReplaceChildSplitterControl(ref _secondChildSplitterGrid, newSecondChildSplitterGridControl, proportion);

            double gridSplitterThickness = GetParentLayout()?.GridSplitterThickness ?? DefaultSplitterThickness;

            // Ensure row or column settings
            if (_containerInfo.SplitterInfo.Mode == SplitterMode.Horizontal)
            {
                Grid.SetRow(_secondChildSplitterGrid, 1);
                _secondChildSplitterGrid.Margin = new Thickness(0, gridSplitterThickness, 0, 0);
                Children.Add(_secondChildSplitterGrid);
            }
            else if (_containerInfo.SplitterInfo.Mode == SplitterMode.Vertical)
            {
                Grid.SetColumn(_secondChildSplitterGrid, 1);
                _secondChildSplitterGrid.Margin = new Thickness(gridSplitterThickness, 0, 0, 0);
                Children.Add(_secondChildSplitterGrid);
            }
        }

        private void OnGridSplitterPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            // Ensure handled to prevent the grid splitter from stealing focus
            e.Handled = true;
        }
        
        private void RemovePanel()
        {
            if (_containerInfo.IsSplitterActive || _containerInfo.IsTopLevel) return;
            
            // Remove this panel from the parent splitter grid and then 'unsplit' the parent splitter
            if (Parent is SplitterGridControl parentSplitterGridControl)
            {
                parentSplitterGridControl.RemoveChildPanel(this);
            }
        }

        private void MergeInPanelElements(SplitterGridControl remainingGridControl)
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
            if (remainingGridControl._containerInfo.SplitterInfo.Mode == SplitterMode.Horizontal)
            {
                _containerInfo.SetSplitterMode(SplitterMode.Horizontal);

                foreach (var rowDefinition in remainingGridControl.RowDefinitions)
                {
                    RowDefinitions.Add(rowDefinition);
                }
            }
            else if (remainingGridControl._containerInfo.SplitterInfo.Mode == SplitterMode.Vertical)
            {
                _containerInfo.SetSplitterMode(SplitterMode.Vertical);

                foreach (var columnDefinition in remainingGridControl.ColumnDefinitions)
                {
                    ColumnDefinitions.Add(columnDefinition);
                }
            }
        }

        private void RemoveChildPanel(SplitterGridControl childSplitterGridControl)
        {
            SplitterGridControl remainingGridControl = Children.OfType<SplitterGridControl>().FirstOrDefault(c => c != childSplitterGridControl);

            if (remainingGridControl != null)
            {
                RowDefinitions.Clear();
                ColumnDefinitions.Clear();

                // Remove the now redundant grid splitter
                _gridSplitter.PointerPressed -= OnGridSplitterPointerPressed;
                Children.Remove(_gridSplitter);

                _gridSplitter = null;

                if (!remainingGridControl._containerInfo.IsSplitterActive)
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
    }
}
