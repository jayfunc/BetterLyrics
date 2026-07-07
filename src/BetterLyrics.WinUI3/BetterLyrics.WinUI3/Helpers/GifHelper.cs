using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

namespace BetterLyrics.WinUI3.Helpers;

public static class GifHelper
{
    public static readonly DependencyProperty IsPlayingProperty =
        DependencyProperty.RegisterAttached(
            "IsPlaying",
            typeof(bool),
            typeof(GifHelper),
            new PropertyMetadata(true, OnIsPlayingChanged));

    public static bool GetIsPlaying(DependencyObject obj)
    {
        return (bool)obj.GetValue(IsPlayingProperty);
    }

    public static void SetIsPlaying(DependencyObject obj, bool value)
    {
        obj.SetValue(IsPlayingProperty, value);
    }

    private static void OnIsPlayingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Image image)
        {
            var isPlaying = (bool)e.NewValue;

            UpdatePlayback(image, isPlaying);

            image.ImageOpened -= Image_ImageOpened;
            image.ImageOpened += Image_ImageOpened;
        }
    }

    private static void Image_ImageOpened(object sender, RoutedEventArgs e)
    {
        if (sender is Image image) UpdatePlayback(image, GetIsPlaying(image));
    }

    private static void UpdatePlayback(Image image, bool isPlaying)
    {
        if (image.Source is BitmapImage bitmap && bitmap.IsAnimatedBitmap)
        {
            if (isPlaying)
                bitmap.Play();
            else
                bitmap.Stop();
        }
    }
}