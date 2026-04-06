using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using CommunityToolkit.WinUI;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vanara.PInvoke;

namespace BetterLyrics.WinUI3.Hooks
{
    public partial class TaskbarHook : IDisposable
    {
        private readonly UIA3Automation _automation;
        private AutomationElement? _taskbar;

        private readonly Microsoft.UI.Xaml.Window _targetWindow;
        private readonly IntPtr _targetHwnd;
        private IntPtr _taskbarHwnd;

        private TaskbarPlacement _lastAutoPlacement = TaskbarPlacement.Right;
        private TaskbarPlacement _currentPlacement;
        private Rectangle _targetMonitorRect;

        private readonly DispatcherQueue? _dispatcherQueue;
        private readonly DispatcherQueueTimer? _debounceTimer;
        private readonly DispatcherQueueTimer? _pollingTimer;
        private bool _isDisposed;

        public TaskbarHook(Microsoft.UI.Xaml.Window window, TaskbarPlacement placement, Rectangle targetMonitorRect)
        {
            _targetWindow = window;
            _targetHwnd = WinRT.Interop.WindowNative.GetWindowHandle(_targetWindow);

            _automation = new UIA3Automation();
            _dispatcherQueue = DispatcherQueueHelper.Instance;

            _debounceTimer = _dispatcherQueue?.CreateTimer();

            _pollingTimer = _dispatcherQueue?.CreateTimer();
            if (_pollingTimer != null)
            {
                _pollingTimer.Interval = TimeSpan.FromMilliseconds(1000);
                _pollingTimer.Tick += (s, e) => RequestUpdate();
            }

            _currentPlacement = placement;
            _targetMonitorRect = targetMonitorRect;

            StartHook();
        }

        public void UpdatePlacement(TaskbarPlacement newPlacement)
        {
            if (_currentPlacement != newPlacement)
            {
                _currentPlacement = newPlacement;
                RequestUpdate();
            }
        }

        private AutomationElement? FindTargetTaskbar()
        {
            var desktop = _automation.GetDesktop();
            var primaryTaskbar = desktop.FindFirstChild(cf => cf.ByClassName("Shell_TrayWnd"));

            // 如果外部还没传入显示器范围，默认使用主任务栏
            if (_targetMonitorRect == Rectangle.Empty)
                return primaryTaskbar;

            // 检查主任务栏是否刚好在目标显示器上
            if (primaryTaskbar != null && IsTaskbarOnMonitor(primaryTaskbar))
            {
                return primaryTaskbar;
            }

            // 遍历所有的副屏任务栏
            var secondaryTaskbars = desktop.FindAllChildren(cf => cf.ByClassName("Shell_SecondaryTrayWnd"));
            foreach (var taskbar in secondaryTaskbars)
            {
                if (IsTaskbarOnMonitor(taskbar))
                {
                    return taskbar;
                }
            }

            // 如果都没匹配上，返回主任务栏防止崩溃
            return primaryTaskbar;
        }

        private bool IsTaskbarOnMonitor(AutomationElement taskbarElement)
        {
            try
            {
                var rect = taskbarElement.BoundingRectangle;
                // 只要任务栏和目标显示器有交集，就认为它属于该显示器
                return rect.IntersectsWith(_targetMonitorRect);
            }
            catch
            {
                return false;
            }
        }

        private void StartHook()
        {
            try
            {
                _taskbar = FindTargetTaskbar();

                if (_taskbar == null) return;

                _taskbarHwnd = _taskbar.Properties.NativeWindowHandle.ValueOrDefault;
                if (_taskbarHwnd != IntPtr.Zero)
                {
                    AttachToTaskbar(_taskbarHwnd);
                }

                _pollingTimer?.Start();

                RequestUpdate();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Hook Init Failed: {ex.Message}");
            }
        }

        private void RequestUpdate()
        {
            if (_isDisposed || _taskbar == null) return;

            _debounceTimer?.Debounce(() =>
            {
                _ = Task.Run(() =>
                {
                    Rectangle voidRect = CalculateVoidRect(_currentPlacement);

                    if (!_isDisposed && voidRect != Rectangle.Empty)
                    {
                        Rectangle taskbarRect;
                        try
                        {
                            taskbarRect = _taskbar.BoundingRectangle;
                        }
                        catch
                        {
                            return;
                        }

                        int relativeX = voidRect.Left - taskbarRect.Left;
                        int relativeY = voidRect.Top - taskbarRect.Top;

                        _dispatcherQueue?.TryEnqueue(() =>
                        {
                            if (!_isDisposed)
                            {
                                User32.SetWindowPos((HWND)_targetHwnd, HWND.HWND_TOPMOST,
                                    relativeX, relativeY, voidRect.Width, voidRect.Height,
                                    User32.SetWindowPosFlags.SWP_NOACTIVATE | User32.SetWindowPosFlags.SWP_NOZORDER);
                            }
                        });
                    }
                });
            }, TimeSpan.FromMilliseconds(100));
        }

        private Rectangle CalculateVoidRect(TaskbarPlacement placement)
        {
            if (_taskbar == null) return Rectangle.Empty;

            try { var _ = _taskbar.BoundingRectangle; }
            catch
            {
                _taskbar = FindTargetTaskbar();
                if (_taskbar == null)
                {
                    return Rectangle.Empty;
                }

                _taskbarHwnd = _taskbar.Properties.NativeWindowHandle.ValueOrDefault;
                if (_taskbarHwnd != IntPtr.Zero)
                {
                    AttachToTaskbar(_taskbarHwnd);
                }
            }

            Rectangle taskbarRect = _taskbar.BoundingRectangle;
            if (taskbarRect.Width <= 0)
            {
                return Rectangle.Empty;
            }

            int taskbarCenter = taskbarRect.Left + taskbarRect.Width / 2;

            List<(int Left, int Right)> occupiedSegments = new List<(int, int)>();

            // 托盘区域 TrayNotifyWnd
            var tray = _taskbar.FindFirstChild(cf => cf.ByClassName("TrayNotifyWnd"));
            if (tray != null && !tray.IsOffscreen)
            {
                occupiedSegments.Add((tray.BoundingRectangle.Left, tray.BoundingRectangle.Right));
            }

            // 任务栏固定的按钮 TaskbarFrameAutomationPeer
            var frames = _taskbar.FindAllDescendants(cf => cf.ByClassName("Taskbar.TaskbarFrameAutomationPeer"));
            foreach (var frame in frames)
            {
                var children = frame.FindAllChildren();
                foreach (var child in children)
                {
                    if (child.IsOffscreen) continue;
                    var rect = child.BoundingRectangle;
                    if (rect.Width > 0 && rect.Height > 0)
                    {
                        occupiedSegments.Add((rect.Left, rect.Right));
                    }
                }
            }

            // 独立窗口控件
            if (_taskbarHwnd != IntPtr.Zero)
            {
                User32.EnumChildWindows((HWND)_taskbarHwnd, (hwnd, lParam) =>
                {
                    // 排除自身窗口
                    if ((IntPtr)hwnd == _targetHwnd)
                        return true;

                    // 过滤掉不可见/隐藏的窗口
                    if (!User32.IsWindowVisible(hwnd))
                        return true;

                    // 仅关注直接挂载在任务栏下的直接子窗口
                    if ((IntPtr)User32.GetParent(hwnd) != _taskbarHwnd)
                        return true;

                    // 获取窗口类名进行过滤
                    StringBuilder sb = new StringBuilder(256);
                    User32.GetClassName(hwnd, sb, sb.Capacity);
                    string className = sb.ToString();

                    // 排除 Windows 任务栏自身的系统组件窗口
                    if (className == "TrayNotifyWnd" ||
                        className == "MSTaskSwWClass" ||
                        className == "Windows.UI.Input.InputSite.WindowClass" ||
                        className == "Windows.UI.Composition.DesktopWindowContentBridge" || // Win11 任务栏桥接层
                        className == "ReBarWindow32" || // Win10 任务栏容器
                        className == "SysPager")
                    {
                        return true; // 继续遍历下一个
                    }

                    // 获取第三方窗口的位置并记录占用的区域
                    if (User32.GetWindowRect(hwnd, out var rect))
                    {
                        if (rect.Width > 0 && rect.Height > 0)
                        {
                            occupiedSegments.Add((rect.left, rect.right));
                        }
                    }

                    return true;
                }, IntPtr.Zero);
            }

            // 将所有被占用的区域按左边界排序并合并重叠部分
            occupiedSegments = occupiedSegments.OrderBy(s => s.Left).ToList();
            List<(int Left, int Right)> mergedSegments = new List<(int, int)>();

            foreach (var seg in occupiedSegments)
            {
                if (mergedSegments.Count == 0)
                {
                    mergedSegments.Add(seg);
                }
                else
                {
                    var last = mergedSegments[mergedSegments.Count - 1];
                    if (seg.Left <= last.Right) // 有重叠或相连
                    {
                        mergedSegments[mergedSegments.Count - 1] = (last.Left, Math.Max(last.Right, seg.Right));
                    }
                    else
                    {
                        mergedSegments.Add(seg);
                    }
                }
            }

            // 提取所有连续的空白区域
            List<Rectangle> voids = new List<Rectangle>();
            int currentX = taskbarRect.Left;
            int padding = 0; // 左右边距

            foreach (var seg in mergedSegments)
            {
                int gapWidth = seg.Left - currentX;
                int actualWidth = gapWidth - (2 * padding);
                if (actualWidth > 20) // 过滤掉太小的缝隙
                {
                    voids.Add(new Rectangle(currentX + padding, taskbarRect.Top, actualWidth, taskbarRect.Height));
                }
                currentX = Math.Max(currentX, seg.Right);
            }

            // 检查最后一个占用段到任务栏右边缘的空白
            int finalGapWidth = taskbarRect.Right - currentX;
            if (finalGapWidth - (2 * padding) > 20)
            {
                voids.Add(new Rectangle(currentX + padding, taskbarRect.Top, finalGapWidth - (2 * padding), taskbarRect.Height));
            }

            if (voids.Count == 0)
            {
                return Rectangle.Empty;
            }

            // 根据用户偏好和空白区域长度进行筛选
            // 将空白区按中心点分类为 Left 和 Right，并按宽度降序排列
            var leftVoids = voids.Where(v => (v.Left + v.Width / 2) < taskbarCenter).OrderByDescending(v => v.Width).ToList();
            var rightVoids = voids.Where(v => (v.Left + v.Width / 2) >= taskbarCenter).OrderByDescending(v => v.Width).ToList();

            Rectangle bestLeft = leftVoids.FirstOrDefault();
            Rectangle bestRight = rightVoids.FirstOrDefault();

            // 如果某一侧完全没有空白，则使用另一侧的最优空白
            if (bestLeft == Rectangle.Empty && bestRight != Rectangle.Empty) bestLeft = bestRight;
            if (bestRight == Rectangle.Empty && bestLeft != Rectangle.Empty) bestRight = bestLeft;

            // 全局最宽的空白（用于 Center 偏好）
            Rectangle widestOverall = voids.OrderByDescending(v => v.Width).First();

            switch (placement)
            {
                case TaskbarPlacement.Left:
                    return bestLeft != Rectangle.Empty ? bestLeft : widestOverall;

                case TaskbarPlacement.Right:
                    return bestRight != Rectangle.Empty ? bestRight : widestOverall;

                case TaskbarPlacement.Center:
                    // 居中模式直接返回全局最长的那个空白段
                    return widestOverall;

                case TaskbarPlacement.Auto:
                default:
                    int threshold = 50;
                    int minRequiredWidth = 100;

                    // 平滑切换防抖逻辑
                    if (_lastAutoPlacement == TaskbarPlacement.Left)
                    {
                        // 如果左侧最优解不够宽，或者右侧最优解比左侧长出太多，则切换到右侧
                        if (bestLeft.Width < minRequiredWidth || bestRight.Width > bestLeft.Width + threshold)
                        {
                            _lastAutoPlacement = TaskbarPlacement.Right;
                            return bestRight;
                        }
                        return bestLeft;
                    }
                    else
                    {
                        // 反之同理
                        if (bestRight.Width < minRequiredWidth || bestLeft.Width > bestRight.Width + threshold)
                        {
                            _lastAutoPlacement = TaskbarPlacement.Left;
                            return bestLeft;
                        }
                        return bestRight;
                    }
            }
        }

        private void AttachToTaskbar(IntPtr taskbarHwnd)
        {
            if (taskbarHwnd == IntPtr.Zero || _targetHwnd == IntPtr.Zero) return;

            _targetWindow.SetIsChildWindow(true);

            User32.SetParent((HWND)_targetHwnd, (HWND)taskbarHwnd);
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            _pollingTimer?.Stop();

            if (_targetHwnd != IntPtr.Zero)
            {
                User32.SetParent((HWND)_targetHwnd, HWND.NULL);
                _targetWindow.SetIsChildWindow(false);
            }

            _ = Task.Run(() =>
            {
                try
                {
                    _automation?.Dispose();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Background Dispose Error: {ex.Message}");
                }
            });
        }
    }
}