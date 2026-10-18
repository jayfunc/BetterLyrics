using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models.Entities;
using LiteDB;
using System.Collections.Specialized;
using System.ComponentModel;

namespace BetterLyrics.Core.Implementations.Services;

/// <summary>
/// Singleton Service
/// </summary>
public class PlaybackStateService : IPlaybackStateService, IDisposable
{
    private readonly IDatabaseService _databaseService;
    private readonly ISettingsService _settingsService;
    private readonly Debouncer _debouncer = new();
    private readonly object _lock = new();

    public PlaybackState CurrentState { get; }

    public PlaybackStateService(IDatabaseService databaseService, ISettingsService settingsService)
    {
        _databaseService = databaseService;
        _settingsService = settingsService;

        CurrentState = LoadInitialState();

        CurrentState.PropertyChanged += OnStatePropertyChanged;
        CurrentState.PlayQueuePaths.CollectionChanged += OnQueueCollectionChanged;
    }

    private ILiteCollection<PlaybackState> GetCollection()
    {
        return _databaseService.PlaybackStateDb.GetCollection<PlaybackState>("playbackState");
    }

    private PlaybackState LoadInitialState()
    {
        try
        {
            var col = GetCollection();
            var state = col.FindById(1);

            var oldSettings = _settingsService.AppSettings.MusicGallerySettings;
            var hasOldQueue = oldSettings.PlayQueuePaths.Count > 0;

            if (state == null)
            {
                state = new PlaybackState();

                // Migrate old playback status from settings if it exists
                if (hasOldQueue)
                {
                    state.PlayQueuePaths = [.. oldSettings.PlayQueuePaths];
                    state.PlayQueueIndex = oldSettings.PlayQueueIndex;
                    state.PlaybackPosition = oldSettings.PlaybackPosition;
                    col.Upsert(state);
                }
            }

            if (hasOldQueue)
            {
                oldSettings.PlayQueuePaths.Clear();
                oldSettings.PlayQueueIndex = -1;
                oldSettings.PlaybackPosition = TimeSpan.Zero;

                _settingsService.TryWriteImmediately();
            }

            return state;
        }
        catch
        {
            return new PlaybackState();
        }
    }

    private void OnStatePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        WriteState();
    }

    private void OnQueueCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        WriteState();
    }

    private void WriteState()
    {
        _ = _debouncer.RunAsync(() =>
        {
            lock (_lock)
            {
                try
                {
                    var col = GetCollection();
                    col.Upsert(CurrentState);
                }
                catch { }
            }
        }, delayMilliseconds: 500);
    }

    public void TryWriteImmediately()
    {
        lock (_lock)
        {
            try
            {
                var col = GetCollection();
                col.Upsert(CurrentState);
            }
            catch { }
        }
    }

    public void Dispose()
    {
        CurrentState.PropertyChanged -= OnStatePropertyChanged;
        CurrentState.PlayQueuePaths.CollectionChanged -= OnQueueCollectionChanged;
        _debouncer.Dispose();
    }
}