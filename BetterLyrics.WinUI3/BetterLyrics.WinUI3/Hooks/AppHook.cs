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
    public class AppHook
    {
        public static HICON? GetIcon(ShellItem shellItem, int size = 32)
        {
            HICON hIconCopy = HICON.NULL;

            try
            {
                using Bitmap baseIcon = shellItem.GetImage(new SIZE { Height = size, Width = size }, ShellItemGetImageOptions.ResizeToFit).ToBitmap();
                hIconCopy = User32.CopyIcon(baseIcon.GetHicon());
            }
            catch (Exception) { }

            if (hIconCopy.IsNull)
            {
                return null;
            }
            else
            {
                return hIconCopy;
            }
        }

        public static async Task<BitmapImage?> ToBitmapImageAsync(HICON hIcon)
        {
            if (hIcon.IsNull)
            {
                return null;
            }

            using Icon icon = Icon.FromHandle(hIcon.DangerousGetHandle());
            using Bitmap bitmap = icon.ToBitmap();

            using var memoryStream = new MemoryStream();
            bitmap.Save(memoryStream, ImageFormat.Png);
            memoryStream.Seek(0, SeekOrigin.Begin);

            var bitmapImage = new BitmapImage();
            await bitmapImage.SetSourceAsync(memoryStream.AsRandomAccessStream());

            User32.DestroyIcon(hIcon);

            return bitmapImage;
        }

        public static ShellItem? GetShellItem(string aumid)
        {
            try
            {
                return new ShellItem($"shell:AppsFolder\\{aumid}");
            }
            catch
            {
                var shellFolder = new ShellFolder(KNOWNFOLDERID.FOLDERID_AppsFolder);
                var found = shellFolder.FirstOrDefault(x => x.ParsingName?.EndsWith(aumid) == true);
                return found;
            }
        }

        public static string? GetDisplayName(ShellItem shellItem) => shellItem.GetDisplayName(ShellItemDisplayString.NormalDisplay);
    }
}
