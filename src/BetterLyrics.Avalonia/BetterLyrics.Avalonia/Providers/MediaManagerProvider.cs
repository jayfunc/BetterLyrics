using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Enums;

namespace BetterLyrics.Avalonia.Providers;

public class MediaManagerProvider : IMediaManagerProvider, IDisposable
{
    private readonly ConcurrentDictionary<string, IMediaSessionProvider> _mediaSessions = new();
    private MediaSessionProvider? _mockSession;
    private Timer? _playbackTimer;

    public IMediaSessionProvider? FocusedSession => _mockSession;

    public IEnumerable<IMediaSessionProvider> CurrentMediaSessions => _mediaSessions.Values;

    public event IMediaManagerProvider.SessionChangeDelegate? OnAnySessionOpened;
    public event IMediaManagerProvider.SessionChangeDelegate? OnAnySessionClosed;
    public event IMediaManagerProvider.SessionChangeDelegate? OnFocusedSessionChanged;
    public event IMediaManagerProvider.SessionChangeDelegate? OnAnyMediaPropertyChanged;
    public event IMediaManagerProvider.SessionChangeDelegate? OnAnyPlaybackStateChanged;
    public event IMediaManagerProvider.SessionChangeDelegate? OnAnyTimelinePropertyChanged;

    public void Init()
    {
        // 1. 创建模拟会话
        _mockSession = new MediaSessionProvider("Simulated_Avalonia_Session_01");

        // 订阅模拟会话的内部事件，用来模拟系统回调
        _mockSession.MockPlaybackStateChanged += MockSession_OnPlaybackStateChanged;
        _mockSession.MockTimelineChanged += MockSession_OnTimelineChanged;

        _mediaSessions.TryAdd(_mockSession.SessionId, _mockSession);

        // 2. 触发初始事件，通知 UI 层发现了新歌曲
        OnAnySessionOpened?.Invoke(_mockSession);
        OnFocusedSessionChanged?.Invoke(_mockSession);
        OnAnyMediaPropertyChanged?.Invoke(_mockSession);
        OnAnyPlaybackStateChanged?.Invoke(_mockSession);

        // 3. 启动后台定时器，每 500ms 更新一次进度条 (更平滑)
        _playbackTimer = new Timer(TimerCallback, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(500));
    }

    public bool IsMediaSessionExisting(string sessionId)
    {
        return _mediaSessions.ContainsKey(sessionId);
    }

    private void TimerCallback(object? state)
    {
        if (_mockSession == null || _mockSession.PlaybackStatus != SessionPlaybackStatus.Playing)
            return;

        // 每次推进 500 毫秒
        _mockSession.CurrentTime = _mockSession.CurrentTime.Add(TimeSpan.FromMilliseconds(500));

        // 如果放完了，模拟单曲循环
        if (_mockSession.CurrentTime >= _mockSession.EndTime)
        {
            _mockSession.CurrentTime = TimeSpan.Zero;
        }

        // 触发时间轴更新，UI 层的歌词同步器会收到这个事件
        OnAnyTimelinePropertyChanged?.Invoke(_mockSession);
    }

    private void MockSession_OnPlaybackStateChanged(MediaSessionProvider session)
    {
        OnAnyPlaybackStateChanged?.Invoke(session);
    }

    private void MockSession_OnTimelineChanged(MediaSessionProvider session)
    {
        OnAnyTimelinePropertyChanged?.Invoke(session);
    }

    public void Dispose()
    {
        // 停止定时器，防止内存泄漏和后台无效占用
        _playbackTimer?.Change(Timeout.Infinite, 0);
        _playbackTimer?.Dispose();

        if (_mockSession != null)
        {
            _mockSession.MockPlaybackStateChanged -= MockSession_OnPlaybackStateChanged;
            _mockSession.MockTimelineChanged -= MockSession_OnTimelineChanged;
        }

        _mediaSessions.Clear();
    }
}