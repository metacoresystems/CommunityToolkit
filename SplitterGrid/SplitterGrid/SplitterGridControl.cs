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
                    Text = supportedDataContext.Item1,
                    Command = new RelayCommand(() =>
                    {
                        DataContext = dataContextFactory.CreateDataContext(supportedDataContext.Item2);
                        _contentControl.ContentTemplate = supportedDataContext.Item3;
                    },
                    () => !_containerInfo.IsSplitterActive)
                });
            }

            menuFlyout.Items.Add(new MenuFlyoutSeparator());

            var addPanelMenuSubItem = new MenuFlyoutSubItem() { Text = "Add Edge Panel" };
            menuFlyout.Items.Add(addPanelMenuSubItem);

            addPanelMenuSubItem.Items.Add(new MenuFlyoutItem()
            {
                Text = "Top",
                Command = new RelayCommand(() =>
                {
                    GetParentLayout()?.AddSplitterEdge(SplitterEdgeKind.Top);
                }),
            });

            addPanelMenuSubItem.Items.Add(new MenuFlyoutItem()
            {
                Text = "Bottom",
                Command = new RelayCommand(() =>
                {
                    GetParentLayout()?.AddSplitterEdge(SplitterEdgeKind.Bottom);
                }),
            });

            addPanelMenuSubItem.Items.Add(new MenuFlyoutItem()
            {
                Text = "Left",
                Command = new RelayCommand(() =>
                {
                    GetParentLayout()?.AddSplitterEdge(SplitterEdgeKind.Left);
                }),
            });

            addPanelMenuSubItem.Items.Add(new MenuFlyoutItem()
            {
                Text = "Right",
                Command = new RelayCommand(() =>
                {
                    GetParentLayout()?.AddSplitterEdge(SplitterEdgeKind.Right);
                }),
            });

            var splitPanelSubMenuItem = new MenuFlyoutSubItem() { Text = "Split Panel" };          

            var splitHorizontallyMenuItem = new MenuFlyoutItem()
            {
                Text = "Horizontal",
                Command = new RelayCommand(() => SetSplitterMode(SplitterMode.Horizontal), () => !_containerInfo.IsSplitterActive)
            };

            splitPanelSubMenuItem.Items.Add(splitHorizontallyMenuItem);

            var splitVerticallyMenuItem = new MenuFlyoutItem()
            {
                Text = "Vertical",
                Command = new RelayCommand(() => SetSplitterMode(SplitterMode.Vertical), () => !_containerInfo.IsSplitterActive)
            };

            splitPanelSubMenuItem.Items.Add(splitVerticallyMenuItem);

            menuFlyout.Items.Add(splitPanelSubMenuItem);

            menuFlyout.Items.Add(new MenuFlyoutSeparator());

            menuFlyout.Items.Add(new MenuFlyoutItem()
            {
                Text = "Remove Panel",
                Command = new RelayCommand(() => RemovePanel(), () => !_containerInfo.IsSplitterActive)
            });

            _designModeOverlayGrid.ContextFlyout = menuFlyout;
        }

        private void DestroySplitterContextMenu()
        {
            // Unassign the context menu
            _designModeOverlayGrid.ContextFlyout = null;
        }

        public void SetSplitterMode(SplitterMode splitterMode)
        {
            if (splitterMode == SplitterMode.ContentHost)
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

                _contentControl = new ContentControl()
                {
                    HorizontalContentAlignment = HorizontalAlignment.Stretch,
                    VerticalContentAlignment = VerticalAlignment.Stretch
                };

                _designModeOverlayGrid = new Grid()
                {
                    Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Black),
                    Opacity = 0.3,
                    Visibility = Visibility.Collapsed
                };

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
            else
            {
                ISplitterPanelDataContextFactory dataContextFactory = GetParentLayout()?.DataContextFactory;
                if (dataContextFactory == null) return;

                // Obtain and preserve the data context and content template of the currently
                // hosted content
                object currentDataContext = DataContext;
                DataTemplate currentDataTemplate = ContentControl.ContentTemplate;

                // Blank on the current hosted content
                DataContext = null;
                ContentControl.ContentTemplate = null;

                // If any UI elements dedicated to being a content host, remove and unsubscribe them
                _contentControl = null;
                _designModeOverlayGrid = null;
                _designModeOverlayBorder = null;

                Children.Clear();

                // The grid splitter is to become a host for two child splitter grids, either in
                // horizontal or vertical orientation
                if (splitterMode == SplitterMode.Horizontal)
                {
                    CreateHorizontalSplit(currentDataContext, currentDataTemplate);
                }
                else
                {
                    CreateVerticalSplit(currentDataContext, currentDataTemplate);
                }
            }

            // Update the serializable container info
            _containerInfo.SetSplitterMode(splitterMode);
        }

        private void CreateHorizontalSplit(object dataContext, DataTemplate dataTemplate)
        {
            var firstRowDefinition = new RowDefinition();

            firstRowDefinition.SetBinding(RowDefinition.HeightProperty,
                new Microsoft.UI.Xaml.Data.Binding()
                {
                    Source = _containerInfo,
                    Path = new PropertyPath($"{nameof(SplitterPanelContainerInfo.SplitterInfo)}.{nameof(SplitterInfo.FirstChildGridLength)}"),
                    Mode = Microsoft.UI.Xaml.Data.BindingMode.TwoWay,
                    Converter = new GridLengthConverter()
                });

            // We now need two rows in the splitter grid, and we place the splitter
            // in the middle assigned to the second row
            var secondRowDefinition = new RowDefinition();

            secondRowDefinition.SetBinding(RowDefinition.HeightProperty,
                new Microsoft.UI.Xaml.Data.Binding()
                {
                    Source = _containerInfo,
                    Path = new PropertyPath($"{nameof(SplitterPanelContainerInfo.SplitterInfo)}.{nameof(SplitterInfo.SecondChildGridLength)}"),
                    Mode = Microsoft.UI.Xaml.Data.BindingMode.TwoWay,
                    Converter = new GridLengthConverter()
                });

            // Add the two row definitions
            RowDefinitions.Add(firstRowDefinition);
            RowDefinitions.Add(secondRowDefinition);

            // Add the first child splitter to the first row
            _firstChildSplitterGrid = new SplitterGridControl(_containerInfo);

            // And assign to first child now instead
            _firstChildSplitterGrid.DataContext = dataContext;
            _firstChildSplitterGrid.ContentControl.ContentTemplate = dataTemplate;

            Children.Add(_firstChildSplitterGrid);
            _firstChildSplitterGrid.ToggleDesignMode(DesignMode);

            // And finally the second splitter control to the second row
            _secondChildSplitterGrid = new SplitterGridControl(_containerInfo);

            Grid.SetRow(_secondChildSplitterGrid, 1);
            _secondChildSplitterGrid.Margin = new Thickness(0, 5, 0, 0);
            Children.Add(_secondChildSplitterGrid);
            _secondChildSplitterGrid.ToggleDesignMode(DesignMode);

            // And the grid splitter to the second row
            _gridSplitter = new GridSplitter()
            {
                MinHeight = 5,
                Height = 5,
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

        private void CreateVerticalSplit(object dataContext, DataTemplate dataTemplate)
        {
            // We now need two columns in the splitter grid, and we place the splitter
            // in the middle assigned to the second column
            var firstColumnDefinition = new ColumnDefinition();

            firstColumnDefinition.SetBinding(ColumnDefinition.WidthProperty,
                new Microsoft.UI.Xaml.Data.Binding()
                {
                    Source = _containerInfo,
                    Path = new PropertyPath($"{nameof(SplitterPanelContainerInfo.SplitterInfo)}.{nameof(SplitterInfo.FirstChildGridLength)}"),
                    Mode = Microsoft.UI.Xaml.Data.BindingMode.TwoWay,
                    Converter = new GridLengthConverter()
                });

            var secondColumnDefinition = new ColumnDefinition();

            // Bind the column definition to the proportion. This will drive the width of
            // the two columns by the splitter
            secondColumnDefinition.SetBinding(ColumnDefinition.WidthProperty,
                new Microsoft.UI.Xaml.Data.Binding()
                {
                    Source = _containerInfo,
                    Path = new PropertyPath($"{nameof(SplitterPanelContainerInfo.SplitterInfo)}.{nameof(SplitterInfo.SecondChildGridLength)}"),
                    Mode = Microsoft.UI.Xaml.Data.BindingMode.TwoWay,
                    Converter = new GridLengthConverter()
                });

            // Add the two column definitions
            ColumnDefinitions.Add(firstColumnDefinition);
            ColumnDefinitions.Add(secondColumnDefinition);

            _firstChildSplitterGrid = new SplitterGridControl(_containerInfo);

            // Reassign to the new splitter grid on the left hand side
            _firstChildSplitterGrid.DataContext = dataContext;
            _firstChildSplitterGrid.ContentControl.ContentTemplate = dataTemplate;

            Children.Add(_firstChildSplitterGrid);
            _firstChildSplitterGrid.ToggleDesignMode(DesignMode);

            // And finally the second splitter control to the second column
            _secondChildSplitterGrid = new SplitterGridControl(_containerInfo);

            Grid.SetColumn(_secondChildSplitterGrid, 1);
            _secondChildSplitterGrid.Margin = new Thickness(5, 0, 0, 0);
            Children.Add(_secondChildSplitterGrid);
            _secondChildSplitterGrid.ToggleDesignMode(DesignMode);

            // Add the grid splitter last to ensure it is on top of the other controls
            _gridSplitter = new GridSplitter()
            {
                MinWidth = 5,
                Width = 5,
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

        /// <summary>
        /// Replaces the first child grid splitter with the new splitter grid control
        /// </summary>
        /// <param name="newFirstChildSplitterGridControl"></param>
        public void ReplaceFirstChildGridSplitter(SplitterGridControl newFirstChildSplitterGridControl, double proportion = 0.5)
        {
            if (_firstChildSplitterGrid == null) return;

            int replaceIndex = Children.IndexOf(_firstChildSplitterGrid);
            if (replaceIndex == -1) return;

            if (replaceIndex == Children.Count - 1)
            {
                // The grid splitter is the last child, so just remove it
                Children.RemoveAt(replaceIndex);
                Children.Add(newFirstChildSplitterGridControl);
            }
            else
            {
                // The grid splitter is not the last child, so replace it with the new splitter
                Children.RemoveAt(replaceIndex);
                Children.Insert(replaceIndex, newFirstChildSplitterGridControl);
            }

            _firstChildSplitterGrid = newFirstChildSplitterGridControl;

            ContainerInfo.SplitterInfo.FirstChildGridLength = proportion;
            ContainerInfo.SplitterInfo.SecondChildGridLength = 1.0 - proportion;

            _firstChildSplitterGrid.ToggleDesignMode(DesignMode);
        }

        /// <summary>
        /// Replaces the second child grid splitter with the new splitter grid control
        /// </summary>
        /// <param name="newSecondChildSplitterGridControl"></param>
        public void ReplaceSecondChildGridSplitter(SplitterGridControl newSecondChildSplitterGridControl, double proportion = 0.5)
        {
            if (_secondChildSplitterGrid == null) return;

            int replaceIndex = Children.IndexOf(_secondChildSplitterGrid);
            if (replaceIndex == -1) return;

            if (replaceIndex == Children.Count - 1)
            {
                // The grid splitter is the last child, so just remove it
                Children.RemoveAt(replaceIndex);
                Children.Add(newSecondChildSplitterGridControl);
            }
            else
            {
                // The grid splitter is not the last child, so replace it with the new splitter
                Children.RemoveAt(replaceIndex);
                Children.Insert(replaceIndex, newSecondChildSplitterGridControl);
            }

            _secondChildSplitterGrid = newSecondChildSplitterGridControl;

            // Ensure row or column settings
            if (_containerInfo.SplitterInfo.Mode == SplitterMode.Horizontal)
            {
                Grid.SetRow(_secondChildSplitterGrid, 1);
                _secondChildSplitterGrid.Margin = new Thickness(0, 5, 0, 0);
                Children.Add(_secondChildSplitterGrid);
            }
            else if (_containerInfo.SplitterInfo.Mode == SplitterMode.Vertical)
            {
                Grid.SetColumn(_secondChildSplitterGrid, 1);
                _secondChildSplitterGrid.Margin = new Thickness(5, 0, 0, 0);
                Children.Add(_secondChildSplitterGrid);
            }

            ContainerInfo.SplitterInfo.FirstChildGridLength = 1.0 - proportion;
            ContainerInfo.SplitterInfo.SecondChildGridLength = proportion;
            _secondChildSplitterGrid.ToggleDesignMode(DesignMode);
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
                    DataTemplate remainingDataTemplate = remainingGridControl.ContentControl.ContentTemplate;

                    // Ensure we now become a content host panel
                    SetSplitterMode(SplitterMode.ContentHost);

                    // And set the data context and content template back to this grid control
                    DataContext = remainingDataContext;
                    ContentControl.ContentTemplate = remainingDataTemplate;
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

        public SplitterPanelContainerInfo ContainerInfo => _containerInfo;

        public ContentControl ContentControl => _contentControl;
    }
}
