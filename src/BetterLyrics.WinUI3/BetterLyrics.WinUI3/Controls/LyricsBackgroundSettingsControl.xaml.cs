using BetterLyrics.Core.Models.Settings;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Controls;

public sealed partial class LyricsBackgroundSettingsControl : UserControl
{
    public static readonly DependencyProperty LyricsBackgroundSettingsProperty =
        DependencyProperty.Register(nameof(LyricsBackgroundSettings), typeof(LyricsBackgroundSettings),
            typeof(LyricsBackgroundSettingsControl), new PropertyMetadata(default));

    public LyricsBackgroundSettingsControl()
    {
        InitializeComponent();
    }

    public LyricsBackgroundSettings LyricsBackgroundSettings
    {
        get => (LyricsBackgroundSettings)GetValue(LyricsBackgroundSettingsProperty);
        set => SetValue(LyricsBackgroundSettingsProperty, value);
    }
}