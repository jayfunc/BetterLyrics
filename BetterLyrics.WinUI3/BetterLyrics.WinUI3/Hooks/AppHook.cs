using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Vanara.PInvoke;
using Vanara.Windows.Shell;
using static Vanara.PInvoke.Shell32;

namespace BetterLyrics.WinUI3.Hooks
{
    /**
     * 使用前先在 .csproj 里添加如下几项
		<TrimmerRootAssembly Include="Vanara.PInvoke.DwmApi" />
		<TrimmerRootAssembly Include="Vanara.PInvoke.Gdi32" />
		<TrimmerRootAssembly Include="Vanara.PInvoke.Shell32" />
		<TrimmerRootAssembly Include="Vanara.PInvoke.User32" />
     * */
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
                User32.DestroyIcon(hIconCopy);
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
            string path = $"shell:AppsFolder\\{aumid}";
            if (Path.Exists(path))
            {
                return new ShellItem(path);
            }
            else
            {
                var shellFolder = new ShellFolder(KNOWNFOLDERID.FOLDERID_AppsFolder);
                var found = shellFolder.FirstOrDefault(x => x.ParsingName?.EndsWith(aumid) == true);
                return found;
            }
        }

        public static string? GetDisplayName(ShellItem shellItem) => shellItem.GetDisplayName(ShellItemDisplayString.NormalDisplay);
    }
}
