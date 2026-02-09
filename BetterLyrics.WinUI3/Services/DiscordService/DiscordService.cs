using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services.SongSearchMapService;
using DiscordRPC;

namespace BetterLyrics.WinUI3.Services.DiscordService
{
    public class DiscordService : IDiscordService
    {
        private readonly ISongSearchMapService _songSearchMapService;
        private DiscordRpcClient? _client;

        public DiscordService(ISongSearchMapService songSearchMapService)
        {
            _songSearchMapService = songSearchMapService;
        }

        public void Enable()
        {
            if (_client == null)
            {
                _client = new DiscordRpcClient(Constants.Discord.AppID);
                _client.Initialize();
            }
        }

        public async void UpdateRichPresence(SongInfo songInfo)
        {
            string mappedTitle = songInfo.Title;
            string mappedArtist = songInfo.Artist;
            string mappedAlbum = songInfo.Album;

            var mapped = await _songSearchMapService.GetMappingAsync(
                songInfo.Title,
                songInfo.Artist,
                songInfo.Album);

            if (mapped != null)
            {
                mappedTitle = mapped.MappedTitle;
                mappedArtist = mapped.MappedArtist;
                mappedAlbum = mapped.MappedAlbum;
            }

            _client?.SetPresence(new RichPresence
            {
                StatusDisplay = StatusDisplayType.Details,
                Type = ActivityType.Listening,
                Buttons = new Button[] { new() { Label = "Get this status", Url = Constants.Link.MicrosoftStore } },
                Assets = new Assets
                {
                    LargeImageKey = "banner",
                    SmallImageKey = "logo"
                },
                Details = songInfo.Title,
                State = songInfo.Artist,
                Timestamps = Timestamps.FromTimeSpan(songInfo.Duration)
            });
        }

        public void Disable()
        {
            _client?.ClearPresence();
            _client?.Dispose();
            _client = null;
        }

    }
}
