using System;
using System.Collections.Generic;
using BetterLyrics.Core.Models.Domain;

namespace BetterLyrics.Core.Interfaces.Providers
{
    public interface IMonitorProvider
    {
        bool IsDisplayOn { get; }
        event EventHandler<bool>? DisplayStatusChanged;
        event EventHandler? MonitorsChanged;
        IEnumerable<string> GetAllMonitorDeviceNames();
        AppRect GetMonitorRectFromDeviceName(string deviceName);
        (string, AppRect) GetPrimaryMonitorInfo();
        string GetPrimaryMonitorDeviceName();
        (string, AppRect) GetMonitorInfoFromWindow(object? window);
    }
}
