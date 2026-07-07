using Avalonia;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Interfaces.Providers;
using CommunityToolkit.Mvvm.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace BetterLyrics.Avalonia.Controls;

public partial class PropertyRow : UserControl
{
    // Avalonia 中将 DependencyProperty 替换为 StyledProperty
    public static readonly StyledProperty<string> HeaderProperty =
        AvaloniaProperty.Register<PropertyRow, string>(nameof(Header), string.Empty);

    public static readonly StyledProperty<string> ValueProperty =
        AvaloniaProperty.Register<PropertyRow, string>(nameof(Value), string.Empty);

    public static readonly StyledProperty<string> LinkProperty =
        AvaloniaProperty.Register<PropertyRow, string>(nameof(Link), string.Empty);

    public static readonly StyledProperty<string> UnitProperty =
        AvaloniaProperty.Register<PropertyRow, string>(nameof(Unit), string.Empty);

    private readonly IGlobalToastProvider? _globalToastProvider;
    private readonly ILauncherProvider? _launcherProvider;

    public PropertyRow()
    {
        InitializeComponent();

        // 避免在设计器(Previewer)中因未初始化依赖注入而崩溃
        if (!Design.IsDesignMode)
        {
            _globalToastProvider = Ioc.Default.GetRequiredService<IGlobalToastProvider>();
            _launcherProvider = Ioc.Default.GetRequiredService<ILauncherProvider>();
        }
    }

    public string Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public string Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public string Link
    {
        get => GetValue(LinkProperty);
        set => SetValue(LinkProperty, value);
    }

    public string Unit
    {
        get => GetValue(UnitProperty);
        set => SetValue(UnitProperty, value);
    }

    private async void OnCopyClicked(object? sender, RoutedEventArgs e)
    {
        var targetValue = string.IsNullOrEmpty(Link) ? Value : Link;
        if (string.IsNullOrEmpty(targetValue)) return;

        try
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel?.Clipboard != null)
            {
                await topLevel.Clipboard.SetTextAsync(targetValue);
            }
        }
        catch (Exception ex)
        {
            _globalToastProvider?.Show("Error", ex.Message, MessageSeverity.Error);
            return;
        }

        CheckIcon.IsVisible = true;
        CopyIcon.IsVisible = false;

        // 在 async void 的事件处理程序中，直接使用 Task.Delay 会自动切回 UI 线程继续执行，
        // 从而彻底抛弃了 WinUI3 中的 DispatcherQueue.TryEnqueue() 臃肿写法
        await Task.Delay(1000);

        CheckIcon.IsVisible = false;
        CopyIcon.IsVisible = true;
    }

    private async void OnLinkClicked(object? sender, RoutedEventArgs e)
    {
        if (Uri.TryCreate(Link, UriKind.Absolute, out var uri))
        {
            if (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                await _launcherProvider?.LaunchUriAsync(uri)!;
            else if (uri.Scheme == Uri.UriSchemeFile)
                await _launcherProvider?.SelectAndShowFileAsync(uri.LocalPath)!;
        }
    }
}