using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Core;

namespace SplitterGrid
{
    public static class SplitterUtilities
    {
        /// <summary>
        /// Changes the current pointer cursor icon to the given cursor type
        /// </summary>
        public static void SetCurrentCursor(CoreCursorType cursorType)
        {
            var cursor = new CoreCursor(cursorType, 0);

            CoreWindow? coreWindow = CoreWindow.GetForCurrentThread();
            if (coreWindow != null) coreWindow.PointerCursor = cursor;
        }
    }
}
