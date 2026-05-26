using Microsoft.Extensions.Logging;
using Serilog.Events;
using Serilog;
using System;
using Windows.Media.Control;
using WindowsMediaController;

namespace Sample.CMD
{
    class Program
    {
        static MediaManager mediaManager;
        static readonly object _writeLock = new object();

        public static void Main()
        {
            mediaManager = new MediaManager()
            {
                Logger = BuildLogger("MediaManager"),
            };

            mediaManager.OnAnySessionOpened += MediaManager_OnAnySessionOpened;
            mediaManager.OnAnySessionClosed += MediaManager_OnAnySessionClosed;
            mediaManager.OnFocusedSessionChanged += MediaManager_OnFocusedSessionChanged;
            mediaManager.OnAnyPlaybackStateChanged += MediaManager_OnAnyPlaybackStateChanged;
            mediaManager.OnAnyMediaPropertyChanged += MediaManager_OnAnyMediaPropertyChanged;
            mediaManager.OnAnyTimelinePropertyChanged += MediaManager_OnAnyTimelinePropertyChanged;

            mediaManager.Start();

            Console.ReadLine();
            mediaManager.Dispose();
        }

        private static void MediaManager_OnAnySessionOpened(MediaManager.MediaSession session)
        {
            WriteLineColor("-- New Source: " + session.Id, ConsoleColor.Green);
        }
        private static void MediaManager_OnAnySessionClosed(MediaManager.MediaSession session)
        {
            WriteLineColor("-- Removed Source: " + session.Id, ConsoleColor.Red);
        }

        private static void MediaManager_OnFocusedSessionChanged(MediaManager.MediaSession mediaSession)
        {
            WriteLineColor("== Session Focus Changed: " + mediaSession?.ControlSession?.SourceAppUserModelId, ConsoleColor.Gray);
        }

        private static void MediaManager_OnAnyPlaybackStateChanged(MediaManager.MediaSession sender, GlobalSystemMediaTransportControlsSessionPlaybackInfo args)
        {
            WriteLineColor($"{sender.Id} is now {args.PlaybackStatus}", ConsoleColor.Yellow);
        }

        private static void MediaManager_OnAnyMediaPropertyChanged(MediaManager.MediaSession sender, GlobalSystemMediaTransportControlsSessionMediaProperties args)
        {
            WriteLineColor($"{sender.Id} is now playing {args.Title} {(string.IsNullOrEmpty(args.Artist) ? "" : $"by {args.Artist}")}", ConsoleColor.Cyan);
        }

        private static void MediaManager_OnAnyTimelinePropertyChanged(MediaManager.MediaSession sender, GlobalSystemMediaTransportControlsSessionTimelineProperties args)
        {
            WriteLineColor($"{sender.Id} timeline is now {args.Position}/{args.EndTime}", ConsoleColor.Magenta);
        }

        public static void WriteLineColor(object toprint, ConsoleColor color = ConsoleColor.White)
        {
            lock (_writeLock)
            {
                Console.ForegroundColor = color;
                Console.WriteLine("[" + DateTime.Now.ToString("HH:mm:ss.fff") + "] " + toprint);
            }
        }

        private static Microsoft.Extensions.Logging.ILogger BuildLogger(string sourceContext = null)
        {
            return new LoggerFactory().AddSerilog(logger: new LoggerConfiguration()
                    .MinimumLevel.Is(LogEventLevel.Information)
                    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss.fff}] [{Level:u4}] " + (sourceContext ?? "{SourceContext}") + ": {Message:lj}{NewLine}{Exception}")
                    .CreateLogger())
                    .CreateLogger(string.Empty);
        }
    }
}
