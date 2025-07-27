using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Helper
{
    public static class ListViewHelper
    {
        public static int FindChildIndex(this ListView listView, object frameworkElement)
        {
            var children = listView.ItemsPanelRoot.Children.Select(x => ((ListViewItem)x).ContentTemplateRoot).ToList();
            return children.IndexOf((UIElement)frameworkElement);
        }
    }
}
