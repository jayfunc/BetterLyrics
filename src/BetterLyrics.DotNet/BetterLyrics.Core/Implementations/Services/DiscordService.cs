using BetterLyrics.Core.Constants;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models;
using DiscordRPC;
using Microsoft.Extensions.Logging;

namespace BetterLyrics.Core.Implementations.Services;

/// <summary>
/// Singleton Service
/// </summary>
public class DiscordService : IDiscordService
{
    private readonly ISongSearchMapService _songSearchMapService;
    private readonly ILogger<DiscordService> _logger;
    private DiscordRpcClient? _client;

    public User? CurrentUser { get; private set; }
    public event EventHandler<User?>? UserChanged;

    public DiscordService(ISongSearchMapService songSearchMapService, ILogger<DiscordService> logger)
    {
        _songSearchMapService = songSearchMapService;
        _logger = logger;
        Enable();
    }

    public void Enable()
    {
        if (_client == null)
        {
            try
            {
                _client = new DiscordRpcClient(Discord.AppID);
                _client.OnReady += (sender, args) =>
                {
                    CurrentUser = args.User;
                    _logger.LogInformation("Discord Rich Presence connected as {UserName}.", CurrentUser?.Username);
                    UserChanged?.Invoke(this, CurrentUser);
                };
                _client.Initialize();
                _logger.LogDebug("Discord Rich Presence client initialized.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Discord Rich Presence.");
                _client?.Dispose();
                _client = null;
            }
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

        _logger.LogDebug("Updating Discord Rich Presence: {Title} - {Artist} (playing: {IsPlaying}).",
            mappedTitle, mappedArtist, isPlaying);

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

    public void ClearRichPresence()
    {
        _logger.LogDebug("Clearing Discord Rich Presence.");
        _client?.ClearPresence();
    }

    public void Disable()
    {
        if (_client != null)
        {
            _logger.LogInformation("Disabling Discord Rich Presence.");
            _client.ClearPresence();
            _client.Dispose();
            _client = null;

            CurrentUser = null;
            UserChanged?.Invoke(this, null);
        }
    }
}