using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI.Core;

namespace SplitterGrid
{
    #region Supporting Classes

    public enum SplitterPanelPosition
    {
        None,
        Left,
        Right,
        Top,
        Bottom
    }

    #endregion

    /// <summary>
    /// The layout control, which is the top level layout control for the splitter grid
    /// Allows for the creation of nested splitter grids, and also for splitter grids on
    /// the left, top, right or bottom of the layout
    /// </summary>
    public class SplitterPanelLayoutControl : Control
    {
        private SplitterPanelControl _topLevelSplitterGridControl;
        private Grid _parentGrid;

        private const int MinDesktopLength = 100;

        // Define a dependency property for design mode
        public static readonly Microsoft.UI.Xaml.DependencyProperty DesignModeProperty =
            Microsoft.UI.Xaml.DependencyProperty.Register(nameof(DesignMode),
                typeof(bool),
                typeof(SplitterPanelLayoutControl),
                new Microsoft.UI.Xaml.PropertyMetadata(false, OnDesignModeChanged));

        // Define a dependency property for the data context factory
        public static readonly Microsoft.UI.Xaml.DependencyProperty DataContextFactoryProperty =
            Microsoft.UI.Xaml.DependencyProperty.Register(nameof(DataContextFactory),
                typeof(ISplitterPanelDataContextFactory),
                typeof(SplitterPanelLayoutControl),
                new Microsoft.UI.Xaml.PropertyMetadata(null));

        // Define a dependency property for the grid splitter thickness
        public static readonly Microsoft.UI.Xaml.DependencyProperty GridSplitterThicknessProperty =
            Microsoft.UI.Xaml.DependencyProperty.Register(nameof(GridSplitterThickness),
                typeof(double),
                typeof(SplitterPanelLayoutControl),
                new Microsoft.UI.Xaml.PropertyMetadata(5.0, OnGridSplitterThicknessChanged));

        // Define a dependency property for the panel data template selector
        public static readonly Microsoft.UI.Xaml.DependencyProperty DataTemplateSelectorProperty =
            Microsoft.UI.Xaml.DependencyProperty.Register(nameof(DataTemplateSelector),
                typeof(DataTemplateSelector),
                typeof(SplitterPanelLayoutControl),
                new Microsoft.UI.Xaml.PropertyMetadata(null, OnDataTemplateSelectorChanged));

        public SplitterPanelLayoutControl()
        {
            DefaultStyleKey = typeof(SplitterPanelLayoutControl);

            // Ensure user will always be able to see each possible edge docker mode
            MinWidth = MinDesktopLength;
            MinHeight = MinDesktopLength;
        }

        internal void AppendDragPreview(Grid grid)
        {
            // Append as last child so that is in front of all other elements
            _parentGrid.Children.Add(grid);
        }

        internal void RemoveDragPreview(Grid grid)
        {
            _parentGrid.Children.Remove(grid);
        }

        private static void OnDesignModeChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
        {
            SplitterPanelLayoutControl splitterGridLayout = dependencyObject as SplitterPanelLayoutControl;
            if (splitterGridLayout == null) return;

            splitterGridLayout._topLevelSplitterGridControl?.ToggleDesignMode((bool)args.NewValue);
        }

        internal void OnCapturedPointerMove(SplitterPanelControl capturingSplitterPanel, PointerRoutedEventArgs e)
        {
            // Get the current mouse position
            var currentPoint = e.GetCurrentPoint(this).Position;

            // Get the leaf splitter grids
            var leafSplitterGrids = _topLevelSplitterGridControl?.GetContentHostSplitterPanels();
            if (leafSplitterGrids == null) return;

            // Determine which leaf splitter grid the mouse is over
            foreach (var leafSplitterGrid in leafSplitterGrids.Where(l => l != capturingSplitterPanel))
            {
                var transform = leafSplitterGrid.TransformToVisual(this);
                var point = transform.TransformPoint(new Point(0, 0));

                if (currentPoint.X >= point.X && currentPoint.X <= point.X + leafSplitterGrid.ActualWidth &&
                    currentPoint.Y >= point.Y && currentPoint.Y <= point.Y + leafSplitterGrid.ActualHeight)
                {
                    // The mouse is over this leaf splitter grid
                    leafSplitterGrid.SetDropActive(true);
                    break;
                }
                else
                {
                    // The mouse is not over this leaf splitter grid
                    leafSplitterGrid.SetDropActive(false);
                }
            }
        }

        internal void OnCapturedPointerReleased(SplitterPanelControl capturingSplitterGrid, PointerRoutedEventArgs e)
        {
            var leafSplitterGrids = _topLevelSplitterGridControl?.GetContentHostSplitterPanels();
            if (leafSplitterGrids == null) return;

            // Get the leaf splitter grids
            SplitterPanelControl dropActiveGrid = leafSplitterGrids.Where(g => g.IsDropActive).FirstOrDefault();

            if (dropActiveGrid != null)
            {
                // Swap the grids by just exchanging the data contexts (no need for manipulation of the visual tree)
                object dropActiveDataContext = dropActiveGrid.DataContext;
                object capturingDataContext = capturingSplitterGrid.DataContext;

                dropActiveGrid.DataContext = capturingDataContext;
                capturingSplitterGrid.DataContext = dropActiveDataContext;
            }

            // Determine which leaf splitter grid the mouse is over
            foreach (var leafSplitterGrid in leafSplitterGrids.Where(l => l != capturingSplitterGrid))
            {
                leafSplitterGrid.SetDropActive(false);
            }
        }

        private static void OnGridSplitterThicknessChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
        {
            SplitterPanelLayoutControl splitterGridLayout = (SplitterPanelLayoutControl)dependencyObject;
            if (splitterGridLayout == null) return;

            // Propagate down the grid splitter tree
            splitterGridLayout._topLevelSplitterGridControl?.SetGridSplitterThickness((double)args.NewValue);
        }

        private static void OnDataTemplateSelectorChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
        {
            SplitterPanelLayoutControl splitterGridLayout = (SplitterPanelLayoutControl)dependencyObject;
            if (splitterGridLayout == null) return;

            splitterGridLayout._topLevelSplitterGridControl?.SetDataTemplateSelector((DataTemplateSelector)args.NewValue);
        }

        private void ReplaceTopLevelSplitterGridControl(SplitterPanelControl splitterGridControl)
        {
            // Remove the current top level splitter grid control from the parent grid
            if (_topLevelSplitterGridControl != null)
                _parentGrid.Children.Remove(_topLevelSplitterGridControl);

            // Add the new top level splitter grid control to the parent grid
            if (splitterGridControl != null)
                _parentGrid.Children.Add(splitterGridControl);

            // Set the new top level splitter grid control
            _topLevelSplitterGridControl = splitterGridControl;
        }

        /// <summary>
        /// Adds a splitter panel to the layout in the specified relative position
        /// </summary>
        /// <param name="splitterPanelPosition">The position in the layout to which the panel should be added</param>
        /// <exception cref="InvalidOperationException"></exception>
        public void AddSplitterPanel(SplitterPanelPosition splitterPanelPosition)
        {
            void AddSplitterEdge(int rowOrColumnIndex, SplitterMode splitterMode, double proportion)
            {
                // For this, we need to replace the current top level splitter grid control with a
                // new one that is split vertically, and then place the old top level splitter grid
                // control in the LHS column
                var newTopLevelSplitterGridControl = new SplitterPanelControl();                

                // Get a reference to the of top level splitter grid control
                var oldTopLevelSplitterGridControl = _topLevelSplitterGridControl;

                // Replace the top level splitter grid control with the new one and split it horizontally
                ReplaceTopLevelSplitterGridControl(newTopLevelSplitterGridControl);
                newTopLevelSplitterGridControl.SplitterMode = splitterMode;

                if (rowOrColumnIndex == 0)
                {
                    // And now replace the upper pane with the old top level splitter grid control
                    newTopLevelSplitterGridControl.ReplaceFirstChildGridSplitter(oldTopLevelSplitterGridControl, proportion);
                }
                else
                {
                    // And now replace the lower pane with the old top level splitter grid control
                    newTopLevelSplitterGridControl.ReplaceSecondChildGridSplitter(oldTopLevelSplitterGridControl, proportion);
                }
            }

            switch (splitterPanelPosition)
            {
            case SplitterPanelPosition.Left:
                {
                    AddSplitterEdge(1, SplitterMode.Vertical, 0.7);
                }
                break;
            case SplitterPanelPosition.Right:
                {
                    AddSplitterEdge(0, SplitterMode.Vertical, 0.3);
                }
                break;
            case SplitterPanelPosition.Top:
                {
                    AddSplitterEdge(1, SplitterMode.Horizontal, 0.7);
                }
                break;
            case SplitterPanelPosition.Bottom:
                {
                    AddSplitterEdge(0, SplitterMode.Horizontal, 0.3);
                }
                break;
            default:
                throw new InvalidOperationException($"Unhandled {nameof(SplitterPanelPosition)}");
            }
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _topLevelSplitterGridControl = FindName("rootSplitterPanel") as SplitterPanelControl;
            _topLevelSplitterGridControl?.ToggleDesignMode(DesignMode);
            _topLevelSplitterGridControl?.SetGridSplitterThickness(GridSplitterThickness);
            _topLevelSplitterGridControl?.SetDataTemplateSelector(DataTemplateSelector);

            _parentGrid = FindName("parentGrid") as Grid;
        }

        /// <summary>
        /// Recursively obtains the tree of splitter infos describing the configuration of the layout
        /// Mainly of use in either cloning a layout or serializing it
        /// </summary>
        /// <returns>The complete tree of splitter infos describing the panels in the layout configuration</returns>
        internal SplitterPanelInfo GetSplitterPanelInfo()
        {
            return _topLevelSplitterGridControl?.GetSplitterPanelInfo();
        }

        /// <summary>
        /// Indicates whether the panel layout is in design mode
        /// </summary>
        public bool DesignMode
        {
            get => (bool)GetValue(DesignModeProperty);
            set => SetValue(DesignModeProperty, value);
        }

        /// <summary>
        /// Data context factory for creating grid splitter data contexts
        /// </summary>
        public ISplitterPanelDataContextFactory DataContextFactory
        {
            get => (ISplitterPanelDataContextFactory)GetValue(DataContextFactoryProperty);
            set => SetValue(DataContextFactoryProperty, value);
        }

        /// <summary>
        /// The thickness of the grid splitters in the layout
        /// </summary>
        public double GridSplitterThickness
        {
            get => (double)GetValue(GridSplitterThicknessProperty);
            set => SetValue(GridSplitterThicknessProperty, value);
        }

        /// <summary>
        /// The data template selector for the panels in the layout
        /// </summary>
        public DataTemplateSelector DataTemplateSelector
        {
            get => (DataTemplateSelector)GetValue(DataTemplateSelectorProperty);
            set => SetValue(DataTemplateSelectorProperty, value);
        }
    }
}
