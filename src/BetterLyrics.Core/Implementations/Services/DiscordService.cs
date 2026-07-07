using BetterLyrics.Core.Constants;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models;
using DiscordRPC;

namespace BetterLyrics.Core.Implementations.Services;

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
            _client = new DiscordRpcClient(Discord.AppID);
            _client.Initialize();
        }
    }

    public async void UpdateRichPresence(SongInfo songInfo)
    {
        var (mappedTitle, mappedArtist, mappedAlbum) = await _songSearchMapService.GetMappingAsync(songInfo);

        _client?.SetPresence(new RichPresence
        {
            StatusDisplay = StatusDisplayType.Details,
            Type = ActivityType.Listening,
            Buttons = new Button[] { new() { Label = "Get this status", Url = Link.MicrosoftStore } },
            Assets = new Assets
            {
                LargeImageKey = "banner",
                SmallImageKey = "logo"
            },
            Details = mappedTitle,
            State = mappedArtist,
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