using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Events;
using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Helper;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.EventHandlers;
using FlaUI.UIA3;
using Microsoft.UI.Dispatching;
using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Hooks
{
    public partial class TaskbarHook : IDisposable
    {
        private readonly UIA3Automation _automation;
        private AutomationElement? _taskbar;

        private StructureChangedEventHandlerBase? _structureHandler;
        private PropertyChangedEventHandlerBase? _propertyHandler;

        private TaskbarPlacement _currentPlacement;

        private readonly DispatcherQueue? _dispatcherQueue;
        private readonly Action<TaskbarFreeBoundsChangedEventArgs> _onLayoutChanged;
        private Timer? _debounceTimer;
        private readonly LatestOnlyTaskRunner _updateTaskRunner;
        private const int DebounceDelay = 1000;
        private bool _isDisposed;

        public TaskbarHook(TaskbarPlacement placement, Action<TaskbarFreeBoundsChangedEventArgs> onLayoutChanged)
        {
            _automation = new UIA3Automation();
            _onLayoutChanged = onLayoutChanged;
            _dispatcherQueue = DispatcherQueueHelper.Instance;
            _updateTaskRunner = new();

            _currentPlacement = placement;

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

        private void StartHook()
        {
            try
            {
                var desktop = _automation.GetDesktop();
                _taskbar = desktop.FindFirstChild(cf => cf.ByClassName("Shell_TrayWnd"));

                if (_taskbar == null) return;

                // 监听图标增删
                _structureHandler = _taskbar.RegisterStructureChangedEvent(
                    TreeScope.Descendants,
                    (element, type, id) => RequestUpdate());

                // 捕获内部按钮的位移
                _propertyHandler = _taskbar.RegisterPropertyChangedEvent(
                    TreeScope.Descendants,
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
            _ = _updateTaskRunner.RunAsync(async (token) =>
            {
                await Task.Delay(1000, token);
                Rectangle voidRect = CalculateVoidRect(_currentPlacement);
                _dispatcherQueue?.TryEnqueue(() =>
                {
                    if (!_isDisposed && voidRect != Rectangle.Empty)
                    {
                        _onLayoutChanged?.Invoke(new TaskbarFreeBoundsChangedEventArgs(voidRect.ToRect()));
                    }
                });
            });
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

                int barrierRight = taskbarRect.Right;
                var tray = _taskbar.FindFirstDescendant(cf => cf.ByAutomationId("SystemTrayIcon")); // Win11
                if (tray == null) tray = _taskbar.FindFirstDescendant(cf => cf.ByClassName("TrayNotifyWnd")); // Win10
                if (tray != null) barrierRight = (int)tray.BoundingRectangle.Left;

                int barrierLeft = taskbarRect.Left;
                var widgets = _taskbar.FindFirstDescendant(cf => cf.ByAutomationId("WidgetsButton"));

                if (widgets != null && widgets.BoundingRectangle.Left < taskbarRect.Left + 200)
                {
                    barrierLeft = (int)widgets.BoundingRectangle.Right;
                }


                int contentMinLeft = barrierRight;
                int contentMaxRight = barrierLeft;

                string[] systemButtonIds = new[] {
                    "StartButton", "SearchButton", "TaskViewButton", "ChatButton", "CortanaButton"
                };

                foreach (var id in systemButtonIds)
                {
                    var btn = _taskbar.FindFirstDescendant(cf => cf.ByAutomationId(id));
                    if (btn != null && !btn.IsOffscreen)
                    {
                        var rect = btn.BoundingRectangle;
                        if (rect.Width <= 0 || rect.Height <= 0) continue;

                        if (rect.Left < contentMinLeft) contentMinLeft = (int)rect.Left;
                        if (rect.Right > contentMaxRight) contentMaxRight = (int)rect.Right;
                    }
                }

                var appIcons = _taskbar.FindAllDescendants(cf => cf.ByClassName("Taskbar.TaskListButtonAutomationPeer"));
                foreach (var icon in appIcons)
                {
                    var rect = icon.BoundingRectangle;
                    if (rect.Width <= 0) continue;

                    if (rect.Left < contentMinLeft) contentMinLeft = (int)rect.Left;
                    if (rect.Right > contentMaxRight) contentMaxRight = (int)rect.Right;
                }

                if (contentMinLeft >= barrierRight) contentMinLeft = taskbarRect.Left + taskbarRect.Width / 2;
                if (contentMaxRight <= barrierLeft) contentMaxRight = taskbarRect.Left + taskbarRect.Width / 2;


                int padding = 12;

                int leftZoneWidth = (contentMinLeft - padding) - (barrierLeft + padding);
                Rectangle leftZone = Rectangle.Empty;
                if (leftZoneWidth > 20)
                {
                    leftZone = new Rectangle(barrierLeft + padding, taskbarRect.Top, leftZoneWidth, taskbarRect.Height);
                }

                int rightZoneWidth = (barrierRight - padding) - (contentMaxRight + padding);
                Rectangle rightZone = Rectangle.Empty;
                if (rightZoneWidth > 20)
                {
                    rightZone = new Rectangle(contentMaxRight + padding, taskbarRect.Top, rightZoneWidth, taskbarRect.Height);
                }


                switch (placement)
                {
                    case TaskbarPlacement.Left:
                        return leftZone;

                    case TaskbarPlacement.Right:
                        return rightZone;

                    case TaskbarPlacement.Auto:
                    case TaskbarPlacement.Center:
                        if (leftZone.Width > rightZone.Width)
                            return leftZone;
                        else
                            return rightZone;

                    default:
                        return rightZone;
                }
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