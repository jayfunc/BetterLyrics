using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Models.Domain;

namespace BetterLyrics.Core.Interfaces.Providers;

public interface ISystemUIProvider
{
    AppColor GetAccentColor(IntPtr myHwnd, WindowPixelSampleMode mode);

    AppTheme GetAppTheme();

    void SetAppLanguage(string languageCode);
}