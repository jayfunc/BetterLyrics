using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Media.Control;

namespace WindowsMediaController
{
    public sealed class MediaManager : IDisposable
    {
        public delegate void SessionChangeDelegate(MediaSession mediaSession);
        public delegate void PlaybackChangeDelegate(MediaSession mediaSession, GlobalSystemMediaTransportControlsSessionPlaybackInfo playbackInfo);
        public delegate void SongChangeDelegate(MediaSession mediaSession, GlobalSystemMediaTransportControlsSessionMediaProperties mediaProperties);
        public delegate void TimelineChangeDelegate(MediaSession mediaSession, GlobalSystemMediaTransportControlsSessionTimelineProperties timelineProperties);

        /// <summary>
        /// Triggered when a new media source gets added to the <see cref="CurrentMediaSessions"/> dictionary.
        /// </summary>
        public event SessionChangeDelegate OnAnySessionOpened;

        /// <summary>
        /// Triggered when a media source gets removed from the <see cref="CurrentMediaSessions"/> dictionary.
        /// </summary>
        public event SessionChangeDelegate OnAnySessionClosed;

        /// <summary>
        /// Triggered when the focused <see cref="MediaSession"/> changes.
        /// </summary>
        public event SessionChangeDelegate OnFocusedSessionChanged;

        /// <summary>
        /// Triggered when a playback state changes of any <see cref="MediaSession"/>.
        /// </summary>
        public event PlaybackChangeDelegate OnAnyPlaybackStateChanged;

        /// <summary>
        /// Triggered when a song changes of any <see cref="MediaSession"/>.
        /// </summary>
        public event SongChangeDelegate OnAnyMediaPropertyChanged;

        /// <summary>
        /// Triggered when the timeline changes of any <see cref="MediaSession"/>.
        /// </summary>
        public event TimelineChangeDelegate OnAnyTimelinePropertyChanged;

        /// <summary>
        /// A dictionary of the current <c>(<see cref="string"/> MediaSessionIds, <see cref="MediaSession"/> MediaSessionInstance)</c>
        /// </summary>
        public IReadOnlyDictionary<string, MediaSession> CurrentMediaSessions => _CurrentMediaSessions;
        private readonly ConcurrentDictionary<string, MediaSession> _CurrentMediaSessions = new ConcurrentDictionary<string, MediaSession>();

        /// <summary>
        /// Whether the <see cref="MediaSession"/> has started.
        /// </summary>
        public bool IsStarted { get; private set; }

        /// <summary>
        /// The <see cref="GlobalSystemMediaTransportControlsSessionManager"/> component from the Windows library.
        /// </summary>
        /// <seealso href="https://docs.microsoft.com/en-us/uwp/api/windows.media.control.globalsystemmediatransportcontrolssessionmanager"/>
        public GlobalSystemMediaTransportControlsSessionManager WindowsSessionManager { get; private set; }

        /// <summary>
        /// The <see cref="ILogger"/> used for logging.
        /// </summary>
        public ILogger Logger { get; set; }

        /// <summary>
        /// Starts the <see cref="MediaSession"/>.
        /// </summary>
        public void Start()
        {
            CheckStarted(true);

            //Populate CurrentMediaSessions with already open Sessions
            WindowsSessionManager = GlobalSystemMediaTransportControlsSessionManager.RequestAsync().GetAwaiter().GetResult();
            CompleteStart();
        }


        /// <summary>
        /// Starts the <see cref="MediaSession"/> asynchronously.
        /// </summary>
        public async Task StartAsync()
        {
            CheckStarted(true);

            //Populate CurrentMediaSessions with already open Sessions
            WindowsSessionManager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            CompleteStart();
        }

        /// <summary>
        /// Force updates <see cref="CurrentMediaSessions"/> dictionary and triggers <see cref="OnFocusedSessionChanged"/>. Exists to help mitigate bug where some events stop triggering: <a href="https://github.com/DubyaDude/WindowsMediaController/issues/6">Github Issue Link</a>.
        /// </summary>
        public void ForceUpdate()
        {
            SessionsChanged(WindowsSessionManager);
            CurrentSessionChanged(WindowsSessionManager);
        }

        /// <summary>
        /// Gets the currently focused <see cref="MediaSession"/>.
        /// </summary>
        public MediaSession GetFocusedSession()
        {
            CheckStarted(false);
            return GetFocusedSession(WindowsSessionManager);
        }

        private void CheckStarted(bool checkStarted)
        {
            if (IsStarted && checkStarted)
            {
                throw new InvalidOperationException("MediaManager already started");
            }
            else if (!IsStarted && !checkStarted)
            {
                throw new InvalidOperationException("MediaManager has not started");
            }
        }

        private void CompleteStart()
        {
            ForceUpdate();
            WindowsSessionManager.SessionsChanged += SessionsChanged;
            WindowsSessionManager.CurrentSessionChanged += CurrentSessionChanged;
            IsStarted = true;
        }

        private void CurrentSessionChanged(GlobalSystemMediaTransportControlsSessionManager sender, CurrentSessionChangedEventArgs args = null)
        {
            MediaSession currentMediaSession = GetFocusedSession(sender);

            try
            {
                OnFocusedSessionChanged?.Invoke(currentMediaSession);
            }
            catch (Exception exception)
            {
                Logger?.LogError(exception, "Error in OnFocusedSessionChanged Invoke");
            }
        }

        private MediaSession GetFocusedSession(GlobalSystemMediaTransportControlsSessionManager sender)
        {
            GlobalSystemMediaTransportControlsSession currentSession = null;
            try
            {
                currentSession = sender.GetCurrentSession();
            }
            catch (Exception exception)
            {
                Logger?.LogError(exception, "Error when getting CurrentSession");
            }

            if (currentSession != null && _CurrentMediaSessions.TryGetValue(currentSession.SourceAppUserModelId, out MediaSession mediaSession))
            {
                return mediaSession;
            }

            return null;
        }

        private void SessionsChanged(GlobalSystemMediaTransportControlsSessionManager winSessionManager, SessionsChangedEventArgs args = null)
        {
            IReadOnlyList<GlobalSystemMediaTransportControlsSession> controlSessionList;
            try
            {
                controlSessionList = winSessionManager.GetSessions();
            }
            catch (Exception exception)
            {
                Logger?.LogError(exception, "Error when getting Sessions");
                return;
            }

            //Checking for any new sessions, if found a new one add it to our dictiony and fire OnNewSource
            foreach (var controlSession in controlSessionList)
            {
                if (!_CurrentMediaSessions.ContainsKey(controlSession.SourceAppUserModelId))
                {
                    MediaSession mediaSession = new MediaSession(controlSession, this);
                    _CurrentMediaSessions[controlSession.SourceAppUserModelId] = mediaSession;

                    try
                    {
                        OnAnySessionOpened?.Invoke(mediaSession);
                    }
                    catch (Exception exception)
                    {
                        Logger?.LogError(exception, "Error in OnAnySessionOpened Invoke");
                    }

                    mediaSession.OnTimelinePropertiesChanged(controlSession);
                    mediaSession.OnSongChangeAsync(controlSession);
                }
            }

            //Checking if a source fell off the session list without doing a proper Closed event (*cough* spotify *cough*)
            string[] controlSessionIds = new string[controlSessionList.Count];
            for (int i = 0; i < controlSessionList.Count; i++)
            {
                controlSessionIds[i] = controlSessionList[i].SourceAppUserModelId;
            }

            List<MediaSession> sessionsToRemove = new List<MediaSession>();

            foreach (var session in _CurrentMediaSessions)
            {
                if (Array.IndexOf(controlSessionIds, session.Key) == -1)
                {
                    sessionsToRemove.Add(session.Value);
                }
            }

            foreach (var session in sessionsToRemove)
            {
                session.Dispose();
            }
        }

        private bool RemoveSource(MediaSession mediaSession)
        {
            if (_CurrentMediaSessions.TryRemove(mediaSession.Id, out _))
            {
                try
                {
                    OnAnySessionClosed?.Invoke(mediaSession);
                }
                catch (Exception exception)
                {
                    Logger?.LogError(exception, "Error in OnAnySessionClosed Invoke");
                }

                return true;
            }
            return false;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            OnAnySessionOpened = null;
            OnAnySessionClosed = null;
            OnAnyMediaPropertyChanged = null;
            OnAnyPlaybackStateChanged = null;
            OnFocusedSessionChanged = null;

            var keys = new List<string>(_CurrentMediaSessions.Keys);
            foreach (var key in keys)
            {
                _CurrentMediaSessions[key].Dispose();
            }
            _CurrentMediaSessions?.Clear();

            IsStarted = false;
            WindowsSessionManager.SessionsChanged -= SessionsChanged;
            WindowsSessionManager.CurrentSessionChanged -= CurrentSessionChanged;
            WindowsSessionManager = null;
            Logger = null;
        }

        public sealed class MediaSession
        {
            /// <summary>
            /// Triggered when this media source gets removed from the <see cref="CurrentMediaSessions"/> dictionary.
            /// </summary>
            public event SessionChangeDelegate OnSessionClosed;

            /// <summary>
            /// Triggered when a playback state changes of the <see cref="MediaSession"/>.
            /// </summary>
            public event PlaybackChangeDelegate OnPlaybackStateChanged;

            /// <summary>
            /// Triggered when a song changes of the <see cref="MediaSession"/>.
            /// </summary>
            public event SongChangeDelegate OnMediaPropertyChanged;

            /// <summary>
            /// Triggered when the timeline changes of the <see cref="MediaSession"/>.
            /// </summary>
            public event TimelineChangeDelegate OnTimelinePropertyChanged;

            /// <summary>
            /// The <see cref="GlobalSystemMediaTransportControlsSession"/> component from the Windows library.
            /// </summary>
            /// <seealso href="https://docs.microsoft.com/en-us/uwp/api/windows.media.control.globalsystemmediatransportcontrolssession"/>
            public GlobalSystemMediaTransportControlsSession ControlSession { get; private set; }

            /// <summary>
            /// The Unique Id of the <see cref="MediaSession"/>, grabbed from <see cref="GlobalSystemMediaTransportControlsSession.SourceAppUserModelId"/> from the Windows library.
            /// </summary>
            /// <seealso href="https://docs.microsoft.com/en-us/uwp/api/windows.media.control.globalsystemmediatransportcontrolssession.sourceappusermodelid"/>
            public readonly string Id;

            internal MediaManager MediaManagerInstance;

            internal MediaSession(GlobalSystemMediaTransportControlsSession controlSession, MediaManager mediaMangerInstance)
            {
                MediaManagerInstance = mediaMangerInstance;
                ControlSession = controlSession;
                Id = ControlSession.SourceAppUserModelId;
                ControlSession.MediaPropertiesChanged += OnSongChangeAsync;
                ControlSession.PlaybackInfoChanged += OnPlaybackInfoChanged;
                ControlSession.TimelinePropertiesChanged += OnTimelinePropertiesChanged;
            }

            private void OnPlaybackInfoChanged(GlobalSystemMediaTransportControlsSession controlSession, PlaybackInfoChangedEventArgs args = null)
            {
                try
                {
                    var playbackInfo = controlSession.GetPlaybackInfo();

                    if (playbackInfo.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Closed)
                    {
                        Dispose();
                    }
                    else
                    {
                        try
                        {
                            OnPlaybackStateChanged?.Invoke(this, playbackInfo);
                        }
                        catch (Exception exception)
                        {
                            MediaManagerInstance.Logger?.LogError(exception, "[{mediaId}] Error in OnPlaybackStateChanged Invoke", Id);
                        }

                        try
                        {
                            MediaManagerInstance.OnAnyPlaybackStateChanged?.Invoke(this, playbackInfo);
                        }
                        catch (Exception exception)
                        {
                            MediaManagerInstance.Logger?.LogError(exception, "[{mediaId}] Error in OnAnyPlaybackStateChanged Invoke", Id);
                        }
                    }
                }
                catch (Exception exception)
                {
                    MediaManagerInstance.Logger?.LogError(exception, "[{mediaId}] Error when getting PlaybackInfo", Id);
                }
            }

            internal async void OnSongChangeAsync(GlobalSystemMediaTransportControlsSession controlSession, MediaPropertiesChangedEventArgs args = null)
            {
                try
                {
                    var mediaProperties = await controlSession.TryGetMediaPropertiesAsync();

                    try
                    {
                        OnMediaPropertyChanged?.Invoke(this, mediaProperties);
                    }
                    catch (Exception exception)
                    {
                        MediaManagerInstance.Logger?.LogError(exception, "[{mediaId}] Error in OnMediaPropertyChanged Invoke", Id);
                    }

                    try
                    {
                        MediaManagerInstance.OnAnyMediaPropertyChanged?.Invoke(this, mediaProperties);
                    }
                    catch (Exception exception)
                    {
                        MediaManagerInstance.Logger?.LogError(exception, "[{mediaId}] Error in OnAnyMediaPropertyChanged Invoke", Id);
                    }
                }
                catch (Exception exception)
                {
                    // Silence "The RPC server is unavailable. (0x800706BA)" and "The device is not ready. (0x80070015)"
                    if (exception.Message.Contains("0x800706BA") || exception.Message.Contains("0x80070015"))
                    {
                        MediaManagerInstance.Logger?.LogWarning(exception, "[{mediaId}] Ignorable error when getting MediaProperties", Id);
                    }
                    else
                    {
                        MediaManagerInstance.Logger?.LogError(exception, "[{mediaId}] Error when getting MediaProperties", Id);
                    }
                }
            }

            internal void OnTimelinePropertiesChanged(GlobalSystemMediaTransportControlsSession sender, TimelinePropertiesChangedEventArgs args = null)
            {
                try
                {
                    var timelineProperties = sender.GetTimelineProperties();

                    try
                    {
                        OnTimelinePropertyChanged?.Invoke(this, timelineProperties);
                    }
                    catch (Exception exception)
                    {
                        MediaManagerInstance.Logger?.LogError(exception, "[{mediaId}] Error in OnTimelinePropertyChanged Invoke", Id);
                    }

                    try
                    {
                        MediaManagerInstance.OnAnyTimelinePropertyChanged?.Invoke(this, timelineProperties);
                    }
                    catch (Exception exception)
                    {
                        MediaManagerInstance.Logger?.LogError(exception, "[{mediaId}] Error in OnAnyTimelinePropertyChanged Invoke", Id);
                    }
                }
                catch (Exception exception)
                {
                    MediaManagerInstance.Logger?.LogError(exception, "[{mediaId}] Error when getting TimelineProperties", Id);
                }
            }

            internal void Dispose()
            {
                if (MediaManagerInstance.RemoveSource(this))
                {
                    OnPlaybackStateChanged = null;
                    OnMediaPropertyChanged = null;
                    OnSessionClosed = null;
                    ControlSession.PlaybackInfoChanged -= OnPlaybackInfoChanged;
                    ControlSession.MediaPropertiesChanged -= OnSongChangeAsync;
                    ControlSession.TimelinePropertiesChanged -= OnTimelinePropertiesChanged;
                    ControlSession = null;

                    try
                    {
                        OnSessionClosed?.Invoke(this);
                    }
                    catch (Exception exception)
                    {
                        MediaManagerInstance.Logger?.LogError(exception, "[{mediaId}] Error in OnSessionClosed Invoke", Id);
                    }
                }
            }
        }
    }
}
