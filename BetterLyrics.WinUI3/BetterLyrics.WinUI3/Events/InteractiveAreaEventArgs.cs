using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;

namespace BetterLyrics.WinUI3.Events
{
    public class InteractiveAreaEventArgs : EventArgs
    {
        public IList<FrameworkElement> Elements { get; set; }

        public InteractiveAreaEventArgs(IList<FrameworkElement> elements)
        {
            Elements = elements;
        }
    }
}
