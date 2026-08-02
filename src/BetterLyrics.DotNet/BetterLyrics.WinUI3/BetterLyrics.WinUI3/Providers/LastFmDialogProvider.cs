using System;
using System.Linq;
using System.Threading.Tasks;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.WinUI3.Views;
using Microsoft.UI.Xaml.Controls;

namespace BetterLyrics.WinUI3.Providers;

public class LastFmDialogProvider : ILastFmDialogProvider
{
    private readonly IWindowManagerProvider _windowManagerProvider;
    private readonly ILocalizationService _localizationService;

    public LastFmDialogProvider(
        IWindowManagerProvider windowManagerProvider,
        ILocalizationService localizationService)
    {
        _windowManagerProvider = windowManagerProvider;
        _localizationService = localizationService;
    }

    public async Task ShowAuthDialogAsync()
    {
        var dialogXamlRoot = _windowManagerProvider.GetWindow<SettingsWindow>()?.Content.XamlRoot ??
                             _windowManagerProvider.GetWindows<NowPlayingWindow>().FirstOrDefault()?.Content.XamlRoot;
        if (dialogXamlRoot != null)
        {
            var dialog = new ContentDialog
            {
                Title = _localizationService.GetLocalizedString("LastFMRequestAuthTitle") ?? "",
                Content = _localizationService.GetLocalizedString("LastFMRequestAuthDesc") ?? "",
                PrimaryButtonText = _localizationService.GetLocalizedString("LastFMRequestAuthConfirm") ?? "",
                CloseButtonText = _localizationService.GetLocalizedString("Cancel") ?? "",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = dialogXamlRoot
            };
            await dialog.ShowAsync();
        }
    }

    public async Task ShowUnAuthDialogAsync(Func<Task> onConfirm)
    {
        var dialogXamlRoot = _windowManagerProvider.GetWindow<SettingsWindow>()?.Content.XamlRoot ??
                             _windowManagerProvider.GetWindows<NowPlayingWindow>().FirstOrDefault()?.Content.XamlRoot;
        if (dialogXamlRoot == null) return;

        var dialog = new ContentDialog
        {
            Title = _localizationService.GetLocalizedString("LastFMRequestUnAuthTitle") ?? "",
            Content = _localizationService.GetLocalizedString("LastFMRequestUnAuthDesc") ?? "",
            PrimaryButtonText = _localizationService.GetLocalizedString("LastFMRequestUnAuthConfirm") ?? "",
            CloseButtonText = _localizationService.GetLocalizedString("Cancel") ?? "",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = dialogXamlRoot
        };

        dialog.PrimaryButtonClick += async (s, args) => { await onConfirm(); };
        await dialog.ShowAsync();
    }
}
