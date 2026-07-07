using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using BetterLyrics.Avalonia.Extensions;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Models.Settings;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace BetterLyrics.Avalonia.Views;

public partial class SettingsWindow : Window, IRecipient<PropertyChangedMessage<AppTheme>>
{
    private readonly IWindowManagerProvider _windowManagerProvider =
        Ioc.Default.GetRequiredService<IWindowManagerProvider>();

    public SettingsWindow()
    {
        InitializeComponent();

        WeakReferenceMessenger.Default.RegisterAll(this);

        this.Init("SettingsPageTitle");
        this.SyncTheme();

        // Avalonia 的 Window 原生支持 Closing 事件，直接订阅即可
        Closing += SettingsWindow_Closing;
    }

    public void Receive(PropertyChangedMessage<AppTheme> message)
    {
        if (message.Sender is GeneralSettings && message.PropertyName == nameof(GeneralSettings.AppTheme))
        {
            this.SyncTheme();
        }
    }

    private void SettingsWindow_Closing(object? sender, WindowClosingEventArgs e)
    {
        // 传递给 Manager 处理
        _windowManagerProvider.CloseWindow(this);
    }

    private void MusicGalleryButton_Click(object? sender, RoutedEventArgs e)
    {
        _windowManagerProvider.OpenOrShowWindow(WindowType.MusicGalleryWindow);
    }

    private void LyricsWindowSwitchButton_Click(object? sender, RoutedEventArgs e)
    {
        _windowManagerProvider.OpenOrShowWindow(WindowType.LyricsWindowSwitchWindow);
    }

    private void CustomTitleBar_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // 防止拖拽事件干扰到标题栏里的其他按钮点击
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            // 核心代码：通知 Avalonia 开始接管窗口拖拽
            this.BeginMoveDrag(e);
        }
    }
}