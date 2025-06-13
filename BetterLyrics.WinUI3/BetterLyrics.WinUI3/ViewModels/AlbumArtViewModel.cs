using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BetterLyrics.WinUI3.Messages;
using BetterLyrics.WinUI3.Services.Settings;
using CommunityToolkit.Mvvm.Messaging;

namespace BetterLyrics.WinUI3.ViewModels
{
    public class AlbumArtViewModel : BaseSettingsViewModel
    {
        public int CoverImageRadius
        {
            get => Get(SettingsKeys.CoverImageRadius, SettingsDefaultValues.CoverImageRadius);
            set
            {
                Set(SettingsKeys.CoverImageRadius, value);
                WeakReferenceMessenger.Default.Send(new AlbumArtCornerRadiusChangedMessage(value));
            }
        }

        public AlbumArtViewModel(ISettingsService settingsService)
            : base(settingsService) { }
    }
}
