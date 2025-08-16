using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.Services.SettingsService;
using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Services.LiveStatesService
{
    public class LiveStatesService : BaseViewModel, ILiveStatesService,
        IRecipient<PropertyChangedMessage<LyricsWindowMode>>,
        IRecipient<PropertyChangedMessage<LyricsDisplayType>>
    {
        private readonly ISettingsService _settingsService;

        public LiveStates LiveStates { get; set; }

        public LiveStatesService(ISettingsService settingsService)
        {
            _settingsService = settingsService;

            LiveStates = new LiveStates(_settingsService.AppSettings);
        }

        public void Receive(PropertyChangedMessage<LyricsWindowMode> message)
        {
            if (message.Sender is LiveStates)
            {
                if (message.PropertyName == nameof(LiveStates.CurrentLyricsWindowMode))
                {
                    switch (message.NewValue)
                    {
                        case LyricsWindowMode.StandardMode:
                            LiveStates.CurrentLyricsStyleSettings = _settingsService.AppSettings.StandardLyricsStyleSettings;
                            LiveStates.CurrentLyricsEffectSettings = _settingsService.AppSettings.StandardLyricsEffectSettings;
                            LiveStates.CurrentLyricsDisplayType = _settingsService.AppSettings.StandardModeSettings.LyricsDisplayType;
                            break;
                        case LyricsWindowMode.DockMode:
                            LiveStates.CurrentLyricsStyleSettings = _settingsService.AppSettings.DockLyricsStyleSettings;
                            LiveStates.CurrentLyricsEffectSettings = _settingsService.AppSettings.DockLyricsEffectSettings;
                            LiveStates.CurrentLyricsDisplayType = _settingsService.AppSettings.DockModeSettings.LyricsDisplayType;
                            break;
                        case LyricsWindowMode.DesktopMode:
                            LiveStates.CurrentLyricsStyleSettings = _settingsService.AppSettings.DesktopLyricsStyleSettings;
                            LiveStates.CurrentLyricsEffectSettings = _settingsService.AppSettings.DesktopLyricsEffectSettings;
                            LiveStates.CurrentLyricsDisplayType = _settingsService.AppSettings.DesktopModeSettings.LyricsDisplayType;
                            break;
                        case LyricsWindowMode.PictureInPictureMode:
                            LiveStates.CurrentLyricsStyleSettings = _settingsService.AppSettings.PictureInPictureLyricsStyleSettings;
                            LiveStates.CurrentLyricsEffectSettings = _settingsService.AppSettings.PictureInPictureLyricsEffectSettings;
                            LiveStates.CurrentLyricsDisplayType = _settingsService.AppSettings.PictureInPictureModeSettings.LyricsDisplayType;
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        public void Receive(PropertyChangedMessage<LyricsDisplayType> message)
        {
            if (message.Sender is StandardModeSettings)
            {
                if (message.PropertyName == nameof(StandardModeSettings.LyricsDisplayType))
                {
                    if (LiveStates.CurrentLyricsWindowMode == LyricsWindowMode.StandardMode)
                    {
                        LiveStates.CurrentLyricsDisplayType = message.NewValue;
                    }
                }
            }
            else if (message.Sender is DockModeSettings)
            {
                if (message.PropertyName == nameof(DockModeSettings.LyricsDisplayType))
                {
                    if (LiveStates.CurrentLyricsWindowMode == LyricsWindowMode.DockMode)
                    {
                        LiveStates.CurrentLyricsDisplayType = message.NewValue;
                    }
                }
            }
            else if (message.Sender is DesktopModeSettings)
            {
                if (message.PropertyName == nameof(DesktopModeSettings.LyricsDisplayType))
                {
                    if (LiveStates.CurrentLyricsWindowMode == LyricsWindowMode.DesktopMode)
                    {
                        LiveStates.CurrentLyricsDisplayType = message.NewValue;
                    }
                }
            }
            else if (message.Sender is PictureInPictureModeSettings)
            {
                if (message.PropertyName == nameof(PictureInPictureModeSettings.LyricsDisplayType))
                {
                    if (LiveStates.CurrentLyricsWindowMode == LyricsWindowMode.PictureInPictureMode)
                    {
                        LiveStates.CurrentLyricsDisplayType = message.NewValue;
                    }
                }
            }
        }
    }
}
