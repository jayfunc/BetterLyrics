using BetterLyrics.WinUI3.Hooks;
using BetterLyrics.WinUI3.Services.LocalizationService;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using Vanara.PInvoke;
using WinRT.Interop;
using WinUIEx;

namespace BetterLyrics.WinUI3.Helper
{
    public static class GlobalToastManager
    {
        private static readonly ILocalizationService _localizationService = Ioc.Default.GetRequiredService<ILocalizationService>();

        private static List<ToastOverlayWindow> _overlayWindows = [];
        private static bool _isInitialized = false;

        public static void Initialize()
        {
            if (_isInitialized) return;

            var displayAreas = DisplayArea.FindAll();

            for (int i = 0; i < displayAreas.Count; i++)
            {
                var display = displayAreas[i];
                if (display == null) continue;

                var window = new ToastOverlayWindow();
                window.Init(display);
                _overlayWindows.Add(window);
            }

            _isInitialized = true;
        }

        public static void Show(string localizedTitleKey, string? message = null, InfoBarSeverity severity = InfoBarSeverity.Informational, TimeSpan? duration = null)
        {
            if (!_isInitialized)
            {
                Initialize();
            }

            foreach (var window in _overlayWindows)
            {
                User32.ShowWindow(WindowNative.GetWindowHandle(window), ShowWindowCommand.SW_SHOWNOACTIVATE);
            }

            TimeSpan actualDuration;
            if (duration.HasValue)
            {
                actualDuration = duration.Value;
            }
            else
            {
                if (severity == InfoBarSeverity.Error)
                {
                    actualDuration = TimeSpan.FromSeconds(3);
                }
                else
                {
                    actualDuration = TimeSpan.FromSeconds(3);
                }
            }

            foreach (var window in _overlayWindows)
            {
                window.DispatcherQueue.TryEnqueue(() =>
                {
                    window.Stack.Show(_localizationService.GetLocalizedString(localizedTitleKey), message, severity, actualDuration, false);
                    window.StartOverlayInputHelper();
                });
            }
        }
    }
}
