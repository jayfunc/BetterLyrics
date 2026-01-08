using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Events;
using BetterLyrics.WinUI3.Extensions;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.EventHandlers;
using FlaUI.UIA3;
using Microsoft.UI.Dispatching;
using System;
using System.Drawing;
using System.Threading;

namespace BetterLyrics.WinUI3.Hooks
{
    public partial class TaskbarHook : IDisposable
    {
        private readonly UIA3Automation _automation;
        private AutomationElement? _taskbar;

        private StructureChangedEventHandlerBase? _structureHandler;
        private PropertyChangedEventHandlerBase? _propertyHandler;

        private TaskbarPlacement _currentPlacement;

        private readonly DispatcherQueue _dispatcherQueue;
        private readonly Action<TaskbarFreeBoundsChangedEventArgs> _onLayoutChanged;
        private Timer? _debounceTimer;
        private const int DebounceDelay = 150;
        private bool _isDisposed;

        public TaskbarHook(TaskbarPlacement placement, Action<TaskbarFreeBoundsChangedEventArgs> onLayoutChanged)
        {
            _automation = new UIA3Automation();
            _onLayoutChanged = onLayoutChanged;
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            _currentPlacement = placement;

            StartHook();
        }

        public void UpdatePlacement(TaskbarPlacement newPlacement)
        {
            _currentPlacement = newPlacement;
            RequestUpdate(); // 立即刷新位置
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
                Rectangle voidRect = CalculateVoidRect(_currentPlacement);
                _dispatcherQueue.TryEnqueue(() =>
                {
                    if (!_isDisposed && voidRect != Rectangle.Empty)
                    {
                        _onLayoutChanged?.Invoke(new TaskbarFreeBoundsChangedEventArgs(voidRect.ToRect()));
                    }
                });
            }, null, DebounceDelay, Timeout.Infinite);
        }

        private Rectangle CalculateVoidRect(TaskbarPlacement placement)
        {
            try
            {
                if (_taskbar == null) return Rectangle.Empty;

                try { var _ = _taskbar.BoundingRectangle; }
                catch
                {
                    var desktop = _automation.GetDesktop();
                    _taskbar = desktop.FindFirstChild(cf => cf.ByClassName("Shell_TrayWnd"));
                    if (_taskbar == null) return Rectangle.Empty;
                }

                Rectangle taskbarRect = _taskbar.BoundingRectangle;

                // 绝对右边界：托盘
                int barrierRight = taskbarRect.Right;
                var tray = _taskbar.FindFirstDescendant(cf => cf.ByAutomationId("SystemTrayIcon")); // Win11
                if (tray == null) tray = _taskbar.FindFirstDescendant(cf => cf.ByClassName("TrayNotifyWnd")); // Win10
                if (tray != null) barrierRight = tray.BoundingRectangle.Left;

                // 绝对左边界：任务栏左边缘 或 小组件(Win11)
                int barrierLeft = taskbarRect.Left;
                var widgets = _taskbar.FindFirstDescendant(cf => cf.ByAutomationId("WidgetsButton"));

                // 只有当小组件确实在最左侧时 (Win11默认)，它才构成左边界
                // 如果用户把任务栏设为靠左对齐，小组件会在开始按钮右边，这时候不把它当做左边界
                if (widgets != null && widgets.BoundingRectangle.Left < taskbarRect.Left + 100)
                {
                    barrierLeft = (int)widgets.BoundingRectangle.Right;
                }


                // 寻找 中间内容区域 (Start + Search + Apps) 的 左右极值
                int contentMinLeft = barrierRight;
                int contentMaxRight = barrierLeft;

                // 定义所有中间元素
                string[] systemButtonIds = new[] {
                    "StartButton", "SearchButton", "TaskViewButton", "ChatButton"
                };

                // 系统按钮
                foreach (var id in systemButtonIds)
                {
                    var btn = _taskbar.FindFirstDescendant(cf => cf.ByAutomationId(id));
                    if (btn != null)
                    {
                        var rect = btn.BoundingRectangle;
                        // 排除不可见的
                        if (rect.Width <= 0) continue;

                        // 更新极值
                        if (rect.Left < contentMinLeft) contentMinLeft = (int)rect.Left;
                        if (rect.Right > contentMaxRight) contentMaxRight = (int)rect.Right;
                    }
                }

                // App 图标
                var appIcons = _taskbar.FindAllDescendants(cf => cf.ByClassName("Taskbar.TaskListButtonAutomationPeer"));
                foreach (var icon in appIcons)
                {
                    var rect = icon.BoundingRectangle;
                    if (rect.Width <= 0) continue;

                    if (rect.Left < contentMinLeft) contentMinLeft = (int)rect.Left;
                    if (rect.Right > contentMaxRight) contentMaxRight = (int)rect.Right;
                }

                // 如果完全没找到内容，重置为中间
                if (contentMinLeft == barrierRight) contentMinLeft = taskbarRect.Left;
                if (contentMaxRight == barrierLeft) contentMaxRight = taskbarRect.Left;

                int finalLeft, finalRight;
                int padding = 10;

                if (placement == TaskbarPlacement.Left)
                {
                    // 【小组件】... [空隙] ...【开始按钮】
                    // 如果是 Win10 或 Win11左对齐，contentMinLeft 几乎等于 barrierLeft，空隙为0
                    finalLeft = barrierLeft + padding;
                    finalRight = contentMinLeft - padding;
                }
                else // Right
                {
                    // 【最后一个图标】... [空隙] ...【托盘】
                    finalLeft = contentMaxRight + padding;
                    finalRight = barrierRight - padding;
                }

                int width = finalRight - finalLeft;

                if (width < 20) return Rectangle.Empty;

                var finalRect = new Rectangle(finalLeft, taskbarRect.Top, width, taskbarRect.Height);
                return finalRect;
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