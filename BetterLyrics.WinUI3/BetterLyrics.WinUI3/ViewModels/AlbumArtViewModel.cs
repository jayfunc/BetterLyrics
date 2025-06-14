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
        private int? _coverImageRadius;
        public int CoverImageRadius
        {
            get
            {
                _coverImageRadius ??= Get(
                    SettingsKeys.CoverImageRadius,
                    SettingsDefaultValues.CoverImageRadius
                );
                return _coverImageRadius ?? 0;
            }
            set
            {
                Set(SettingsKeys.CoverImageRadius, value);
                _coverImageRadius = value;
                WeakReferenceMessenger.Default.Send(new AlbumArtCornerRadiusChangedMessage(value));
            }
        }

        public AlbumArtViewModel(ISettingsService settingsService)
            : base(settingsService) { }
    }
}
