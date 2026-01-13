using BetterLyrics.WinUI3.Models.Settings;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Controls
{
    public sealed partial class LyricsStyleSettingsControl : UserControl
    {
        public LyricsStyleSettingsControl()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty LyricsStyleSettingsProperty =
            DependencyProperty.Register(nameof(LyricsStyleSettings), typeof(LyricsStyleSettings), typeof(LyricsStyleSettingsControl), new PropertyMetadata(default));

        public LyricsStyleSettings LyricsStyleSettings
        {
            get => (LyricsStyleSettings)GetValue(LyricsStyleSettingsProperty);
            set => SetValue(LyricsStyleSettingsProperty, value);
        }
    }
}
