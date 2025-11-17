using DevWinUI;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vanara.PInvoke;
using Vanara.Windows.Shell;
using static Vanara.PInvoke.Shell32;

namespace BetterLyrics.WinUI3.Hooks
{
    public class IconHook
    {
        private static HICON? GetIconFromAumid(string aumid, int size = 32)
        {
            if (string.IsNullOrEmpty(aumid)) return null;

            HICON hIconCopy = HICON.NULL;

            try
            {
                using var shellItem = GetShellItem(aumid);
                if (shellItem != null)
                {
                    using Bitmap baseIcon = shellItem.GetImage(new SIZE { Height = size, Width = size }, ShellItemGetImageOptions.ResizeToFit).ToBitmap();
                    hIconCopy = User32.CopyIcon(baseIcon.GetHicon());
                }
            }
            catch (Exception) { }

            if (hIconCopy.IsNull)
            {
                User32.DestroyIcon(hIconCopy);
                return null;
            }
            else
            {
                return hIconCopy;
            }
        }

        private static ShellItem? GetShellItem(string aumid)
        {
            var appsFolder = new ShellFolder(KNOWNFOLDERID.FOLDERID_AppsFolder);
            var found = appsFolder.Where(x => x.ParsingName?.Contains(aumid) == true).FirstOrDefault();
            return found;
        }

        private static async Task<BitmapImage?> ToBitmapImageAsync(HICON hIcon)
        {
            if (hIcon.IsNull)
            {
                return null;
            }

            try
            {
                using Icon icon = Icon.FromHandle(hIcon.DangerousGetHandle());
                using Bitmap bitmap = icon.ToBitmap();

                using var memoryStream = new MemoryStream();
                bitmap.Save(memoryStream, ImageFormat.Png);
                memoryStream.Seek(0, SeekOrigin.Begin);

                var bitmapImage = new BitmapImage();
                await bitmapImage.SetSourceAsync(memoryStream.AsRandomAccessStream());
                return bitmapImage;
            }
            catch (Exception) { }
            User32.DestroyIcon(hIcon);
            return null;
        }

        public static async Task<BitmapImage?> GetBitmapImageFromAumid(string aumid)
        {
            var hIcon = GetIconFromAumid(aumid);
            if (hIcon == null)
            {
                return null;
            }

            var bitmapImage = await ToBitmapImageAsync(hIcon.Value);
            return bitmapImage;
        }
    }
}
