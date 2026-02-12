using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Events;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Windows.Foundation;

namespace BetterLyrics.WinUI3.Hooks
{
    public partial class TaskbarHook : IDisposable
    {
        private readonly Action<TaskbarFreeBoundsChangedEventArgs> _onLayoutChanged;
        private Timer? _scanTimer;
        private Rect _lastValidRect;
        private bool _isDisposed;

        private TaskbarPlacement _currentPlacement;

        private const int ScanInterval = 1000;
        private const int ColorTolerance = 15;
        private const int MinValidWidth = 50;

        public TaskbarHook(TaskbarPlacement placement, Action<TaskbarFreeBoundsChangedEventArgs> onLayoutChanged)
        {
            _currentPlacement = placement;
            _onLayoutChanged = onLayoutChanged;
            _scanTimer = new Timer(ScanLoop, null, 1000, ScanInterval);

            ScanLoop(null);
        }

        public void UpdatePlacement(TaskbarPlacement newPlacement)
        {
            _currentPlacement = newPlacement;
            ScanLoop(null);
        }

        private void ScanLoop(object? state)
        {
            if (_isDisposed) return;

            try
            {
                Rectangle bounds = GetTaskbarBounds();
                if (bounds.IsEmpty || bounds.Width <= 0 || bounds.Height <= 0) return;

                using (Bitmap bmp = new Bitmap(bounds.Width, bounds.Height))
                {
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.CopyFromScreen(bounds.Location, System.Drawing.Point.Empty, bounds.Size);
                    }

                    var freeRect = AnalyzeFreeSpace(bmp, bounds, _lastValidRect);

                    if (freeRect != Rect.Empty && ShouldUpdate(freeRect, _lastValidRect))
                    {
                        _onLayoutChanged?.Invoke(new TaskbarFreeBoundsChangedEventArgs(freeRect));
                        _lastValidRect = freeRect;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TaskbarHook] Scan Error: {ex.Message}");
            }
        }

        private bool ShouldUpdate(Rect newRect, Rect oldRect)
        {
            if (oldRect == Rect.Empty) return true;
            return Math.Abs(newRect.X - oldRect.X) > 5 || Math.Abs(newRect.Width - oldRect.Width) > 5;
        }

        private Rect AnalyzeFreeSpace(Bitmap bmp, Rectangle globalBounds, Rect currentWindowRect)
        {
            int width = bmp.Width;
            int height = bmp.Height;

            BitmapData bData = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            List<Rect> candidates = new List<Rect>();

            try
            {
                unsafe
                {
                    byte* ptr = (byte*)bData.Scan0;
                    int stride = bData.Stride;

                    int sampleX = width - 5;
                    if (sampleX < 0) sampleX = 0;
                    int sampleY = height / 2;

                    byte baseB = ptr[sampleY * stride + sampleX * 4];
                    byte baseG = ptr[sampleY * stride + sampleX * 4 + 1];
                    byte baseR = ptr[sampleY * stride + sampleX * 4 + 2];

                    bool[] isColumnFree = new bool[width];
                    int yStart = 2;
                    int yEnd = height - 2;

                    int ignoreStartX = -1, ignoreEndX = -1;
                    if (currentWindowRect != Rect.Empty)
                    {
                        ignoreStartX = (int)(currentWindowRect.X - globalBounds.Left);
                        ignoreEndX = (int)(ignoreStartX + currentWindowRect.Width);
                    }

                    for (int x = 0; x < width; x++)
                    {
                        if (x >= ignoreStartX && x <= ignoreEndX)
                        {
                            isColumnFree[x] = true;
                            continue;
                        }

                        bool columnHasIcon = false;
                        for (int y = yStart; y < yEnd; y++)
                        {
                            int offset = y * stride + x * 4;
                            if (!IsColorSimilar(ptr[offset], ptr[offset + 1], ptr[offset + 2], baseB, baseG, baseR, ColorTolerance))
                            {
                                columnHasIcon = true;
                                break;
                            }
                        }
                        isColumnFree[x] = !columnHasIcon;
                    }

                    int currentStart = 0;
                    int currentLen = 0;
                    int padding = 4;

                    for (int x = 0; x < width; x++)
                    {
                        if (isColumnFree[x])
                        {
                            if (currentLen == 0) currentStart = x;
                            currentLen++;
                        }
                        else
                        {
                            if (currentLen >= MinValidWidth)
                            {
                                int finalX = globalBounds.Left + currentStart + padding;
                                int finalW = currentLen - (padding * 2);
                                if (finalW > 0)
                                    candidates.Add(new Rect(finalX, globalBounds.Top, finalW, globalBounds.Height));
                            }
                            currentLen = 0;
                        }
                    }
                    if (currentLen >= MinValidWidth)
                    {
                        int finalX = globalBounds.Left + currentStart + padding;
                        int finalW = currentLen - (padding * 2);
                        if (finalW > 0)
                            candidates.Add(new Rect(finalX, globalBounds.Top, finalW, globalBounds.Height));
                    }
                }
            }
            finally
            {
                bmp.UnlockBits(bData);
            }

            return SelectBestCandidate(candidates, _currentPlacement);
        }

        private Rect SelectBestCandidate(List<Rect> candidates, TaskbarPlacement placement)
        {
            if (candidates == null || candidates.Count == 0) return Rect.Empty;

            if (candidates.Count == 1) return candidates[0];

            switch (placement)
            {
                case TaskbarPlacement.Left:
                    return candidates.OrderBy(r => r.X).First();

                case TaskbarPlacement.Right:
                    return candidates.OrderByDescending(r => r.X).First();

                case TaskbarPlacement.Auto:
                default:
                    return candidates.OrderByDescending(r => r.Width).First();
            }
        }

        private bool IsColorSimilar(byte b1, byte g1, byte r1, byte b2, byte g2, byte r2, int tolerance)
        {
            int diff = Math.Abs(b1 - b2) + Math.Abs(g1 - g2) + Math.Abs(r1 - r2);
            return diff < tolerance * 3;
        }

        #region Win32 API
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string? lpWindowName);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private Rectangle GetTaskbarBounds()
        {
            IntPtr hWnd = FindWindow("Shell_TrayWnd", null);
            if (hWnd == IntPtr.Zero) return Rectangle.Empty;

            if (GetWindowRect(hWnd, out RECT rect))
            {
                return new Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
            }
            return Rectangle.Empty;
        }
        #endregion

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            _scanTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            _scanTimer?.Dispose();
            _scanTimer = null;
        }
    }
}