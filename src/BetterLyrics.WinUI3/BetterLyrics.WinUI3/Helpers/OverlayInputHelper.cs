using System;
using System.Collections.Generic;
using Windows.Foundation;
using BetterLyrics.Core.Events;
using Microsoft.UI.Xaml;
using Vanara.PInvoke;
using WinRT.Interop;

namespace BetterLyrics.WinUI3.Helpers;

/// <summary>
///     用于管理覆盖层窗口的鼠标交互区域检测
/// </summary>
public class OverlayInputHelper
{
    private readonly IntPtr _hwnd;
    private readonly List<FrameworkElement> _interactiveControls = new();
    private readonly DispatcherTimer _timer;
    private readonly Window _window;

    private bool _wasOverControl;

    public Action<InteractiveAreaEventArgs> OnInteractiveAreaEntered;
    public Action OnInteractiveAreaExited;
    public Action<InteractiveAreaEventArgs> OnInteractiveAreaMoved;

    public OverlayInputHelper(Window window)
    {
        _window = window;
        _hwnd = WindowNative.GetWindowHandle(_window);

        _timer = new DispatcherTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(50);
        _timer.Tick += Timer_Tick;
    }

    public void Register(FrameworkElement element)
    {
        if (!_interactiveControls.Contains(element)) _interactiveControls.Add(element);
    }

    public void Unregister(FrameworkElement element)
    {
        if (_interactiveControls.Contains(element)) _interactiveControls.Remove(element);
    }

    public void Start()
    {
        _timer.Start();
        OnInteractiveAreaExited?.Invoke();
    }

    public void Stop()
    {
        _timer.Stop();
    }

    private void Timer_Tick(object? sender, object e)
    {
        User32.GetCursorPos(out var mousePoint);

        var isOverAnyControl = false;

        List<object> overlappedElements = new();

        foreach (var control in _interactiveControls)
        {
            if (control.XamlRoot == null || !control.XamlRoot.IsHostVisible) continue;

            var bounds = GetElementScreenBounds(control);

            if (mousePoint.X >= bounds.X &&
                mousePoint.X <= bounds.X + bounds.Width &&
                mousePoint.Y >= bounds.Y &&
                mousePoint.Y <= bounds.Y + bounds.Height)
            {
                isOverAnyControl = true;
                overlappedElements.Add(control);
            }
        }

        if (isOverAnyControl) OnInteractiveAreaMoved?.Invoke(new InteractiveAreaEventArgs(overlappedElements));

        if (isOverAnyControl != _wasOverControl)
        {
            if (isOverAnyControl)
                OnInteractiveAreaEntered?.Invoke(new InteractiveAreaEventArgs(overlappedElements));
            else
                OnInteractiveAreaExited?.Invoke();

            _wasOverControl = isOverAnyControl;
        }
    }

    private Rect GetElementScreenBounds(FrameworkElement element)
    {
        try
        {
            var transform = element.TransformToVisual(null);
            var topLeft = transform.TransformPoint(new Point(0, 0));
            var dpiScale = element.XamlRoot.RasterizationScale;

            var clientX = (int)(topLeft.X * dpiScale);
            var clientY = (int)(topLeft.Y * dpiScale);

            var point = new POINT { X = clientX, Y = clientY };
            User32.ClientToScreen(_hwnd, ref point);

            return new Rect(point.X, point.Y, element.ActualWidth * dpiScale, element.ActualHeight * dpiScale);
        }
        catch
        {
            return Rect.Empty;
        }
    }
}