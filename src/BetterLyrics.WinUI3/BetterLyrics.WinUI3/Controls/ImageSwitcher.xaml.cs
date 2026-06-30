using BetterLyrics.Core.Constants;
using BetterLyrics.Core.Enums;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Controls;

public sealed partial class ImageSwitcher : UserControl
{
    public int CornerRadiusAmount
    {
        get { return (int)GetValue(CornerRadiusAmountProperty); }
        set { SetValue(CornerRadiusAmountProperty, value); }
    }

    public static readonly DependencyProperty CornerRadiusAmountProperty =
        DependencyProperty.Register(nameof(CornerRadiusAmount), typeof(int), typeof(ImageSwitcher), new PropertyMetadata(0));

    public int ShadowAmount
    {
        get { return (int)GetValue(ShadowAmountProperty); }
        set { SetValue(ShadowAmountProperty, value); }
    }

    public static readonly DependencyProperty ShadowAmountProperty =
        DependencyProperty.Register(nameof(ShadowAmount), typeof(int), typeof(ImageSwitcher), new PropertyMetadata(0));

    public ImageSource? Source
    {
        get { return (ImageSource?)GetValue(SourceProperty); }
        set { SetValue(SourceProperty, value); }
    }

    public static readonly DependencyProperty SourceProperty =
        DependencyProperty.Register(nameof(Source), typeof(ImageSource), typeof(ImageSwitcher), new PropertyMetadata(null, OnDependencyPropertyChanged));

    public Stretch Stretch
    {
        get { return (Stretch)GetValue(StretchProperty); }
        set { SetValue(StretchProperty, value); }
    }

    public static readonly DependencyProperty StretchProperty =
        DependencyProperty.Register(nameof(Stretch), typeof(Stretch), typeof(ImageSwitcher), new PropertyMetadata(Stretch.Uniform));

    public ImageSwitchType SwitchType
    {
        get { return (ImageSwitchType)GetValue(SwitchTypeProperty); }
        set { SetValue(SwitchTypeProperty, value); }
    }

    public static readonly DependencyProperty SwitchTypeProperty =
        DependencyProperty.Register(nameof(SwitchType), typeof(ImageSwitchType), typeof(ImageSwitcher), new PropertyMetadata(ImageSwitchType.Crossfade));

    public ImageSwitcher()
    {
        InitializeComponent();
    }

    private void UpdateSource()
    {
        switch (SwitchType)
        {
            case ImageSwitchType.Crossfade:
                UpdateSourceCrossfade();
                break;
            case ImageSwitchType.Slide:
                UpdateSourceSlide();
                break;
            default:
                break;
        }
    }

    private void UpdateSourceCrossfade()
    {
        // Ϊ����ͼƬ���þ�Դ
        LastAlbumArtImage.Source = AlbumArtImage.Source;
        // ʹ�������ɼ�
        LastAlbumArtImage.TranslationTransition = null;
        LastAlbumArtImage.OpacityTransition = null;
        LastAlbumArtImage.Translation = new();
        LastAlbumArtImage.Opacity = 1;
        LastAlbumArtImage.OpacityTransition = new ScalarTransition { Duration = Time.AnimationDuration };

        // ʹǰ��ͼƬ�������ɼ�
        AlbumArtImage.TranslationTransition = null;
        AlbumArtImage.OpacityTransition = null;
        AlbumArtImage.Translation = new();
        AlbumArtImage.Opacity = 0;
        AlbumArtImage.OpacityTransition = new ScalarTransition { Duration = Time.AnimationDuration };
        // ֮��Ϊ��������Դ
        AlbumArtImage.Source = Source;

        // ���浭������
        LastAlbumArtImage.Opacity = 0;
        AlbumArtImage.Opacity = 1;
    }

    private void UpdateSourceSlide()
    {
        // Ϊ����ͼƬ���þ�Դ
        LastAlbumArtImage.Source = AlbumArtImage.Source;
        // ʹ���λ
        LastAlbumArtImage.TranslationTransition = null;
        LastAlbumArtImage.OpacityTransition = null;
        LastAlbumArtImage.Translation = new();
        LastAlbumArtImage.Opacity = 1;
        LastAlbumArtImage.TranslationTransition = new Vector3Transition { Duration = Time.AnimationDuration };
        LastAlbumArtImage.OpacityTransition = new ScalarTransition { Duration = Time.AnimationDuration };

        // ʹǰ��ͼƬ�������ɼ�
        AlbumArtImage.TranslationTransition = null;
        AlbumArtImage.OpacityTransition = null;
        AlbumArtImage.Translation = new(-(float)ActualWidth, 0, 0);
        AlbumArtImage.Opacity = 0;
        AlbumArtImage.TranslationTransition = new Vector3Transition { Duration = Time.AnimationDuration };
        AlbumArtImage.OpacityTransition = new ScalarTransition { Duration = Time.AnimationDuration };
        // ֮��Ϊ��������Դ
        AlbumArtImage.Source = Source;

        // ����
        LastAlbumArtImage.Opacity = 0;
        AlbumArtImage.Opacity = 1;
        LastAlbumArtImage.Translation = new(-(float)ActualWidth, 0, 0);
        AlbumArtImage.Translation = new();
    }

    private static void OnDependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ImageSwitcher imageSwitcher)
        {
            if (e.Property == SourceProperty)
            {
                imageSwitcher.UpdateSource();
            }
        }
    }
}
