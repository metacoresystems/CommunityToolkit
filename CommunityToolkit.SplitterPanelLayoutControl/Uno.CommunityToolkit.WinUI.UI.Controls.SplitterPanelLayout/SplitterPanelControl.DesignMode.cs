using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace CommunityToolkit.WinUI.UI.Controls.SplitterPanelLayout
{
    /// <summary>
    /// The content host logic components for the splitter panel control
    /// </summary>
    public partial class SplitterPanelControl
    {
        private ContentControl _designModeSettingsContentControl;
        private Grid _designModeOverlayGrid;
        private Border _designModeOverlayBorder;
        private Grid _designSettingsBackgroundGrid;
        private Button _designModeSettingsIconButton;

        /// <summary>
        /// Makes the overlay UI visible when entering design mode
        /// </summary>
        private void ShowDesignModePanelOverlay()
        {
            _designModeSettingsIconButton.Visibility = Visibility.Visible;
            _designModeOverlayGrid.Visibility = Visibility.Visible;
            _designModeOverlayBorder.Visibility = Visibility.Visible;

            _designSettingsBackgroundGrid.Visibility = Visibility.Collapsed;
            _designModeSettingsContentControl.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Hides the overlay UI when exiting design mode
        /// </summary>
        private void HideDesignModePanelOverlay()
        {
            _designModeSettingsIconButton.Visibility = Visibility.Collapsed;
            _designModeOverlayGrid.Visibility = Visibility.Collapsed;
            _designModeOverlayBorder.Visibility = Visibility.Collapsed;

            _designSettingsBackgroundGrid.Visibility = Visibility.Collapsed;
            _designModeSettingsContentControl.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Adds the menu items for the supported plugin panel types
        /// </summary>
        private void AddSupportedPanelMenuItems(MenuFlyout menuFlyout, ISplitterPanelDataContextFactory dataContextFactory)
        {
            var setPanelMenuItem = new MenuFlyoutSubItem() { Text = "Set Panel Content" };
            menuFlyout.Items.Add(setPanelMenuItem);

            foreach (var supportedDataContext in dataContextFactory.GetSupportedDataContexts())
            {
                var menuFlyoutItem = new MenuFlyoutItem() { Text = supportedDataContext.Key };
                menuFlyoutItem.Click += (s, e) =>
                {
                    var dataContext = dataContextFactory.CreateDataContext(supportedDataContext.Value);

                    DataContext = dataContext;
                    var dataTemplateSelector = GetParentLayout()?.DataTemplateSelector;
                    var settingsDataTemplateSelector = GetParentLayout()?.SettingsDataTemplateSelector;

                    // Note: Would use a DataTemplateSelector here, but there seems to be a bug in
                    // Uno's repainting of the content control when the template selector is changed
                    // We therefore for now call the data template selector directly
                    _contentControl.ContentTemplateSelector = dataTemplateSelector;

                    var dataTemplate = dataTemplateSelector?.SelectTemplate(DataContext, this);
                    _contentControl.ContentTemplate = dataTemplate;

                    _designModeSettingsContentControl.ContentTemplateSelector = settingsDataTemplateSelector;
                    _designModeSettingsContentControl.ContentTemplate = settingsDataTemplateSelector?.SelectTemplate(DataContext, this);
                };

                setPanelMenuItem.Items.Add(menuFlyoutItem);
            }
        }

        /// <summary>
        /// Adds the panel creation menu items
        /// </summary>
        private void AddPanelCreationMenuItems(MenuFlyout menuFlyout)
        {
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
        }

        /// <summary>
        /// Adds the panel splitting menu items
        /// </summary>
        private void AddPanelSplittingMenuItems(MenuFlyout menuFlyout)
        {
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
        }

        /// <summary>
        /// Creates the design mode context menu for the splitter panel when entering design mode
        /// </summary>
        private void CreateSplitterContextMenu()
        {
            ISplitterPanelDataContextFactory dataContextFactory = GetParentLayout()?.DataContextFactory;
            if (dataContextFactory == null) return;

            // For splitting
            // Available panels are determined by the injected splitter panel data context factory
            var menuFlyout = new MenuFlyout();

            // Add the supported panel menu items
            AddSupportedPanelMenuItems(menuFlyout, dataContextFactory);

            menuFlyout.Items.Add(new MenuFlyoutSeparator());

            // Add the panel creation menu items
            AddPanelCreationMenuItems(menuFlyout);

            // Add the panel splitting menu items
            AddPanelSplittingMenuItems(menuFlyout);

            // Set the panel context menu
            _designModeOverlayGrid.ContextFlyout = menuFlyout;
        }

        /// <summary>
        /// Destroys the design mode context menu when exiting design mode
        /// </summary>
        private void DestroySplitterContextMenu()
        {
            // Unassign the context menu
            if (_designModeOverlayGrid != null) _designModeOverlayGrid.ContextFlyout = null;
        }

        private void CreateDesignModeControls()
        {
            // The design mode overlay is a semi-transparent overlay that covers the entire panel
            // and shows to the user that the panel is in design mode
            _designModeOverlayGrid = new Grid()
            {
                Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White),
                Opacity = 0.5,
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

            // The additional background to make the settings UI more distinct from the active panel controls
            _designSettingsBackgroundGrid = new Grid()
            {
                Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White),
                IsHitTestVisible = false,
                Opacity = 0.5,
                Padding = new Thickness(5),
                Visibility = Visibility.Collapsed
            };

            // The settings content control stretches to fill the entire grid
            _designModeSettingsContentControl = new ContentControl()
            {
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch,
                Visibility = Visibility.Collapsed,
                Padding = new Thickness(5),
                Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent),
            };

            // The settings icon grid is a grid that shows the settings icon when the panel is in design mode
            _designModeSettingsIconButton = new Button()
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(5),
            };

            _designModeSettingsIconButton.Width = 100;
            _designModeSettingsIconButton.Height = 100;

            _designModeSettingsIconButton.Content = new TextBlock()
            {
                Text = "Settings"
            };

            _designModeSettingsIconButton.Click += (s, e) =>
            {
                // Flip the visibility of the settings UI
                _designSettingsBackgroundGrid.Visibility = _designSettingsBackgroundGrid.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
                _designModeSettingsContentControl.Visibility = _designModeSettingsContentControl.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            };
        }
    }
}
