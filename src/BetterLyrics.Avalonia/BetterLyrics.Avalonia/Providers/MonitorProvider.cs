using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using BetterLyrics.Avalonia.Extensions;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Models.Domain;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BetterLyrics.Avalonia.Providers
{
    public class MonitorProvider : IMonitorProvider
    {
        public IEnumerable<string> GetAllMonitorDeviceNames()
        {
            throw new NotImplementedException();
        }

        public (string, AppRect) GetMonitorInfoFromWindow(object? window)
        {
            throw new NotImplementedException();
        }

        public AppRect GetMonitorRectFromDeviceName(string deviceName)
        {
            throw new NotImplementedException();
        }

        public string GetPrimaryMonitorDeviceName()
        {
            throw new NotImplementedException();
        }

        public (string, AppRect) GetPrimaryMonitorInfo()
        {
            var lifetime = Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
            var mainWindow = lifetime?.MainWindow;

            if (mainWindow?.Screens != null)
            {
                var primaryScreen = mainWindow.Screens.All.FirstOrDefault(s => s.IsPrimary);
                if (primaryScreen != null)
                    return (primaryScreen.DisplayName ?? string.Empty, primaryScreen.Bounds.ToAppRect());
            }

            return (string.Empty, AppRect.Empty);
        }
    }
}
