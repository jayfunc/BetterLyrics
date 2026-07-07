using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using BetterLyrics.Core.Interfaces.Providers;
using CommunityToolkit.Mvvm.DependencyInjection;
using Vanara.PInvoke;
using Vanara.Windows.Shell;
using static Vanara.PInvoke.Gdi32;
using static Vanara.PInvoke.Shell32;

namespace BetterLyrics.WinUI3.Providers;

public class ProgramProvider : IProgramProvider
{
    private static readonly IAppUIThreadProvider _appUIThreadProvider = Ioc.Default.GetRequiredService<IAppUIThreadProvider>();

    private static readonly ConcurrentDictionary<string, string?> _nameCache = new();
    private static readonly ConcurrentDictionary<string, string?> _pathCache = new();
    private static readonly ConcurrentDictionary<string, byte[]?> _iconCache = new();

    private static ShellItem? GetShellItem(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;

        try
        {
            return new ShellItem($"shell:AppsFolder\\{id}");
        }
        catch
        {
        }

        if (Path.IsPathRooted(id) && File.Exists(id))
            try
            {
                return new ShellItem(id);
            }
            catch
            {
            }

        try
        {
            using var appsFolder = new ShellFolder(KNOWNFOLDERID.FOLDERID_AppsFolder);

            //Debug.WriteLine("== Enumerating AppsFolder ==");
            //var tmp = appsFolder.EnumerateChildren(FolderItemFilter.NonFolders);
            //foreach (var item in tmp)
            //{
            //    Debug.WriteLine($"Found app: {item.Name}, ParsingName: {item.ParsingName}");
            //}

            var found = appsFolder.FirstOrDefault(x =>
                Path.GetFileName(x.ParsingName)?.Equals(id, StringComparison.OrdinalIgnoreCase) == true ||
                x.Name?.Equals(id, StringComparison.OrdinalIgnoreCase) == true);

            if (found != null) return found;
        }
        catch
        {
        }

        var processPath = TryGetPathFromProcess(id);
        if (!string.IsNullOrEmpty(processPath))
            try
            {
                return new ShellItem(processPath);
            }
            catch
            {
            }

        return null;
    }

    private static string? TryGetPathFromProcess(string name)
    {
        try
        {
            var processName = name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                ? Path.GetFileNameWithoutExtension(name)
                : name;

            var processes = Process.GetProcessesByName(processName);
            if (processes.Length == 0) return null;

            foreach (var proc in processes)
                try
                {
                    if (proc.MainModule?.FileName is string path && File.Exists(path)) return path;
                }
                catch
                {
                }
                finally
                {
                    proc.Dispose();
                }
        }
        catch
        {
        }

        return null;
    }

    /// <summary>
    ///     通过 AUMID 获取应用名称 (DisplayName)
    /// </summary>
    public async Task<string?> GetDisplayNameByAumidAsync(string? aumid)
    {
        if (aumid == null) return null;
        if (_nameCache.TryGetValue(aumid, out var cachedName)) return cachedName;

        var name = await Task.Run(() =>
        {
            var item = GetShellItem(aumid);
            if (item != null && item.IsFileSystem && item.ParsingName is string parsingName)
                try
                {
                    var info = FileVersionInfo.GetVersionInfo(parsingName);
                    if (!string.IsNullOrWhiteSpace(info.FileDescription))
                        return info.FileDescription;
                }
                catch
                {
                }

            return item?.GetDisplayName(ShellItemDisplayString.NormalDisplay);
        });

        _nameCache.TryAdd(aumid, name);
        return name;
    }

    /// <summary>
    ///     通过 AUMID 获取 BitmapImage
    /// </summary>
    public async Task<byte[]?> GetIconByAumidAsync(string aumid)
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
            catch
            {
                return null;
            }
        });

        if (stream == null)
        {
            _iconCache.TryAdd(aumid, null);
            return null;
        }

        var tcs = new TaskCompletionSource<byte[]?>();
        _appUIThreadProvider.Execute(async () =>
        {
            try
            {
                var bytes = stream.ToArray();
                _iconCache.TryAdd(aumid, bytes);
                tcs.SetResult(bytes);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });

        return await tcs.Task;
    }

    /// <summary>
    ///     通过 AUMID 获取应用程序的物理路径
    /// </summary>
    public async Task<string?> GetAppPathByAumidAsync(string? aumid)
    {
        if (string.IsNullOrWhiteSpace(aumid)) return null;
        if (_pathCache.TryGetValue(aumid, out var cachedPath)) return cachedPath;

        var path = await Task.Run(() =>
        {
            var item = GetShellItem(aumid);
            return item?.GetDisplayName(ShellItemDisplayString.DesktopAbsoluteParsing);
        });

        if (path != null) path = $"shell:AppsFolder\\{path}";

        _pathCache.TryAdd(aumid, path);

        return path;
    }

    private static Bitmap? CreateBitmapWithAlpha(SafeHBITMAP hBitmap)
    {
        if (hBitmap.IsInvalid) return null;

        BITMAP dobj;
        var structSize = Marshal.SizeOf(typeof(BITMAP));
        var pStruct = Marshal.AllocHGlobal(structSize);

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
        var data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
            ImageLockMode.WriteOnly,
            PixelFormat.Format32bppArgb);

        var byteCount = dobj.bmWidth * dobj.bmHeight * 4;
        var tempBuffer = new byte[byteCount];

        GetBitmapBits(hBitmap, byteCount, tempBuffer);

        Marshal.Copy(tempBuffer, 0, data.Scan0, byteCount);

        bmp.UnlockBits(data);
        return bmp;
    }
}