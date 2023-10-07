using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.Foundation;
using Windows.UI.Core;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI;
using Microsoft.UI.Xaml.Input;

using CommunityToolkit.WinUI.UI.Controls.SplitterPanelLayout.Utilities;

namespace CommunityToolkit.WinUI.UI.Controls.SplitterPanelLayout
{
    public partial class SplitterPanelControl
    {
        /// <summary>
        /// Handles the pointer being pressed and if in design mode, initiates a drag operation
        /// if it has not already been started
        /// </summary>
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

        /// <summary>
        /// Handles the pointer being moved. If in a drag operation, modifies the position of the
        /// drag preview visual
        /// </summary>
        private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (!DesignMode) return;

            // If we're not in a drag operation, nothing to do
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

        /// <summary>
        /// Handles the pointer being released. If in a drag operation, completes the operation
        /// by committing the new panel position and removing the drag preview
        /// </summary>
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

        /// <summary>
        /// Creates the drag preview overlay control when moving the position of a panel
        /// </summary>
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
    }
}
