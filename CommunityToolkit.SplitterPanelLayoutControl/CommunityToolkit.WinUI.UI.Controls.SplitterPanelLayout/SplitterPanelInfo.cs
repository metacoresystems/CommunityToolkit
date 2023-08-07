using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommunityToolkit.WinUI.UI.Controls.SplitterPanelLayout
{
    /// <summary>
    /// Represents minimal metadata about a splitter panel
    /// Generally used for serialization purposes
    /// </summary>
    public class SplitterPanelInfo
    {
        /// <summary>
        /// The data context associated with the panel (if this is a content host panel)
        /// </summary>
        public object DataContext { get; set; }

        /// <summary>
        /// If this panel is a splitter, this is the proportional size of the first child
        /// </summary>
        public double? FirstChildProportionalSize { get; set; }

        /// <summary>
        /// If this panel is a splitter, this is the proportional size of the second child
        /// </summary>
        public double? SecondChildProportionalSize { get; set; }

        /// <summary>
        /// The mode of the splitter
        /// </summary>
        public SplitterMode SplitterMode { get; set; }

        /// <summary>
        /// The metadata of the first child
        /// </summary>
        public SplitterPanelInfo FirstChildSplitterPanelInfo { get; set; }

        /// <summary>
        /// The metadata of the second child
        /// </summary>
        public SplitterPanelInfo SecondChildSplitterPanelInfo { get; set; }
    }
}
