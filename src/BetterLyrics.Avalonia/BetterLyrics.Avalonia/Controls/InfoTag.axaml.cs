using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace BetterLyrics.Avalonia.Controls;

public partial class InfoTag : UserControl
{
    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<InfoTag, string>(nameof(Text));

    public static readonly StyledProperty<string> GlyphProperty =
        AvaloniaProperty.Register<InfoTag, string>(nameof(Glyph));

    public static readonly StyledProperty<string> LinkProperty =
        AvaloniaProperty.Register<InfoTag, string>(nameof(Link));

    public InfoTag()
    {
        InitializeComponent();
    }

    public string Text { get => GetValue(TextProperty); set => SetValue(TextProperty, value); }
    public string Glyph { get => GetValue(GlyphProperty); set => SetValue(GlyphProperty, value); }
    public string Link { get => GetValue(LinkProperty); set => SetValue(LinkProperty, value); }

    // Avalonia 使用 bool 控制可见性
    public bool HasIcon => !string.IsNullOrEmpty(Glyph);
    public bool HasText => !string.IsNullOrEmpty(Text);
    public bool HasLink => !string.IsNullOrEmpty(Link);

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TextProperty || change.Property == GlyphProperty)
        {
            //this.RaisePropertyChanged(nameof(HasIcon));
            //this.RaisePropertyChanged(nameof(HasText));
        }
        else if (change.Property == LinkProperty)
        {
            //this.RaisePropertyChanged(nameof(HasLink));
        }
    }

    private async void OnTapped(object? sender, PointerPressedEventArgs e)
    {
        if (HasLink && Uri.TryCreate(Link, UriKind.Absolute, out var uri))
        {
            // 使用 Avalonia 的全局启动器
            await TopLevel.GetTopLevel(this)!.Launcher.LaunchUriAsync(uri);
        }
    }
}