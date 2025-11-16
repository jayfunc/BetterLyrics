using BetterLyrics.WinUI3.Models;
using DiscordRPC;

namespace BetterLyrics.WinUI3.Services.DiscordService
{
    public class DiscordService : IDiscordService
    {
        private DiscordRpcClient? _client;

        public DiscordService()
        {
        }

        public void Enable()
        {
            if (_client == null)
            {
                _client = new DiscordRpcClient(Constants.Discord.AppID);
                _client.Initialize();
            }
        }

        public void UpdateRichPresence(SongInfo songInfo)
        {
            _client?.SetPresence(new RichPresence
            {
                StatusDisplay = StatusDisplayType.Details,
                Type = ActivityType.Listening,
                Buttons = new Button[] { new() { Label = "Get this status", Url = Constants.Link.MicrosoftStoreUrl } },
                Assets = new Assets
                {
                    LargeImageKey = "banner",
                    SmallImageKey = "logo"
                },
                Details = songInfo.Title,
                State = string.Join("; ", songInfo.Artists),
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
