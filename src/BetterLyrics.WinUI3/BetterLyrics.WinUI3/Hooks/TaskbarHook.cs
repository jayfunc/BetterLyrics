using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Models.Domain;
using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.DependencyInjection;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using Microsoft.Extensions.Logging;
using Vanara.PInvoke;
using WinRT.Interop;

namespace BetterLyrics.WinUI3.Hooks;

public partial class TaskbarHook : IDisposable
{
    private readonly IAppUIThreadProvider _appUIThreadProvider =
        Ioc.Default.GetRequiredService<IAppUIThreadProvider>();

    private readonly UIA3Automation _automation;
    private readonly ILogger<TaskbarHook> _logger = Ioc.Default.GetRequiredService<ILogger<TaskbarHook>>();
    private readonly AsyncPoller _poller = new();

    private readonly Debouncer _positionDebouncer = new();
    private readonly IntPtr _targetHwnd;
    private readonly AppRect _targetMonitorRect;

    private readonly NowPlayingWindow _targetWindow;

    private readonly Dictionary<HWND, TrackedWindowInfo> _trackedThirdPartyWindows = new();

    private readonly IWindowManagerProvider _windowManagerProvider =
        Ioc.Default.GetRequiredService<IWindowManagerProvider>();

    private AutomationElement? _cachedTaskbar;

    private TaskbarPlacement _currentPlacement;
    private bool _isDisposed;
    private IntPtr _taskbarHwnd;

    public TaskbarHook(NowPlayingWindow window, TaskbarPlacement placement, AppRect targetMonitorRect)
    {
        _targetWindow = window;
        _targetHwnd = WindowNative.GetWindowHandle(_targetWindow);

        _automation = new UIA3Automation();

        _currentPlacement = placement;
        _targetMonitorRect = targetMonitorRect;

        StartHook();
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        //_logger.LogInformation("Disposing TaskbarHook...");
        _isDisposed = true;

        _poller.Stop();
        _positionDebouncer.Dispose();

        if (_targetHwnd != IntPtr.Zero)
        {
            var windowBounds = _targetWindow.LyricsWindowStatus.WindowBounds;

            User32.SetParent(_targetHwnd, HWND.NULL);
            _windowManagerProvider.SetIsChildWindow(_targetWindow, false);
            _windowManagerProvider.MoveAndResize(_targetWindow, windowBounds);
        }

        _ = Task.Run(() =>
        {
            try
            {
                _automation?.Dispose();
                //_logger.LogInformation("FlaUI Automation disposed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background Dispose Error for FlaUI Automation.");
            }
        });
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
        if (_cachedTaskbar != null)
            try
            {
                // 简单访问一下属性测试对象是否存活（比如 explorer.exe 没有崩溃重启）
                var test = _cachedTaskbar.BoundingRectangle;
                return _cachedTaskbar;
            }
            catch
            {
                // 对象已失效（例如 explorer.exe 重启了），清空缓存重新查找
                _cachedTaskbar = null;
            }

        try
        {
            AutomationElement? target = null;

            var desktop = _automation.GetDesktop();
            var primaryTaskbar = desktop.FindFirstChild(x => x.ByClassName("Shell_TrayWnd"));

            // 如果外部还没传入显示器范围，默认使用主任务栏
            if (_targetMonitorRect == AppRect.Empty)
            {
                target = primaryTaskbar;
            }
            // 检查主任务栏是否刚好在目标显示器上
            else if (primaryTaskbar != null && IsTaskbarOnMonitor(primaryTaskbar))
            {
                target = primaryTaskbar;
            }
            else
            {
                // 遍历所有的副屏任务栏
                var secondaryTaskbars = desktop.FindAllChildren(cf => cf.ByClassName("Shell_SecondaryTrayWnd"));
                foreach (var taskbar in secondaryTaskbars)
                    if (IsTaskbarOnMonitor(taskbar))
                    {
                        target = taskbar;
                        break;
                    }
            }

            // 如果都没匹配上，使用主任务栏
            if (target == null) target = primaryTaskbar;

            _cachedTaskbar = target; // 存入缓存
            return target;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while finding target taskbar.");
            return null;
        }
    }

    private bool IsTaskbarOnMonitor(AutomationElement taskbarElement)
    {
        try
        {
            var rect = taskbarElement.BoundingRectangle.ToAppRect();
            // 只要任务栏和目标显示器有交集，就认为它属于该显示器
            return rect.IntersectsWith(_targetMonitorRect);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to get BoundingRectangle for taskbar element to check monitor intersection.");
            return false;
        }
    }

    private void StartHook()
    {
        try
        {
            var taskbar = FindTargetTaskbar();

            if (taskbar == null) return;

            _taskbarHwnd = taskbar.Properties.NativeWindowHandle.ValueOrDefault;
            if (_taskbarHwnd != IntPtr.Zero) AttachToTaskbar(_taskbarHwnd);

            _poller.Start(async token => await Task.Run(() => RequestUpdate(), token));

            RequestUpdate();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hook Init Failed in StartHook!");
        }
    }

    private void RequestUpdate()
    {
        if (_isDisposed) return;

        var taskbar = FindTargetTaskbar();

        if (taskbar == null) return;

        _ = _positionDebouncer.RunAsync(() =>
        {
            _ = Task.Run(() =>
            {
                var voidRect = CalculateVoidRect(taskbar, _currentPlacement);
                //Debug.WriteLine($"Calculated void rectangle: {voidRect}");

                if (!_isDisposed && voidRect != Rectangle.Empty)
                {
                    Rectangle taskbarRect;
                    try
                    {
                        taskbarRect = taskbar.BoundingRectangle;
                        //Debug.WriteLine($"Taskbar Bounding Rectangle: {taskbarRect}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex,
                            "Failed to get _taskbar.BoundingRectangle inside RequestUpdate. COM object might be disconnected.");
                        return;
                    }

                    var relativeX = voidRect.Left - taskbarRect.Left;
                    var relativeY = voidRect.Top - taskbarRect.Top;

                    _appUIThreadProvider.Execute(() =>
                    {
                        if (!_isDisposed)
                            User32.SetWindowPos(
                                _targetHwnd, HWND.HWND_TOPMOST,
                                relativeX, relativeY, voidRect.Width, voidRect.Height,
                                User32.SetWindowPosFlags.SWP_NOACTIVATE | User32.SetWindowPosFlags.SWP_NOZORDER);
                    });
                }
            });
        }, 100);
    }

    private Rectangle CalculateVoidRect(AutomationElement? taskbar, TaskbarPlacement placement)
    {
        try
        {
            if (taskbar == null) return Rectangle.Empty;

            try
            {
                var _ = taskbar.BoundingRectangle;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Taskbar bounding rectangle access failed");
                return Rectangle.Empty;
            }

            var taskbarRect = taskbar.BoundingRectangle;
            if (taskbarRect.Width <= 0) return Rectangle.Empty;

            var taskbarTop = taskbarRect.Top;
            var taskbarBottom = taskbarRect.Bottom;

            var taskbarCenter = taskbarRect.Left + taskbarRect.Width / 2;
            List<(int Left, int Right)> occupiedSegments = new();

            // 托盘区域
            var tray = taskbar.FindFirstChild(x => x.ByClassName("TrayNotifyWnd")); // Win 10/11
            if (tray != null)
            {
                var rect = tray.BoundingRectangle;
                occupiedSegments.Add((rect.Left, rect.Right));
            }

            // 任务栏固定的按钮
            AutomationElement? pinned = null;
            if (SystemHelper.IsWindows11OrGreater) // Win 11
            {
                var inputSite =
                    taskbar.FindFirstChild(x => x.ByClassName("Windows.UI.Input.InputSite.WindowClass"));
                pinned = inputSite?.FindFirstChild(x => x.ByClassName("Taskbar.TaskbarFrameAutomationPeer"));
            }
            else // Win 10
            {
                pinned = taskbar.FindFirstChild(x => x.ByClassName("MSTaskListWClass"));
            }

            if (pinned != null)
            {
                var children = pinned.FindAllChildren();
                foreach (var child in children)
                {
                    var rect = child.BoundingRectangle;
                    if (rect.Width > 0 && rect.Height > 0) occupiedSegments.Add((rect.Left, rect.Right));
                }
            }

            // 直接子元素（via FlaUI3）
            //Debug.WriteLine("=== Enumerating child windows of taskbar via FlaUI... ===");
            var directChildrenByFlaUI = taskbar.FindAllChildren();
            foreach (var child in directChildrenByFlaUI)
                if (child.Properties.ClassName.TryGetValue(out var className))
                {
                    if ( // Win 10/11
                        className == "TrayNotifyWnd" ||
                        // Win 10
                        className == "MSTaskListWClass" ||
                        // Win 11
                        className == "MSTaskSwWClass" ||
                        className == "Windows.UI.Input.InputSite.WindowClass" ||
                        // 待定
                        className == "Xaml_WindowedPopupClass")
                        continue; // 跳过系统组件窗口
                    // 这里不对 ClassName 为 Start 或 TrayButton 的进行拦截
                    // 因为在 Win 10 中，开始按钮、Cortana 等组件为任务栏的直接子元素，不包含在 MsTaskListWClass 中
                    if (child.Properties.NativeWindowHandle.ValueOrDefault == _targetHwnd) continue; // 跳过自身窗口

                    var rect = child.BoundingRectangle;
                    //Debug.WriteLine($"FlaUI found child window: {className}, Rect: {rect}");
                    if (rect.Width > 0 && rect.Height > 0 && rect.Top >= taskbarTop && rect.Bottom <= taskbarBottom)
                        occupiedSegments.Add((rect.Left, rect.Right));
                }

            // 直接子元素（via Win32）
            //_logger.LogInformation("=== Enumerating child windows of taskbar via Win32 API... ===");
            // Debug.WriteLine("=== Enumerating child windows of taskbar via Win32 API... ===");
            var subWindows = User32.EnumChildWindows(_taskbarHwnd);
            HashSet<HWND> currentVisibleHwnds = [];
            foreach (var hwnd in subWindows)
            {
                if (hwnd == _targetHwnd) continue; // 跳过自身窗口

                StringBuilder classNameBuilder = new(256);
                _ = User32.GetClassName(hwnd, classNameBuilder, classNameBuilder.Capacity);
                var className = classNameBuilder.ToString();

                if ( // 共有
                    className == "InputNonClientPointerSource" ||
                    className == "Microsoft.UI.Content.DesktopChildSiteBridge" ||
                    className == "InputSiteWindowClass" ||
                    className == "Start" ||
                    className == "TrayDummySearchControl" ||
                    className == "TrayNotifyWnd" ||
                    className == "ReBarWindow32" ||
                    className == "MSTaskSwWClass" ||
                    className == "MSTaskListWClass" ||

                    // Win 11
                    className == "Windows.UI.Core.CoreWindow" ||
                    className == "Windows.UI.Composition.DesktopWindowContentBridge" ||
                    className == "Windows.UI.Input.InputSite.WindowClass" ||

                    // Win 10
                    className == "DynamicContent1" ||
                    className == "Button" ||
                    className == "Static" ||
                    className == "ToolbarWindow32" ||
                    className == "TrayButton" ||
                    className == "DynamicContent2" ||
                    className == "SysPager" ||
                    className == "PenWorkspaceButton" ||
                    className == "TrayInputIndicatorWClass" ||
                    className == "IMEModeButton" ||
                    className == "InputIndicatorButton" ||
                    className == "TrayClockWClass" ||
                    className == "TrayShowDesktopButtonWClass")
                    continue; // 跳过系统组件窗口

                if (!User32.IsWindowVisible(hwnd)) continue; // 跳过不可见窗口

                if (User32.GetWindowRect(hwnd, out var rect))
                {
                    var width = rect.Right - rect.Left;
                    var height = rect.Bottom - rect.Top;
                    //_logger.LogInformation("Found child window: {ClassName}", className);
                    // Debug.WriteLine($"Found child window: {className}, Rect: {rect}");
                    if (width > 0 && height > 0 && rect.Top >= taskbarTop && rect.Bottom <= taskbarBottom)
                    {
                        occupiedSegments.Add((rect.Left, rect.Right));
                        currentVisibleHwnds.Add(hwnd);
                        _trackedThirdPartyWindows[hwnd] = new TrackedWindowInfo
                        {
                            ClassName = className,
                            LastRect = rect,
                            LastSeen = DateTime.Now
                        };
                    }
                }
            }

            List<HWND> hwndsToRemove = [];
            foreach (var kvp in _trackedThirdPartyWindows)
            {
                var trackedHwnd = kvp.Key;
                var info = kvp.Value;

                // 如果这个第三方窗口在这个周期里突然不在任务栏的可见子窗口列表里了
                if (!currentVisibleHwnds.Contains(trackedHwnd))
                {
                    var keepTracking = false;

                    // 尝试全局追踪检查（它可能只是被取消了 WS_VISIBLE 或临时剥离了父级）
                    // 只要句柄没被系统彻底销毁，GetWindowRect 通常能拿到它最后一次的物理坐标
                    if (User32.IsWindow(trackedHwnd))
                        if (User32.GetWindowRect(trackedHwnd, out var globalRect))
                        {
                            var globalWidth = globalRect.Right - globalRect.Left;
                            var globalHeight = globalRect.Bottom - globalRect.Top;

                            // 只要物理坐标还在任务栏的 Y 轴高度范围内，就判定还在占位
                            if (globalWidth > 0 && globalHeight > 0 && globalRect.Top >= taskbarTop &&
                                globalRect.Bottom <= taskbarBottom)
                            {
                                occupiedSegments.Add((globalRect.Left, globalRect.Right));
                                info.LastRect = new Rectangle(globalRect.Left, globalRect.Top, globalWidth,
                                    globalHeight);
                                info.LastSeen = DateTime.Now; // 句柄仍存活着且坐标合理，刷新存活时间
                                keepTracking = true;
                            }
                        }

                    // 尝试幽灵滞留防抖（纯时间衰减）
                    // 走到这里说明句柄真的被销毁了，或者坐标飞到了屏幕外（比如被移到了极坐标区隐藏）
                    if (!keepTracking)
                    {
                        var timeSinceLost = DateTime.Now - info.LastSeen;

                        // 纯靠时间防抖
                        // 哪怕用户一直开着开始菜单，只要上面的 IsWindow 策略没拦住，这里也能顶 2.5 秒不闪烁
                        if (timeSinceLost.TotalSeconds < 2.5)
                        {
                            occupiedSegments.Add((info.LastRect.Left, info.LastRect.Right));
                            keepTracking = true;
                            // 这里不能更新 LastSeen，直到超时让它自然消亡
                        }
                    }

                    // 超时放弃，彻底清理
                    if (!keepTracking) hwndsToRemove.Add(trackedHwnd);
                }
            }

            // 清理那些已经离开超过 2.5 秒的僵尸句柄
            foreach (var hwnd in hwndsToRemove) _trackedThirdPartyWindows.Remove(hwnd);

            // 将所有被占用的区域按左边界排序并合并重叠部分
            occupiedSegments = occupiedSegments.OrderBy(s => s.Left).ToList();
            List<(int Left, int Right)> mergedSegments = [];

            foreach (var seg in occupiedSegments)
                if (mergedSegments.Count == 0)
                {
                    mergedSegments.Add(seg);
                }
                else
                {
                    var last = mergedSegments[mergedSegments.Count - 1];
                    if (seg.Left <= last.Right) // 有重叠或相连
                        mergedSegments[mergedSegments.Count - 1] = (last.Left, Math.Max(last.Right, seg.Right));
                    else
                        mergedSegments.Add(seg);
                }

            // 提取所有连续的空白区域
            var voids = new List<Rectangle>();
            var currentX = taskbarRect.Left;
            var padding = 0; // 左右边距

            foreach (var seg in mergedSegments)
            {
                var gapWidth = seg.Left - currentX;
                var actualWidth = gapWidth - 2 * padding;
                if (actualWidth > 20) // 过滤掉太小的缝隙
                    voids.Add(new Rectangle(currentX + padding, taskbarRect.Top, actualWidth, taskbarRect.Height));

                currentX = Math.Max(currentX, seg.Right);
            }

            // 检查最后一个占用段到任务栏右边缘的空白
            var finalGapWidth = taskbarRect.Right - currentX;
            if (finalGapWidth - 2 * padding > 20)
                voids.Add(new Rectangle(currentX + padding, taskbarRect.Top, finalGapWidth - 2 * padding,
                    taskbarRect.Height));

            if (voids.Count == 0)
                //_logger.LogWarning("No available voids found on the taskbar.");
                return Rectangle.Empty;

            // 根据用户偏好和空白区域长度进行筛选
            // 将空白区按中心点分类为 Left 和 Right，并按宽度降序排列
            var leftVoids = voids.Where(v => v.Left + v.Width / 2 < taskbarCenter).OrderByDescending(v => v.Width)
                .ToList();
            var rightVoids = voids.Where(v => v.Left + v.Width / 2 >= taskbarCenter)
                .OrderByDescending(v => v.Width).ToList();

            var bestLeft = leftVoids.FirstOrDefault();
            var bestRight = rightVoids.FirstOrDefault();

            // 如果某一侧完全没有空白，则使用另一侧的最优空白
            if (bestLeft == Rectangle.Empty && bestRight != Rectangle.Empty) bestLeft = bestRight;
            if (bestRight == Rectangle.Empty && bestLeft != Rectangle.Empty) bestRight = bestLeft;

            // 全局最宽的空白（用于 Center / Auto 偏好）
            var widestOverall = voids.OrderByDescending(v => v.Width).First();

            switch (placement)
            {
                case TaskbarPlacement.Left:
                    return bestLeft != Rectangle.Empty ? bestLeft : widestOverall;

                case TaskbarPlacement.Right:
                    return bestRight != Rectangle.Empty ? bestRight : widestOverall;

                case TaskbarPlacement.Center:
                case TaskbarPlacement.Auto:
                default:
                    return widestOverall;
            }
        }
        catch (Exception ex)
        {
            //_logger.LogError(ex, "Error calculating taskbar voids.");
            return Rectangle.Empty;
        }
    }

    private void AttachToTaskbar(IntPtr taskbarHwnd)
    {
        if (taskbarHwnd == IntPtr.Zero || _targetHwnd == IntPtr.Zero) return;

        _windowManagerProvider.SetIsChildWindow(_targetWindow, true);
        User32.SetParent(_targetHwnd, taskbarHwnd);
    }
}