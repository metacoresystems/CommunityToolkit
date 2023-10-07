using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Input;

namespace CommunityToolkit.WinUI.UI.Controls.SplitterPanelLayout
{
    public partial class SplitterPanelControl
    {
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

        /// <summary>
        /// Recursively sets the settings data template selector of all grid splitters in the panel,
        /// including on child splitter grid controls
        /// </summary>
        /// <param name="dataTemplateSelector">The settings data template selector the splitter grid control should use</param>
        public void SetSettingsDataTemplateSelector(DataTemplateSelector dataTemplateSelector)
        {
            if (_contentControl != null)
            {
                // Note: pending a fix to the redraw mechanism when we change the content
                // template selector, we call it directly here on the current data context
                _designModeSettingsContentControl.ContentTemplateSelector = dataTemplateSelector;
                _designModeSettingsContentControl.ContentTemplate = dataTemplateSelector?.SelectTemplate(DataContext);
            }

            _firstChildSplitterPanelControl?.SetSettingsDataTemplateSelector(dataTemplateSelector);
            _secondChildSplitterPanelControl?.SetSettingsDataTemplateSelector(dataTemplateSelector);
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

        private void OnGridSplitterPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            // Ensure handled to prevent the grid splitter from stealing focus
            e.Handled = true;
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

            // The content control hosts the active content and stretches across the entire panel
            _contentControl = new ContentControl()
            {
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch
            };

            // Create the design mode UI elements which will be hidden when not in design mode
            CreateDesignModeControls();

            // Add in correct Z-order (bottom to top)
            Children.Add(_contentControl);
            Children.Add(_designModeOverlayGrid);
            Children.Add(_designModeOverlayBorder);
            Children.Add(_designSettingsBackgroundGrid);
            Children.Add(_designModeSettingsContentControl);
            Children.Add(_designModeSettingsIconButton);
        }
    }
}
