using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.Core.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Windows.ApplicationModel.DataTransfer;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Controls;

public sealed partial class WindowSettingsControl : BaseUserControl
{
    private readonly IGlobalToastProvider _globalToastProvider = Ioc.Default.GetRequiredService<IGlobalToastProvider>();

    public WindowSettingsControlViewModel ViewModel => (WindowSettingsControlViewModel)DataContext;

    public static readonly DependencyProperty LyricsWindowStatusProperty =
        DependencyProperty.Register(nameof(LyricsWindowStatus), typeof(LyricsWindowStatus),
            typeof(WindowSettingsControl), new PropertyMetadata(default, OnLyricsWindowStatusChanged));

    public LyricsWindowStatus LyricsWindowStatus
    {
        get => (LyricsWindowStatus)GetValue(LyricsWindowStatusProperty);
        set => SetValue(LyricsWindowStatusProperty, value);
    }

    public WindowSettingsControl()
    {
        InitializeComponent();
        DataContext = Ioc.Default.GetRequiredService<WindowSettingsControlViewModel>();
    }

    protected override void BindGeneratedControls()
    {
    }

    protected override void UnbindGeneratedControls()
    {
    }

    protected override void OnLoaded()
    {
        ViewModel.Attach();
        _ = ViewModel.InitializeAsync();
    }

    protected override void OnUnloaded()
    {
        ViewModel?.Detach();
    }

    private static void OnLyricsWindowStatusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not WindowSettingsControl control) return;

        control.ViewModel.SetLyricsWindowStatus(control.LyricsWindowStatus);
    }

    private void RefreshMonitorButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.RefreshMonitorDeviceNames();
    }

    private async void RefreshWallpaperButton_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadWallpaperOptionsAsync(forceRefresh: true);
    }

    private void CopyWallpaperName_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: string name }) return;

        CopyToClipboard(name);
    }

    private void CopyCurrentWallpaperName_Click(object sender, RoutedEventArgs e) =>
        CopyToClipboard(ViewModel.CurrentWallpaperNames);

    private void CopyToClipboard(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        var dataPackage = new DataPackage
        {
            RequestedOperation = DataPackageOperation.Copy
        };
        dataPackage.SetText(text);
        Clipboard.SetContent(dataPackage);

        _globalToastProvider.Show("ActionCompleted", null, MessageSeverity.Success);
    }
}
