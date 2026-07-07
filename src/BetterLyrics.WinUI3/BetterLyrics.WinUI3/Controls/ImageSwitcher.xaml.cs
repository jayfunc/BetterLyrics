using System.Numerics;
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
    public static readonly DependencyProperty CornerRadiusAmountProperty =
        DependencyProperty.Register(nameof(CornerRadiusAmount), typeof(int), typeof(ImageSwitcher),
            new PropertyMetadata(0));

    public static readonly DependencyProperty ShadowAmountProperty =
        DependencyProperty.Register(nameof(ShadowAmount), typeof(int), typeof(ImageSwitcher), new PropertyMetadata(0));

    public static readonly DependencyProperty SourceProperty =
        DependencyProperty.Register(nameof(Source), typeof(ImageSource), typeof(ImageSwitcher),
            new PropertyMetadata(null, OnDependencyPropertyChanged));

    public static readonly DependencyProperty StretchProperty =
        DependencyProperty.Register(nameof(Stretch), typeof(Stretch), typeof(ImageSwitcher),
            new PropertyMetadata(Stretch.Uniform));

    public static readonly DependencyProperty SwitchTypeProperty =
        DependencyProperty.Register(nameof(SwitchType), typeof(ImageSwitchType), typeof(ImageSwitcher),
            new PropertyMetadata(ImageSwitchType.Crossfade));

    public ImageSwitcher()
    {
        InitializeComponent();
    }

    public int CornerRadiusAmount
    {
        get => (int)GetValue(CornerRadiusAmountProperty);
        set => SetValue(CornerRadiusAmountProperty, value);
    }

    public int ShadowAmount
    {
        get => (int)GetValue(ShadowAmountProperty);
        set => SetValue(ShadowAmountProperty, value);
    }

    public ImageSource? Source
    {
        get => (ImageSource?)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public Stretch Stretch
    {
        get => (Stretch)GetValue(StretchProperty);
        set => SetValue(StretchProperty, value);
    }

    public ImageSwitchType SwitchType
    {
        get => (ImageSwitchType)GetValue(SwitchTypeProperty);
        set => SetValue(SwitchTypeProperty, value);
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
        }
    }

    private void UpdateSourceCrossfade()
    {
        // Ϊ����ͼƬ���þ�Դ
        LastAlbumArtImage.Source = AlbumArtImage.Source;
        // ʹ�������ɼ�
        LastAlbumArtImage.TranslationTransition = null;
        LastAlbumArtImage.OpacityTransition = null;
        LastAlbumArtImage.Translation = new Vector3();
        LastAlbumArtImage.Opacity = 1;
        LastAlbumArtImage.OpacityTransition = new ScalarTransition { Duration = Time.AnimationDuration };

        // ʹǰ��ͼƬ�������ɼ�
        AlbumArtImage.TranslationTransition = null;
        AlbumArtImage.OpacityTransition = null;
        AlbumArtImage.Translation = new Vector3();
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
        LastAlbumArtImage.Translation = new Vector3();
        LastAlbumArtImage.Opacity = 1;
        LastAlbumArtImage.TranslationTransition = new Vector3Transition { Duration = Time.AnimationDuration };
        LastAlbumArtImage.OpacityTransition = new ScalarTransition { Duration = Time.AnimationDuration };

        // ʹǰ��ͼƬ�������ɼ�
        AlbumArtImage.TranslationTransition = null;
        AlbumArtImage.OpacityTransition = null;
        AlbumArtImage.Translation = new Vector3(-(float)ActualWidth, 0, 0);
        AlbumArtImage.Opacity = 0;
        AlbumArtImage.TranslationTransition = new Vector3Transition { Duration = Time.AnimationDuration };
        AlbumArtImage.OpacityTransition = new ScalarTransition { Duration = Time.AnimationDuration };
        // ֮��Ϊ��������Դ
        AlbumArtImage.Source = Source;

        // ����
        LastAlbumArtImage.Opacity = 0;
        AlbumArtImage.Opacity = 1;
        LastAlbumArtImage.Translation = new Vector3(-(float)ActualWidth, 0, 0);
        AlbumArtImage.Translation = new Vector3();
    }

    private static void OnDependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ImageSwitcher imageSwitcher)
            if (e.Property == SourceProperty)
                imageSwitcher.UpdateSource();
    }
}