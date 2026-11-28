using BetterLyrics.Core.Models.Settings;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace BetterLyrics.WinUI3.Controls;

public sealed partial class ForegroundOverlaySettingsControl : UserControl
{
    public static readonly DependencyProperty LyricsBackgroundSettingsProperty =
        DependencyProperty.Register(nameof(LyricsBackgroundSettings), typeof(LyricsBackgroundSettings),
            typeof(ForegroundOverlaySettingsControl), new PropertyMetadata(default));

    public LyricsBackgroundSettings LyricsBackgroundSettings
    {
        get => (LyricsBackgroundSettings)GetValue(LyricsBackgroundSettingsProperty);
        set => SetValue(LyricsBackgroundSettingsProperty, value);
    }

    public ForegroundOverlaySettingsControl()
    {
        InitializeComponent();
    }
}