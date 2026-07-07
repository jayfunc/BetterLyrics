using System;
using System.Collections.Generic;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Windowing;
using Vanara.PInvoke;
using WinRT.Interop;

namespace BetterLyrics.WinUI3.Providers;

public class GlobalToastProvider : IGlobalToastProvider
{
    private static readonly ILocalizationService _localizationService =
        Ioc.Default.GetRequiredService<ILocalizationService>();

    private static readonly IAppUIThreadProvider _appUIThreadProvider =
        Ioc.Default.GetRequiredService<IAppUIThreadProvider>();

    private static readonly List<ToastOverlayWindow> _overlayWindows = [];
    private static bool _isInitialized;

    public void Initialize()
    {
        if (_isInitialized) return;

        var displayAreas = DisplayArea.FindAll();

        for (var i = 0; i < displayAreas.Count; i++)
        {
            var display = displayAreas[i];
            if (display == null) continue;

            var window = new ToastOverlayWindow();
            window.Init(display);
            _overlayWindows.Add(window);
        }

        _isInitialized = true;
    }

    public void Show(string localizedTitleKey, string? message = null,
        MessageSeverity severity = MessageSeverity.Informational, TimeSpan? duration = null)
    {
        if (!_isInitialized) Initialize();

        foreach (var window in _overlayWindows)
            User32.ShowWindow(WindowNative.GetWindowHandle(window), ShowWindowCommand.SW_SHOWNOACTIVATE);

        TimeSpan actualDuration;
        if (duration.HasValue)
        {
            actualDuration = duration.Value;
        }
        else
        {
            if (severity == MessageSeverity.Error)
                actualDuration = TimeSpan.FromSeconds(3);
            else
                actualDuration = TimeSpan.FromSeconds(3);
        }

        foreach (var window in _overlayWindows)
            _appUIThreadProvider.Execute(() =>
            {
                window.Stack.Show(_localizationService.GetLocalizedString(localizedTitleKey), message,
                    InfoBarSeverityExtensions.FromMessageSeverity(severity), actualDuration, false);
                window.StartOverlayInputHelper();
            });
    }
}