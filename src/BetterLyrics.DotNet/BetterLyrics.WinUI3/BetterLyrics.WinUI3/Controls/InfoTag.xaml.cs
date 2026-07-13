using System;
using Windows.System;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Controls;

public sealed partial class InfoTag : UserControl
{
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(string), typeof(InfoTag),
            new PropertyMetadata(string.Empty, OnDependencyPropertyChanged));

    public static readonly DependencyProperty GlyphProperty =
        DependencyProperty.Register(nameof(Glyph), typeof(string), typeof(InfoTag),
            new PropertyMetadata(string.Empty, OnDependencyPropertyChanged));

    public static readonly DependencyProperty LinkProperty =
        DependencyProperty.Register(nameof(Link), typeof(string), typeof(InfoTag),
            new PropertyMetadata(string.Empty, OnDependencyPropertyChanged));

    public InfoTag()
    {
        InitializeComponent();
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public string Glyph
    {
        get => (string)GetValue(GlyphProperty);
        set => SetValue(GlyphProperty, value);
    }

    public string Link
    {
        get => (string)GetValue(LinkProperty);
        set => SetValue(LinkProperty, value);
    }

    public Visibility HasIcon => string.IsNullOrEmpty(Glyph) ? Visibility.Collapsed : Visibility.Visible;
    public Visibility HasText => string.IsNullOrEmpty(Text) ? Visibility.Collapsed : Visibility.Visible;

    private bool HasLink => !string.IsNullOrEmpty(Link);

    private static void OnDependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is InfoTag tag)
        {
            if (tag.HasLink)
                tag.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);
            else
                tag.ProtectedCursor = null;

            tag.Bindings.Update();
        }
    }

    private void OnPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (HasLink) BadgeBorder.Background = (Brush)Resources["CardBackgroundFillColorSecondaryBrush"];
    }

    private void OnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (HasLink) BadgeBorder.Background = (Brush)Resources["CardBackgroundFillColorDefaultBrush"];
    }

    private async void OnTapped(object sender, TappedRoutedEventArgs e)
    {
        if (HasLink && Uri.TryCreate(Link, UriKind.Absolute, out var uri)) await Launcher.LaunchUriAsync(uri);
    }
}