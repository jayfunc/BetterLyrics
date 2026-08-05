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

    public static readonly DependencyProperty ThemeProperty =
        DependencyProperty.Register(nameof(Theme), typeof(InfoTagTheme), typeof(InfoTag),
            new PropertyMetadata(InfoTagTheme.Default, OnDependencyPropertyChanged));

    public InfoTag()
    {
        InitializeComponent();
        Loaded += (s, e) => 
        {
            UpdateThemeColors();
            UpdateDisabledVisuals();
        };
        IsEnabledChanged += (s, e) =>
        {
            UpdateDisabledVisuals();
        };
    }

    private void UpdateDisabledVisuals()
    {
        Opacity = IsEnabled ? 1.0 : 0.6;
        if (DisabledStrikethrough != null)
        {
            DisabledStrikethrough.Visibility = IsEnabled ? Visibility.Collapsed : Visibility.Visible;
        }
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

    public InfoTagTheme Theme
    {
        get => (InfoTagTheme)GetValue(ThemeProperty);
        set => SetValue(ThemeProperty, value);
    }

    public Visibility HasIcon => string.IsNullOrEmpty(Glyph) ? Visibility.Collapsed : Visibility.Visible;
    public Visibility HasText => string.IsNullOrEmpty(Text) ? Visibility.Collapsed : Visibility.Visible;

    private bool HasLink => !string.IsNullOrEmpty(Link);

    private Brush _hoverBackground;
    private Brush _normalBackground;

    private void UpdateThemeColors()
    {
        string backgroundKey = "CardBackgroundFillColorDefaultBrush";
        string borderKey = "CardStrokeColorDefaultBrush";
        string foregroundKey = "TextFillColorSecondaryBrush";
        string hoverBackgroundKey = "CardBackgroundFillColorSecondaryBrush";
        
        switch (Theme)
        {
            case InfoTagTheme.Default:
                break;
            case InfoTagTheme.Accent:
                backgroundKey = "AccentFillColorDefaultBrush";
                borderKey = "AccentFillColorDefaultBrush";
                foregroundKey = "TextOnAccentFillColorPrimaryBrush";
                hoverBackgroundKey = "AccentFillColorSecondaryBrush";
                break;
            case InfoTagTheme.Success:
                backgroundKey = "SystemFillColorSuccessBackgroundBrush";
                borderKey = "SystemFillColorSuccessBrush";
                foregroundKey = "SystemFillColorSuccessBrush";
                hoverBackgroundKey = "SystemFillColorSuccessBackgroundBrush";
                break;
            case InfoTagTheme.Warning:
                backgroundKey = "SystemFillColorCautionBackgroundBrush";
                borderKey = "SystemFillColorCautionBrush";
                foregroundKey = "SystemFillColorCautionBrush";
                hoverBackgroundKey = "SystemFillColorCautionBackgroundBrush";
                break;
            case InfoTagTheme.Error:
                backgroundKey = "SystemFillColorCriticalBackgroundBrush";
                borderKey = "SystemFillColorCriticalBrush";
                foregroundKey = "SystemFillColorCriticalBrush";
                hoverBackgroundKey = "SystemFillColorCriticalBackgroundBrush";
                break;
        }

        if (Application.Current.Resources.TryGetValue(backgroundKey, out var bg) && bg is Brush bgBrush)
            BadgeBorder.Background = bgBrush;
        if (Application.Current.Resources.TryGetValue(borderKey, out var border) && border is Brush borderBrush)
            BadgeBorder.BorderBrush = borderBrush;
        if (Application.Current.Resources.TryGetValue(foregroundKey, out var fg) && fg is Brush fgBrush)
        {
            TagText.Foreground = fgBrush;
            TagIcon.Foreground = fgBrush;
        }
        
        _hoverBackground = Application.Current.Resources.TryGetValue(hoverBackgroundKey, out var hbg) && hbg is Brush hbgBrush ? hbgBrush : BadgeBorder.Background;
        _normalBackground = BadgeBorder.Background;
    }

    private static void OnDependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is InfoTag tag)
        {
            if (tag.HasLink)
                tag.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);
            else
                tag.ProtectedCursor = null;

            if (e.Property == ThemeProperty)
            {
                tag.UpdateThemeColors();
            }

            tag.Bindings.Update();
        }
    }

    private void OnPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (HasLink && _hoverBackground != null) BadgeBorder.Background = _hoverBackground;
    }

    private void OnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (HasLink && _normalBackground != null) BadgeBorder.Background = _normalBackground;
    }

    private async void OnTapped(object sender, TappedRoutedEventArgs e)
    {
        if (HasLink && Uri.TryCreate(Link, UriKind.Absolute, out var uri)) await Launcher.LaunchUriAsync(uri);
    }
}

public enum InfoTagTheme
{
    Default,
    Accent,
    Success,
    Warning,
    Error
}