using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BetterLyrics.WinUI3.Views;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.Storage;
using WinRT.Interop;

namespace BetterLyrics.WinUI3.Helper
{
    public class WindowHelper
    {
        public static void TrackWindow(Window window)
        {
            window.Closed += (sender, args) =>
            {
                _activeWindows.Remove(window);
            };
            _activeWindows.Add(window);
        }

        public static Window GetWindowForElement(UIElement element)
        {
            if (element.XamlRoot != null)
            {
                foreach (Window window in _activeWindows)
                {
                    if (element.XamlRoot == window.Content.XamlRoot)
                    {
                        return window;
                    }
                }
            }
            return null;
        }

        // get dpi for an element
        static public double GetRasterizationScaleForElement(UIElement element)
        {
            if (element.XamlRoot != null)
            {
                foreach (Window window in _activeWindows)
                {
                    if (element.XamlRoot == window.Content.XamlRoot)
                    {
                        return element.XamlRoot.RasterizationScale;
                    }
                }
            }
            return 0.0;
        }

        public static List<Window> ActiveWindows
        {
            get { return _activeWindows; }
        }

        private static List<Window> _activeWindows = new List<Window>();

        public static StorageFolder GetAppLocalFolder()
        {
            StorageFolder localFolder;
            if (!NativeHelper.IsAppPackaged)
            {
                localFolder = Task.Run(async () =>
                    await StorageFolder.GetFolderFromPathAsync(System.AppContext.BaseDirectory)
                ).Result;
            }
            else
            {
                localFolder = ApplicationData.Current.LocalFolder;
            }
            return localFolder;
        }
    }
}
