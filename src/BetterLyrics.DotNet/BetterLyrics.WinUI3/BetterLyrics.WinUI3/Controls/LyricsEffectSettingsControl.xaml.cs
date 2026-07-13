using BetterLyrics.Core.Models.Settings;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Controls;

public sealed partial class LyricsEffectSettingsControl : UserControl
{
    public static readonly DependencyProperty LyricsEffectSettingsProperty =
        DependencyProperty.Register(nameof(LyricsEffectSettings), typeof(LyricsEffectSettings),
            typeof(LyricsEffectSettingsControl), new PropertyMetadata(default));

    public LyricsEffectSettingsControl()
    {
        InitializeComponent();
    }

    public LyricsEffectSettings LyricsEffectSettings
    {
        get => (LyricsEffectSettings)GetValue(LyricsEffectSettingsProperty);
        set => SetValue(LyricsEffectSettingsProperty, value);
    }
}