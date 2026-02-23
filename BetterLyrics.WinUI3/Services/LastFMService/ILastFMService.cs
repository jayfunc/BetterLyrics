using BetterLyrics.WinUI3.Events;
using BetterLyrics.WinUI3.Models;
using LiteFM.Abstractions;
using System;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Services.LastFMService
{
    public interface ILastFMService
    {
        LastFMUser? User { get; }
        bool IsAuthenticated { get; }

        event EventHandler<LastFMUserChangedEventArgs>? UserChanged;
        event EventHandler<LastFMIsAuthenticatedChangedEventArgs>? IsAuthenticatedChanged;

        Task AuthAsync();
        Task ConfirmAuthAsync(string param);
        Task UnAuthAsync();
        Task ConfirmUnAuthAsync();
        Task TrackAsync(SongInfo songInfo);
        Task RefreshAsync();
    }
}
