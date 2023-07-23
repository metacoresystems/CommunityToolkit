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

    public enum SplitterEdgeDockKind
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
    public class SplitterGridLayout : Control
    {
        private SplitterGridControl _topLevelSplitterGridControl;
        private Grid _parentGrid;

        private bool _isDragActive;

        private const int MinDesktopLength = 100;

        // Define a dependency property for design mode
        public static readonly Microsoft.UI.Xaml.DependencyProperty DesignModeProperty =
            Microsoft.UI.Xaml.DependencyProperty.Register(nameof(DesignMode),
                typeof(bool),
                typeof(SplitterGridLayout),
                new Microsoft.UI.Xaml.PropertyMetadata(false, OnDesignModeChanged));

        // Define a dependency property for the data context factory
        public static readonly Microsoft.UI.Xaml.DependencyProperty DataContextFactoryProperty =
            Microsoft.UI.Xaml.DependencyProperty.Register(nameof(DataContextFactory),
                typeof(ISplitterPanelDataContextFactory),
                typeof(SplitterGridLayout),
                new Microsoft.UI.Xaml.PropertyMetadata(null));

        public SplitterGridLayout()
        {
            DefaultStyleKey = typeof(SplitterGridLayout);

            // Ensure user will always be able to see each possible edge docker mode
            MinWidth = MinDesktopLength;
            MinHeight = MinDesktopLength;
        }

        public void AppendDragPreview(Grid grid)
        {
            // Append as last child so that is in front of all other elements
            _parentGrid.Children.Add(grid);
        }

        public void RemoveDragPreview(Grid grid)
        {
            _parentGrid.Children.Remove(grid);
        }

        private static void OnDesignModeChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
        {
            SplitterGridLayout splitterGridLayout = dependencyObject as SplitterGridLayout;
            if (splitterGridLayout == null) return;

            splitterGridLayout._topLevelSplitterGridControl.ToggleDesignMode((bool)args.NewValue);
        }

        public void OnCapturedPointerMove(SplitterGridControl capturingSplitterGrid, PointerRoutedEventArgs e)
        {
            _isDragActive = true;

            // Get the current mouse position
            var currentPoint = e.GetCurrentPoint(this).Position;

            // Get the leaf splitter grids
            var leafSplitterGrids = _topLevelSplitterGridControl.GetLeafSplitterGrids();

            // Determine which leaf splitter grid the mouse is over
            foreach (var leafSplitterGrid in leafSplitterGrids.Where(l => l != capturingSplitterGrid))
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

        public void OnCapturedPointerReleased(SplitterGridControl capturingSplitterGrid, PointerRoutedEventArgs e)
        {
            var leafSplitterGrids = _topLevelSplitterGridControl.GetLeafSplitterGrids();

            // Get the leaf splitter grids
            SplitterGridControl dropActiveGrid = leafSplitterGrids.Where(g => g.IsDropActive).FirstOrDefault();

            if (dropActiveGrid != null)
            {
                // Swap the grids by just exchanging the data contexts (no need for manipulation of the visual tree)
                object dropActiveDataContext = dropActiveGrid.DataContext;
                object capturingDataContext = capturingSplitterGrid.DataContext;

                DataTemplate dropActiveDataTemplate = dropActiveGrid.ContentControl.ContentTemplate;
                DataTemplate capturingDataTemplate = capturingSplitterGrid.ContentControl.ContentTemplate;

                dropActiveGrid.DataContext = capturingDataContext;
                dropActiveGrid.ContentControl.ContentTemplate = capturingDataTemplate;

                capturingSplitterGrid.DataContext = dropActiveDataContext;
                capturingSplitterGrid.ContentControl.ContentTemplate = dropActiveDataTemplate;
            }

            // Determine which leaf splitter grid the mouse is over
            foreach (var leafSplitterGrid in leafSplitterGrids.Where(l => l != capturingSplitterGrid))
            {
                leafSplitterGrid.SetDropActive(false);
            }

            _isDragActive = false;
        }

        private void ReplaceTopLevelSplitterGridControl(SplitterGridControl splitterGridControl)
        {
            // Remove the current top level splitter grid control from the parent grid
            _parentGrid.Children.Remove(_topLevelSplitterGridControl);

            // Add the new top level splitter grid control to the parent grid
            _parentGrid.Children.Add(splitterGridControl);

            // Set the new top level splitter grid control
            _topLevelSplitterGridControl = splitterGridControl;
        }

        public void AddSplitterEdge(SplitterEdgeDockKind splitterEdgeDockKind)
        {
            void AddSplitterEdge(int rowOrColumnIndex, SplitterMode splitterMode, double proportion)
            {
                // For this, we need to replace the current top level splitter grid control with a
                // new one that is split vertically, and then place the old top level splitter grid
                // control in the LHS column
                var newTopLevelSplitterGridControl = new SplitterGridControl();

                // Get a reference to the of top level splitter grid control
                var oldTopLevelSplitterGridControl = _topLevelSplitterGridControl;

                // Replace the top level splitter grid control with the new one and split it horizontally
                ReplaceTopLevelSplitterGridControl(newTopLevelSplitterGridControl);
                newTopLevelSplitterGridControl.SetSplitterMode(splitterMode);

                // And now replace the lower pane with the old top level splitter grid control
                if (rowOrColumnIndex == 0)
                {
                    newTopLevelSplitterGridControl.ReplaceFirstChildGridSplitter(oldTopLevelSplitterGridControl, proportion);
                }
                else
                {
                    newTopLevelSplitterGridControl.ReplaceSecondChildGridSplitter(oldTopLevelSplitterGridControl, proportion);
                }
            }

            switch (splitterEdgeDockKind)
            {
                case SplitterEdgeDockKind.Left:
                    {
                        AddSplitterEdge(0, SplitterMode.Vertical, 0.3);
                    }
                    break;
                case SplitterEdgeDockKind.Right:
                    {
                        AddSplitterEdge(1, SplitterMode.Vertical, 0.7);
                    }
                    break;
                case SplitterEdgeDockKind.Top:
                    {
                        AddSplitterEdge(1, SplitterMode.Horizontal, 0.7);
                    }
                    break;
                case SplitterEdgeDockKind.Bottom:
                    {
                        AddSplitterEdge(0, SplitterMode.Horizontal, 0.3);
                    }
                    break;
            }
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _topLevelSplitterGridControl = FindName("rootSplitterGrid") as SplitterGridControl;
            _parentGrid = FindName("parentGrid") as Grid;
        }

        public bool DesignMode
        {
            get => (bool)GetValue(DesignModeProperty);
            set => SetValue(DesignModeProperty, value);
        }

        public ISplitterPanelDataContextFactory DataContextFactory
        {
            get => (ISplitterPanelDataContextFactory)GetValue(DataContextFactoryProperty);
            set => SetValue(DataContextFactoryProperty, value);
        }
    }
}
