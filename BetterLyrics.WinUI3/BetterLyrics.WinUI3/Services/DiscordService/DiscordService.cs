using BetterLyrics.WinUI3.Models;
using DiscordRPC;
using Microsoft.Windows.Storage;
using System;
using System.Diagnostics;
using static Vanara.PInvoke.Kernel32.REASON_CONTEXT;

namespace BetterLyrics.WinUI3.Services.DiscordService
{
    public class DiscordService : IDiscordService
    {
        private DiscordRpcClient? _client;
        private RichPresence _richPresence;

        public DiscordService()
        {
            _richPresence = new()
            {
                StatusDisplay = StatusDisplayType.Name,
                Type = ActivityType.Listening,
            };

            _client = new DiscordRpcClient(Constants.Discord.AppID);
            _client.OnReady += Client_OnReady;
            _client.Initialize();
        }

        public void UpdateRichPresence(SongInfo songInfo)
        {
            _richPresence.Details = songInfo.Title;
            _richPresence.State = songInfo.Artist;
            _richPresence.Timestamps = Timestamps.FromTimeSpan(songInfo.Duration ?? 0);
            _richPresence.Assets = new Assets
            {
            };
            _client?.SetPresence(_richPresence);
        }

        public void UpdateRichPresence(TimeSpan current, TimeSpan duration)
        {
            //_richPresence.Timestamps = new(DateTime.Now - current, DateTime.Now - current + duration);
            //_client?.SetPresence(_richPresence);
        }

        private void Client_OnReady(object sender, DiscordRPC.Message.ReadyMessage args)
        {
            Debug.WriteLine("Connected to discord with user {0}", args.User.Username);
            Debug.WriteLine("Avatar: {0}", args.User.GetAvatarURL(User.AvatarFormat.WebP));
            Debug.WriteLine("Decoration: {0}", args.User.GetAvatarDecorationURL());
        }
    }
}
