using System;
using System.Threading.Tasks;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.WinUI3.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace BetterLyrics.WinUI3.Providers;

public class MediaSourceDialogProvider : IAddMediaSourceDialogProvider
{
    private readonly IWindowManagerProvider _windowManagerProvider;
    private readonly ILocalizationService _localizationService;

    public MediaSourceDialogProvider(
        IWindowManagerProvider windowManagerProvider,
        ILocalizationService localizationService)
    {
        _windowManagerProvider = windowManagerProvider;
        _localizationService = localizationService;
    }

    public async Task ShowDialogAsync(FileSourceType fileSourceType, Func<MediaFolder, Task<(bool isValid, string? errorMessage)>> validationCallback)
    {
        var dialog = new ContentDialog
        {
            XamlRoot = (_windowManagerProvider.GetWindow(WindowType.SettingsWindow) as Window)?.Content.XamlRoot,
            Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
            Title = fileSourceType == FileSourceType.Local
                ? _localizationService.GetLocalizedString("MediaSettingsControlLocalFolder")
                : Enum.GetName(fileSourceType),
            PrimaryButtonText = _localizationService.GetLocalizedString("Add"),
            CloseButtonText = _localizationService.GetLocalizedString("Cancel"),
            DefaultButton = ContentDialogButton.Primary,
            Content = new RemoteServerConfigControl(fileSourceType)
        };

        dialog.PrimaryButtonClick += async (s, e) =>
        {
            var configControl = (RemoteServerConfigControl)dialog.Content;
            var deferral = e.GetDeferral();
            e.Cancel = true; // 默认阻止关闭，直到验证通过

            dialog.IsPrimaryButtonEnabled = false;
            configControl.IsEnabled = false;
            configControl.SetProgressBarVisibility(Visibility.Visible);
            // 清除之前的错误信息
            configControl.ShowError(null);

            try
            {
                var tempFolder = configControl.GetConfig();
                var (isValid, errorMessage) = await validationCallback(tempFolder);

                if (isValid)
                {
                    e.Cancel = false; // 验证通过，允许关闭
                }
                else
                {
                    configControl.ShowError(errorMessage);
                }
            }
            catch (Exception ex)
            {
                configControl.ShowError(ex.Message);
            }
            finally
            {
                if (e.Cancel)
                {
                    dialog.IsPrimaryButtonEnabled = true;
                    configControl.IsEnabled = true;
                    configControl.SetProgressBarVisibility(Visibility.Collapsed);
                }
            }

            deferral.Complete();
        };

        await dialog.ShowAsync();
    }
}
