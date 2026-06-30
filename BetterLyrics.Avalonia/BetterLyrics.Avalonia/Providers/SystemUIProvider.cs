using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using BetterLyrics.Avalonia.Extensions;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Models.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BetterLyrics.Avalonia.Providers
{
    public class SystemUIProvider : ISystemUIProvider
    {
        public AppColor GetAccentColor(nint myHwnd, WindowPixelSampleMode mode)
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
                {
                    return (primaryScreen.DisplayName ?? string.Empty, primaryScreen.Bounds.ToAppRect());
                }
            }

            return (string.Empty, AppRect.Empty);
        }

        public void ShowToast(string localizedTitleKey, string? message = null, MessageSeverity severity = MessageSeverity.Informational, TimeSpan? duration = null)
        {
            throw new NotImplementedException();
        }
    }
}
