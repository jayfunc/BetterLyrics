using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging; // 需引用 System.Drawing.Common
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Vanara.PInvoke;
using Vanara.Windows.Shell;
using static Vanara.PInvoke.Gdi32;
using static Vanara.PInvoke.Shell32;

namespace BetterLyrics.WinUI3.Hooks
{
    public class AppHook
    {
        private static readonly ConcurrentDictionary<string, string?> _nameCache = new();
        private static readonly ConcurrentDictionary<string, BitmapImage?> _iconCache = new();

        private static ShellItem? GetShellItem(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;

            try
            {
                return new ShellItem($"shell:AppsFolder\\{id}");
            }
            catch { }

            if (Path.IsPathRooted(id) && File.Exists(id))
            {
                try
                {
                    return new ShellItem(id);
                }
                catch { }
            }

            try
            {
                using var appsFolder = new ShellFolder(KNOWNFOLDERID.FOLDERID_AppsFolder);
                var found = appsFolder.FirstOrDefault(x =>
                    x.ParsingName?.EndsWith(id, StringComparison.OrdinalIgnoreCase) == true ||
                    x.Name?.Equals(id, StringComparison.OrdinalIgnoreCase) == true);

                if (found != null) return found;
            }
            catch { }

            string? processPath = TryGetPathFromProcess(id);
            if (!string.IsNullOrEmpty(processPath))
            {
                try
                {
                    return new ShellItem(processPath);
                }
                catch { }
            }

            return null;
        }

        private static string? TryGetPathFromProcess(string name)
        {
            try
            {
                string processName = name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                    ? Path.GetFileNameWithoutExtension(name)
                    : name;

                var processes = Process.GetProcessesByName(processName);
                if (processes.Length == 0) return null;

                foreach (var proc in processes)
                {
                    try
                    {
                        if (proc.MainModule?.FileName is string path && File.Exists(path))
                        {
                            return path;
                        }
                    }
                    catch { }
                    finally
                    {
                        proc.Dispose();
                    }
                }
            }
            catch { }
            return null;
        }

        /// <summary>
        /// 通过 AUMID 获取应用名称 (DisplayName)
        /// </summary>
        public static async Task<string?> GetDisplayNameByAumidAsync(string aumid)
        {
            if (_nameCache.TryGetValue(aumid, out var cachedName)) return cachedName;

            string? name = await Task.Run(() =>
            {
                var item = GetShellItem(aumid);
                if (item != null && item.IsFileSystem)
                {
                    try
                    {
                        var info = FileVersionInfo.GetVersionInfo(item.ParsingName);
                        if (!string.IsNullOrWhiteSpace(info.FileDescription))
                            return info.FileDescription;
                    }
                    catch { }
                }

                return item?.GetDisplayName(ShellItemDisplayString.NormalDisplay);
            });

            _nameCache.TryAdd(aumid, name);
            return name;
        }

        /// <summary>
        /// 通过 AUMID 获取 BitmapImage (自动处理 UI 线程切换)
        /// </summary>
        public static async Task<BitmapImage?> GetIconByAumidAsync(string aumid, DispatcherQueue dispatcherQueue)
        {
            if (_iconCache.TryGetValue(aumid, out var cachedImage)) return cachedImage;

            using var stream = await Task.Run(() =>
            {
                var item = GetShellItem(aumid);
                if (item == null) return null;

                try
                {
                    var options = ShellItemGetImageOptions.ResizeToFit | ShellItemGetImageOptions.IconOnly;

                    using var hBitmap = item.GetImage(new SIZE(256, 256), options);
                    using var bitmap = CreateBitmapWithAlpha(hBitmap);

                    if (bitmap == null) return null;

                    var ms = new MemoryStream();
                    bitmap.Save(ms, ImageFormat.Png);
                    ms.Position = 0;
                    return ms;
                }
                catch { return null; }
            });

            if (stream == null)
            {
                _iconCache.TryAdd(aumid, null);
                return null;
            }

            var tcs = new TaskCompletionSource<BitmapImage?>();
            dispatcherQueue.TryEnqueue(async () =>
            {
                try
                {
                    var bitmapImage = new BitmapImage();
                    await bitmapImage.SetSourceAsync(stream.AsRandomAccessStream());
                    _iconCache.TryAdd(aumid, bitmapImage);
                    tcs.SetResult(bitmapImage);
                }
                catch (Exception ex) { tcs.SetException(ex); }
            });

            return await tcs.Task;
        }

        private static Bitmap? CreateBitmapWithAlpha(SafeHBITMAP hBitmap)
        {
            if (hBitmap.IsInvalid) return null;

            BITMAP dobj;
            int structSize = Marshal.SizeOf(typeof(BITMAP));
            IntPtr pStruct = Marshal.AllocHGlobal(structSize);

            try
            {
                if (GetObject(hBitmap, structSize, pStruct) == 0) return null;
                dobj = Marshal.PtrToStructure<BITMAP>(pStruct);
            }
            finally
            {
                Marshal.FreeHGlobal(pStruct);
            }

            var bmp = new Bitmap(dobj.bmWidth, dobj.bmHeight, PixelFormat.Format32bppArgb);
            bmp.SetResolution(96, 96);
            BitmapData data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                                           ImageLockMode.WriteOnly,
                                           PixelFormat.Format32bppArgb);

            int byteCount = dobj.bmWidth * dobj.bmHeight * 4;
            byte[] tempBuffer = new byte[byteCount];

            GetBitmapBits(hBitmap, byteCount, tempBuffer);

            Marshal.Copy(tempBuffer, 0, data.Scan0, byteCount);

            bmp.UnlockBits(data);
            return bmp;
        }
    }
}