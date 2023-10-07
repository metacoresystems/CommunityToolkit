using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using CommunityToolkit.WinUI.UI.Controls.SplitterPanelLayout.Converters;

namespace CommunityToolkit.WinUI.UI.Controls.SplitterPanelLayout
{
    public partial class SplitterPanelControl
    {
        internal SplitterPanelControl FirstChildSplitterPanelControl => _firstChildSplitterPanelControl;

        internal SplitterPanelControl SecondChildSplitterPanelControl => _secondChildSplitterPanelControl;

        /// <summary>
        /// 'Unsplits' a splitter panel control by removing one child
        /// </summary>
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

        /// <summary>
        /// Creates a new splitter panel control and assigns it the provided data context
        /// </summary>        
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

        /// <summary>
        /// Splits the splitter panel control into two child splitter panel controls via a horizontal split
        /// </summary>
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

        /// <summary>
        /// Splits the splitter panel control into two child splitter panel controls via a vertical split
        /// </summary>
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

        /// <summary>
        /// Substitutes the content of this splitter panel control with the content of another splitter panel control
        /// </summary>
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

        /// <summary>
        /// Splits the splitter panel control into two child splitter panel controls, either via
        /// a horizontal or vertical split
        /// </summary>
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
            _designModeSettingsContentControl = null;
            _designModeOverlayGrid = null;
            _designModeOverlayBorder = null;
            _designSettingsBackgroundGrid = null;

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
    }
}
