using BetterLyrics.Core.Models.Domain;

namespace BetterLyrics.Core.Interfaces.Providers
{
    public interface IMonitorProvider
    {
        IEnumerable<string> GetAllMonitorDeviceNames();
        AppRect GetMonitorRectFromDeviceName(string deviceName);
        (string, AppRect) GetPrimaryMonitorInfo();
        string GetPrimaryMonitorDeviceName();
        (string, AppRect) GetMonitorInfoFromWindow(object? window);
    }
}
