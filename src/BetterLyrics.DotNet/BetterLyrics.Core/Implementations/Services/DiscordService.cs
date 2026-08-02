using BetterLyrics.Core.Constants;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models;
using DiscordRPC;

namespace BetterLyrics.Core.Implementations.Services;

public class DiscordService : IDiscordService
{
    private readonly ISongSearchMapService _songSearchMapService;
    private DiscordRpcClient? _client;
    
    public User? CurrentUser { get; private set; }
    public event EventHandler<User?>? UserChanged;

    public DiscordService(ISongSearchMapService songSearchMapService)
    {
        _songSearchMapService = songSearchMapService;
        Enable();
    }

    public void Enable()
    {
        if (_client == null)
        {
            _client = new DiscordRpcClient(Discord.AppID);
            _client.OnReady += (sender, args) =>
            {
                CurrentUser = args.User;
                UserChanged?.Invoke(this, CurrentUser);
            };
            _client.Initialize();
        }
    }

    public async Task UpdateRichPresenceAsync(SongInfo songInfo, bool isPlaying = true, TimeSpan? currentPosition = null, string? albumArtUrl = null)
    {
        var (mappedTitle, mappedArtist, _) = await _songSearchMapService.GetMappingAsync(songInfo);

        Timestamps? timestamps = null;
        if (isPlaying)
        {
            var start = DateTime.UtcNow.Subtract(currentPosition ?? TimeSpan.Zero);
            var end = DateTime.UtcNow.AddMilliseconds(songInfo.DurationMs).Subtract(currentPosition ?? TimeSpan.Zero);
            timestamps = new Timestamps { Start = start, End = end };
        }

        _client?.SetPresence(new RichPresence
        {
            StatusDisplay = StatusDisplayType.Details,
            Type = ActivityType.Listening,
            Buttons = [new() { Label = "Get this status", Url = Link.MicrosoftStore }],
            Assets = new Assets
            {
                LargeImageKey = string.IsNullOrEmpty(albumArtUrl) ? "banner" : albumArtUrl,
                SmallImageKey = "logo"
            },
            Details = mappedTitle,
            State = mappedArtist,
            Timestamps = timestamps
        });
    }

    public void Disable()
    {
        if (_client != null)
        {
            _client.ClearPresence();
            _client.Dispose();
            _client = null;
            
            CurrentUser = null;
            UserChanged?.Invoke(this, null);
        }
    }
}