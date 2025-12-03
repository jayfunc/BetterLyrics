using BetterLyrics.WinUI3.Models.Settings;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Controls
{
    public sealed partial class AlbumArtAreaStyleSettingsControl : UserControl
    {
        public static readonly DependencyProperty AlbumArtLayoutSettingsProperty =
            DependencyProperty.Register(nameof(AlbumArtLayoutSettings), typeof(AlbumArtAreaStyleSettings), typeof(AlbumArtAreaStyleSettingsControl), new PropertyMetadata(default));

        public AlbumArtAreaStyleSettings AlbumArtLayoutSettings
        {
            get => (AlbumArtAreaStyleSettings)GetValue(AlbumArtLayoutSettingsProperty);
            set => SetValue(AlbumArtLayoutSettingsProperty, value);
        }

        public AlbumArtAreaStyleSettingsControl()
        {
            InitializeComponent();
        }
    }
}
