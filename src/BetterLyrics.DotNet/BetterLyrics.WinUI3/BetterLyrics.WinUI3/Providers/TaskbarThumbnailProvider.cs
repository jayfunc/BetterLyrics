using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Collections.Concurrent;
using Vanara.PInvoke;
using BetterLyrics.Core.Interfaces.Providers;
using Vanara.Windows.Shell;

namespace BetterLyrics.WinUI3.Providers;

public partial class TaskbarThumbnailProvider : ITaskbarThumbnailProvider
{
    public const int THB_PREVIOUS = 0;
    public const int THB_PLAYPAUSE = 1;
    public const int THB_NEXT = 2;

    private bool _isDisposed;

    private readonly IntPtr _hIconPrevious;
    private readonly IntPtr _hIconPlay;
    private readonly IntPtr _hIconPause;
    private readonly IntPtr _hIconNext;

    private readonly ConcurrentDictionary<IntPtr, bool> _buttonsAddedMap = new();
    private readonly ConcurrentDictionary<IntPtr, bool> _playingStateMap = new();

    public TaskbarThumbnailProvider()
    {
        try
        {
            _hIconPrevious = GenerateIcon("\uE892");
            _hIconPlay = GenerateIcon("\uE768");
            _hIconPause = GenerateIcon("\uE769");
            _hIconNext = GenerateIcon("\uE893");
        }
        catch
        {
        }
    }

    public void InitializeButtons(IntPtr hwnd)
    {
        if (_buttonsAddedMap.ContainsKey(hwnd)) return;

        var buttons = new Shell32.THUMBBUTTON[3];

        buttons[0] = new Shell32.THUMBBUTTON
        {
            dwMask = Shell32.THUMBBUTTONMASK.THB_ICON | Shell32.THUMBBUTTONMASK.THB_TOOLTIP | Shell32.THUMBBUTTONMASK.THB_FLAGS,
            iId = THB_PREVIOUS,
            hIcon = new HICON(_hIconPrevious),
            szTip = "Previous",
            dwFlags = Shell32.THUMBBUTTONFLAGS.THBF_ENABLED
        };

        buttons[1] = new Shell32.THUMBBUTTON
        {
            dwMask = Shell32.THUMBBUTTONMASK.THB_ICON | Shell32.THUMBBUTTONMASK.THB_TOOLTIP | Shell32.THUMBBUTTONMASK.THB_FLAGS,
            iId = THB_PLAYPAUSE,
            hIcon = new HICON(_hIconPlay),
            szTip = "Play",
            dwFlags = Shell32.THUMBBUTTONFLAGS.THBF_ENABLED
        };

        buttons[2] = new Shell32.THUMBBUTTON
        {
            dwMask = Shell32.THUMBBUTTONMASK.THB_ICON | Shell32.THUMBBUTTONMASK.THB_TOOLTIP | Shell32.THUMBBUTTONMASK.THB_FLAGS,
            iId = THB_NEXT,
            hIcon = new HICON(_hIconNext),
            szTip = "Next",
            dwFlags = Shell32.THUMBBUTTONFLAGS.THBF_ENABLED
        };

        try
        {
            TaskbarList.ThumbBarAddButtons(hwnd, buttons);
            _buttonsAddedMap[hwnd] = true;
        }
        catch
        {
            // Handle cases where thumbnail buttons cannot be added
        }
    }

    public void UpdatePlayPauseState(IntPtr hwnd, bool isPlaying)
    {
        if (_isDisposed || !_buttonsAddedMap.ContainsKey(hwnd)) return;

        if (_playingStateMap.TryGetValue(hwnd, out var currentPlaying) && currentPlaying == isPlaying)
            return;

        _playingStateMap[hwnd] = isPlaying;

        var buttons = new Shell32.THUMBBUTTON[1];
        buttons[0] = new Shell32.THUMBBUTTON
        {
            dwMask = Shell32.THUMBBUTTONMASK.THB_ICON | Shell32.THUMBBUTTONMASK.THB_TOOLTIP,
            iId = THB_PLAYPAUSE,
            hIcon = new HICON(isPlaying ? _hIconPause : _hIconPlay),
            szTip = isPlaying ? "Pause" : "Play"
        };

        try
        {
            TaskbarList.ThumbBarUpdateButtons(hwnd, buttons);
        }
        catch
        {
            // Ignore update errors
        }
    }

    private static IntPtr GenerateIcon(string glyph)
    {
        try
        {
            using var bmp = new Bitmap(32, 32);
            using var g = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

            Font? font = null;
            try
            {
                font = new Font("Segoe Fluent Icons", 20, FontStyle.Bold, GraphicsUnit.Pixel);
            }
            catch
            {
                font = new Font("Segoe MDL2 Assets", 20, FontStyle.Bold, GraphicsUnit.Pixel);
            }

            var format = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };

            g.DrawString(glyph, font, Brushes.White, new RectangleF(0, 0, 32, 32), format);

            return bmp.GetHicon();
        }
        catch
        {
            return IntPtr.Zero;
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        if (_hIconPrevious != IntPtr.Zero) User32.DestroyIcon(new HICON(_hIconPrevious));
        if (_hIconPlay != IntPtr.Zero) User32.DestroyIcon(new HICON(_hIconPlay));
        if (_hIconPause != IntPtr.Zero) User32.DestroyIcon(new HICON(_hIconPause));
        if (_hIconNext != IntPtr.Zero) User32.DestroyIcon(new HICON(_hIconNext));
    }
}
