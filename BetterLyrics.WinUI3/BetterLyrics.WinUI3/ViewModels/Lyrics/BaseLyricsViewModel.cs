using System.Collections.ObjectModel;
using System.Linq;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Messages;
using BetterLyrics.WinUI3.Services.Settings;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Windows.UI;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class BaseLyricsViewModel : BaseSettingsViewModel
    {
        private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        private ObservableCollection<Color> _coverImageDominantColors =
        [
            .. Enumerable.Repeat(Colors.Transparent, ImageHelper.AccentColorCount),
        ];

        public ObservableCollection<Color> CoverImageDominantColors
        {
            get => _coverImageDominantColors;
            set
            {
                _coverImageDominantColors = value;
                OnPropertyChanged();
            }
        }

        public BaseLyricsViewModel(ISettingsService settingsService)
            : base(settingsService)
        {
            WeakReferenceMessenger.Default.Register<BaseLyricsViewModel, SongInfoChangedMessage>(
                this,
                (r, m) =>
                {
                    _dispatcherQueue.TryEnqueue(
                        DispatcherQueuePriority.High,
                        async () =>
                        {
                            if (m.Value?.AlbumArt == null)
                                CoverImageDominantColors =
                                [
                                    .. Enumerable.Repeat(
                                        Colors.Transparent,
                                        ImageHelper.AccentColorCount
                                    ),
                                ];
                            else
                            {
                                CoverImageDominantColors =
                                [
                                    .. await ImageHelper.GetAccentColorsFromByte(m.Value.AlbumArt),
                                ];
                            }
                        }
                    );
                }
            );
        }
    }
}
