using BetterLyrics.WinUI3.Events;
using BetterLyrics.WinUI3.Extensions;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.EventHandlers;
using FlaUI.UIA3;
using FlaUI.UIA3.Extensions;
using Microsoft.UI.Dispatching;
using System;
using System.Drawing;
using System.Threading;

namespace BetterLyrics.WinUI3.Hooks
{
    public class TaskbarHook : IDisposable
    {
        private readonly UIA3Automation _automation;
        private AutomationElement? _taskbar;

        private StructureChangedEventHandlerBase? _structureHandler;
        private PropertyChangedEventHandlerBase? _propertyHandler;

        private readonly DispatcherQueue _dispatcherQueue;
        private readonly Action<TaskbarFreeBoundsChangedEventArgs> _onLayoutChanged;
        private Timer? _debounceTimer;
        private const int DebounceDelay = 150;
        private bool _isDisposed;

        public TaskbarHook(Action<TaskbarFreeBoundsChangedEventArgs> onLayoutChanged)
        {
            _automation = new UIA3Automation();
            _onLayoutChanged = onLayoutChanged;
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            StartHook();
        }

        private void StartHook()
        {
            try
            {
                var desktop = _automation.GetDesktop();
                _taskbar = desktop.FindFirstChild(cf => cf.ByClassName("Shell_TrayWnd"));

                if (_taskbar == null) return;

                // 监听结构变化
                // 这里的返回值就是一个可以 Dispose 的对象
                _structureHandler = _taskbar.RegisterStructureChangedEvent(
                    TreeScope.Descendants,
                    (element, type, id) => RequestUpdate());

                // 监听属性变化
                _propertyHandler = _taskbar.RegisterPropertyChangedEvent(
                    TreeScope.Element,
                    (element, id, val) => RequestUpdate(),
                    _automation.PropertyLibrary.Element.BoundingRectangle);

                RequestUpdate();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Hook Init Failed: {ex.Message}");
            }
        }

        private void RequestUpdate()
        {
            if (_isDisposed) return;
            _debounceTimer?.Dispose();
            _debounceTimer = new Timer(_ =>
            {
                Rectangle voidRect = CalculateVoidRect();
                _dispatcherQueue.TryEnqueue(() =>
                {
                    if (!_isDisposed && voidRect != Rectangle.Empty)
                    {
                        _onLayoutChanged?.Invoke(new TaskbarFreeBoundsChangedEventArgs(voidRect.ToRect()));
                    }
                });
            }, null, DebounceDelay, Timeout.Infinite);
        }

        private Rectangle CalculateVoidRect()
        {
            try
            {
                if (_taskbar == null) return Rectangle.Empty;

                // 重新获取任务栏边界
                try { var _ = _taskbar.BoundingRectangle; }
                catch
                {
                    var desktop = _automation.GetDesktop();
                    _taskbar = desktop.FindFirstChild(cf => cf.ByClassName("Shell_TrayWnd"));
                    if (_taskbar == null) return Rectangle.Empty;
                }

                Rectangle taskbarRect = _taskbar.BoundingRectangle;

                // 确定右边界
                int gapRight = taskbarRect.Right;

                var tray = _taskbar.FindFirstDescendant(cf => cf.ByAutomationId("SystemTrayIcon")); // Win11
                if (tray == null) tray = _taskbar.FindFirstDescendant(cf => cf.ByClassName("TrayNotifyWnd")); // Win10

                if (tray != null)
                {
                    gapRight = tray.BoundingRectangle.Left;
                }

                int gapLeft = taskbarRect.Left;

                // 系统按钮
                string[] systemButtonIds = new[] {
                    "StartButton",
                    "SearchButton",
                    "TaskViewButton",
                    "WidgetsButton",
                    "ChatButton"
                };

                foreach (var id in systemButtonIds)
                {
                    var btn = _taskbar.FindFirstDescendant(cf => cf.ByAutomationId(id));
                    if (btn != null)
                    {
                        var rect = btn.BoundingRectangle;
                        // 只有当按钮在托盘左侧时，才计算它
                        if (rect.Right < gapRight && rect.Right > gapLeft)
                        {
                            gapLeft = (int)rect.Right;
                        }
                    }
                }

                // 遍历图标
                var appIcons = _taskbar.FindAllDescendants(cf => cf.ByClassName("Taskbar.TaskListButtonAutomationPeer"));

                foreach (var icon in appIcons)
                {
                    var rect = icon.BoundingRectangle;

                    // 过滤无效的图标 (宽高为 0 的隐藏图标)
                    if (rect.Width <= 0 || rect.Height <= 0) continue;

                    // 如果这个图标确实在托盘左边，更新 gapLeft
                    if (rect.Right < gapRight && rect.Right > gapLeft)
                    {
                        gapLeft = (int)rect.Right;
                    }
                }

                // 增加一点点间距
                gapLeft += 10;
                gapRight -= 10;

                int width = gapRight - gapLeft;

                if (width < 20) return Rectangle.Empty;

                return new Rectangle(gapLeft, taskbarRect.Top, width, taskbarRect.Height);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Calc Rect Error: {ex.Message}");
                return Rectangle.Empty;
            }
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            _debounceTimer?.Dispose();

            _structureHandler?.Dispose();
            _propertyHandler?.Dispose();

            _automation?.Dispose();
        }
    }
}