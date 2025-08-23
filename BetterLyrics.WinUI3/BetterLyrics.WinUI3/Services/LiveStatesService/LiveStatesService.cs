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
        IRecipient<PropertyChangedMessage<LyricsDisplayType>>,
        IRecipient<PropertyChangedMessage<bool>>
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
                if (message.PropertyName == nameof(LiveStates.LyricsWindowMode))
                {
                    switch (message.NewValue)
                    {
                        case LyricsWindowMode.StandardMode:
                            LiveStates.LyricsStyleSettings = _settingsService.AppSettings.StandardLyricsStyleSettings;
                            LiveStates.LyricsEffectSettings = _settingsService.AppSettings.StandardLyricsEffectSettings;
                            LiveStates.LyricsDisplayType = _settingsService.AppSettings.StandardModeSettings.LyricsDisplayType;
                            LiveStates.IsAlwaysOnTop = _settingsService.AppSettings.StandardModeSettings.IsAlwaysOnTop;
                            break;
                        case LyricsWindowMode.DockMode:
                            LiveStates.LyricsStyleSettings = _settingsService.AppSettings.DockLyricsStyleSettings;
                            LiveStates.LyricsEffectSettings = _settingsService.AppSettings.DockLyricsEffectSettings;
                            LiveStates.LyricsDisplayType = _settingsService.AppSettings.DockModeSettings.LyricsDisplayType;
                            LiveStates.IsAlwaysOnTop = true;
                            break;
                        case LyricsWindowMode.DesktopMode:
                            LiveStates.LyricsStyleSettings = _settingsService.AppSettings.DesktopLyricsStyleSettings;
                            LiveStates.LyricsEffectSettings = _settingsService.AppSettings.DesktopLyricsEffectSettings;
                            LiveStates.LyricsDisplayType = _settingsService.AppSettings.DesktopModeSettings.LyricsDisplayType;
                            LiveStates.IsAlwaysOnTop = _settingsService.AppSettings.DesktopModeSettings.IsAlwaysOnTop;
                            break;
                        case LyricsWindowMode.PictureInPictureMode:
                            LiveStates.LyricsStyleSettings = _settingsService.AppSettings.PictureInPictureLyricsStyleSettings;
                            LiveStates.LyricsEffectSettings = _settingsService.AppSettings.PictureInPictureLyricsEffectSettings;
                            LiveStates.LyricsDisplayType = _settingsService.AppSettings.PictureInPictureModeSettings.LyricsDisplayType;
                            // IsAlwaysOnTop 由系统托管
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
                    if (LiveStates.LyricsWindowMode == LyricsWindowMode.StandardMode)
                    {
                        LiveStates.LyricsDisplayType = message.NewValue;
                    }
                }
            }
            else if (message.Sender is DockModeSettings)
            {
                if (message.PropertyName == nameof(DockModeSettings.LyricsDisplayType))
                {
                    if (LiveStates.LyricsWindowMode == LyricsWindowMode.DockMode)
                    {
                        LiveStates.LyricsDisplayType = message.NewValue;
                    }
                }
            }
            else if (message.Sender is DesktopModeSettings)
            {
                if (message.PropertyName == nameof(DesktopModeSettings.LyricsDisplayType))
                {
                    if (LiveStates.LyricsWindowMode == LyricsWindowMode.DesktopMode)
                    {
                        LiveStates.LyricsDisplayType = message.NewValue;
                    }
                }
            }
            else if (message.Sender is PictureInPictureModeSettings)
            {
                if (message.PropertyName == nameof(PictureInPictureModeSettings.LyricsDisplayType))
                {
                    if (LiveStates.LyricsWindowMode == LyricsWindowMode.PictureInPictureMode)
                    {
                        LiveStates.LyricsDisplayType = message.NewValue;
                    }
                }
            }
        }

        public void Receive(PropertyChangedMessage<bool> message)
        {
            if (message.Sender is StandardModeSettings)
            {
                if (message.PropertyName == nameof(StandardModeSettings.IsAlwaysOnTop))
                {
                    if (LiveStates.LyricsWindowMode == LyricsWindowMode.StandardMode)
                    {
                        LiveStates.IsAlwaysOnTop = message.NewValue;
                    }
                }
            }
            else if (message.Sender is DesktopModeSettings)
            {
                if (message.PropertyName == nameof(DesktopModeSettings.IsAlwaysOnTop))
                {
                    if (LiveStates.LyricsWindowMode == LyricsWindowMode.DesktopMode)
                    {
                        LiveStates.IsAlwaysOnTop = message.NewValue;
                    }
                }
            }
        }
    }
}
