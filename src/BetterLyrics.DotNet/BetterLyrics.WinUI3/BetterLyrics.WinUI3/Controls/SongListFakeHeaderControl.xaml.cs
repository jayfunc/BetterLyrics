using BetterLyrics.Core.ViewModels;
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

    public SongListFakeHeaderControl()
    {
        this.InitializeComponent();
    }
}
