using BetterLyrics.WinUI3.Models.Settings;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Controls;

public sealed partial class AlbumArtAreaEffectSettingsControl : UserControl
{
    public static readonly DependencyProperty AlbumArtAreaEffectSettingsProperty =
    DependencyProperty.Register(nameof(AlbumArtAreaEffectSettings), typeof(AlbumArtAreaEffectSettings), typeof(AlbumArtAreaEffectSettingsControl), new PropertyMetadata(default));

    public AlbumArtAreaEffectSettings AlbumArtAreaEffectSettings
    {
        get => (AlbumArtAreaEffectSettings)GetValue(AlbumArtAreaEffectSettingsProperty);
        set => SetValue(AlbumArtAreaEffectSettingsProperty, value);
    }

    public AlbumArtAreaEffectSettingsControl()
    {
        InitializeComponent();
    }
}
