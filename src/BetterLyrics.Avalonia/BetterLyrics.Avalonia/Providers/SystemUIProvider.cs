using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using BetterLyrics.Avalonia.Extensions;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Models.Domain;

namespace BetterLyrics.Avalonia.Providers;

public class SystemUIProvider : ISystemUIProvider
{
    public AppColor GetAccentColor(nint myHwnd, WindowPixelSampleMode mode)
    {
        throw new NotImplementedException();
    }

    public AppTheme GetAppTheme()
    {
        throw new NotImplementedException();
    }

    public void SetAppLanguage(string languageCode)
    {
    }
}