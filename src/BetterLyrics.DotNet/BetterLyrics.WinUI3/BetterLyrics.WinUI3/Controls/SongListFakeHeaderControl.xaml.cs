using BetterLyrics.Core.ViewModels.MusicGalleryPageViewModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace BetterLyrics.WinUI3.Controls;

public sealed partial class SongListFakeHeaderControl : UserControl
{
    public MusicGalleryPageViewModel ViewModel
    {
        get => (MusicGalleryPageViewModel)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(nameof(ViewModel), typeof(MusicGalleryPageViewModel), typeof(SongListFakeHeaderControl), new PropertyMetadata(null));

    public bool ShowPlaylistGripSpacer
    {
        get => (bool)GetValue(ShowPlaylistGripSpacerProperty);
        set => SetValue(ShowPlaylistGripSpacerProperty, value);
    }

    public static readonly DependencyProperty ShowPlaylistGripSpacerProperty =
        DependencyProperty.Register(nameof(ShowPlaylistGripSpacer), typeof(bool), typeof(SongListFakeHeaderControl), new PropertyMetadata(false));

    public SongListFakeHeaderControl()
    {
        this.InitializeComponent();
    }
}
