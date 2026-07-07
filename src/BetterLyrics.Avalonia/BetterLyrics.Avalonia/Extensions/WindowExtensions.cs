using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Styling;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Interfaces.Services;
using CommunityToolkit.Mvvm.DependencyInjection;
using System;

namespace BetterLyrics.Avalonia.Extensions;

public static class WindowExtensions
{
    public static void Init(
        this Window window,
        string titleKey = "",
        string title = "",
        // TitleBarHeightOption 在 Avalonia 中通常不需要手动指定，交由跨平台层自动处理即可。
        // 如果你的原代码强依赖了这个枚举，可以保留参数，但内部做空处理。
        int titleBarHeightOption = 0,
        BackdropType backdropType = BackdropType.DesktopAcrylic)
    {
        var localizationService = Ioc.Default.GetRequiredService<ILocalizationService>();

        if (!string.IsNullOrEmpty(titleKey))
            window.Title = localizationService.GetLocalizedString(titleKey);

        if (!string.IsNullOrEmpty(title))
            window.Title = title;

        window.Title += $" - {Core.Constants.App.AppName}";

        // 1. 设置跨平台背景材质 (Mica / Acrylic)
        // Avalonia 使用 TransparencyLevelHint 数组，按设备支持能力从前往后降级兼容
        window.TransparencyLevelHint = backdropType switch
        {
            BackdropType.None => [WindowTransparencyLevel.None],
            BackdropType.Mica => [WindowTransparencyLevel.Mica, WindowTransparencyLevel.AcrylicBlur, WindowTransparencyLevel.None],
            BackdropType.MicaAlt => [WindowTransparencyLevel.Mica, WindowTransparencyLevel.AcrylicBlur, WindowTransparencyLevel.None],
            BackdropType.DesktopAcrylic => [WindowTransparencyLevel.AcrylicBlur, WindowTransparencyLevel.None],
            BackdropType.Transparent => [WindowTransparencyLevel.Transparent],
            _ => [WindowTransparencyLevel.None]
        };

        // ⚠️ 关键：要让 Mica 或 Acrylic 生效，Window 的 Background 必须是透明的
        if (backdropType != BackdropType.None)
        {
            window.Background = Brushes.Transparent;
        }

        // 2. 沉浸式标题栏 (等同于 ExtendsContentIntoTitleBar = true)
        window.ExtendClientAreaToDecorationsHint = true;

        // 可选：设置系统标题栏按钮的行为（隐藏、显示、默认等）
        //window.ExtendClientAreaChromeHints = Avalonia.Platform.ExtendClientAreaChromeHints.Default;

        // 3. 窗口图标 (SetIcons)
        // 在 Avalonia 中，图标推荐直接在 XAML 中设置：<Window Icon="/Assets/icon.ico" />
        // 如果需要代码设置：
        window.Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://BetterLyrics.Avalonia/Assets/Logo.ico")));
    }

    public static void SyncTheme(this Window window)
    {
        var settingsService = Ioc.Default.GetRequiredService<ISettingsService>();
        if (settingsService == null || window == null) return;

        var appTheme = settingsService.AppSettings.GeneralSettings.AppTheme;

        // Avalonia 原生支持切换局部窗口或全局的主题变量 (ThemeVariant)
        // 不需要去单独设置 TitleBar.PreferredTheme，Avalonia 会自动接管
        window.RequestedThemeVariant = appTheme switch
        {
            // 此处请根据你 Core.Enums.AppTheme 实际的枚举项名称进行微调
            AppTheme.Light => ThemeVariant.Light,
            AppTheme.Dark => ThemeVariant.Dark,
            AppTheme.Default => ThemeVariant.Default,
            _ => ThemeVariant.Default
        };
    }
}