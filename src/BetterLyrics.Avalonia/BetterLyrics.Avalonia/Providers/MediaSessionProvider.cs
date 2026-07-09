using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Interfaces.Providers;

namespace BetterLyrics.Avalonia.Providers;

// 在数据模型中加入 R, G, B 用于生成专属主题色的封面
internal record MockSongInfo(string Title, string Artist, string Album, TimeSpan Duration, List<string> Genres, byte R, byte G, byte B);

public class MediaSessionProvider : IMediaSessionProvider
{
    // --- 模拟曲库 ---
    private static readonly Random _random = new Random();
    private static readonly List<MockSongInfo> _mockPlaylist = new()
    {
        //new("Blinding Lights", "The Weeknd", "After Hours", TimeSpan.FromMinutes(3).Add(TimeSpan.FromSeconds(20)), new(){"Synthwave", "Pop"}, 220, 20, 20), // 红色系
        new("夜曲 (Nocturne)", "周杰伦 (Jay Chou)", "十一月的萧邦", TimeSpan.FromMinutes(3).Add(TimeSpan.FromSeconds(46)), new(){"C-Pop", "R&B"}, 40, 40, 40),     // 黑色/深灰系
        //new("Bohemian Rhapsody", "Queen", "A Night at the Opera", TimeSpan.FromMinutes(5).Add(TimeSpan.FromSeconds(55)), new(){"Rock"}, 200, 180, 200),        // 淡紫系
        //new("Shape of You", "Ed Sheeran", "÷ (Divide)", TimeSpan.FromMinutes(3).Add(TimeSpan.FromSeconds(53)), new(){"Pop"}, 20, 150, 220),                  // 蓝色系
        //new("A Super Long Song Title That Might Break Your UI Layout If Not Handled Correctly", "Test Artist", "Test Album", TimeSpan.FromMinutes(2), new(){"Testing"}, 50, 200, 50), // 绿色系
        //new("Hotel California", "Eagles", "Hotel California", TimeSpan.FromMinutes(6).Add(TimeSpan.FromSeconds(30)), new(){"Classic Rock"}, 220, 180, 50),     // 金黄系
        //new("起风了", "买辣椒也用券", "起风了", TimeSpan.FromMinutes(5).Add(TimeSpan.FromSeconds(13)), new(){"Pop"}, 100, 200, 255),                              // 天蓝系
    };

    public string SessionId { get; }

    public string? Title { get; private set; }
    public string? Artist { get; private set; }
    public string? Album { get; private set; }
    public List<string>? Genres { get; private set; }
    public byte[]? Thumbnail { get; private set; } // 现在它将装载真实的 BMP 图片字节

    public SessionPlaybackStatus PlaybackStatus { get; private set; }
    public TimeSpan CurrentTime { get; internal set; }
    public TimeSpan EndTime { get; private set; }

    internal event Action<MediaSessionProvider>? MockPlaybackStateChanged;
    internal event Action<MediaSessionProvider>? MockTimelineChanged;
    internal event Action<MediaSessionProvider>? MockMediaPropertyChanged;

    public MediaSessionProvider(string sessionId)
    {
        SessionId = sessionId;
        PlaybackStatus = SessionPlaybackStatus.Playing;

        LoadRandomSong();
    }

    internal void LoadRandomSong()
    {
        var song = _mockPlaylist[_random.Next(_mockPlaylist.Count)];

        Title = song.Title;
        Artist = song.Artist;
        Album = song.Album;
        Genres = song.Genres;
        EndTime = song.Duration;
        CurrentTime = TimeSpan.Zero;

        // 生成一张 300x300 的真实渐变 BMP 图片作为专辑封面
        Thumbnail = GenerateGradientBmpBytes(300, 300, song.R, song.G, song.B);

        MockMediaPropertyChanged?.Invoke(this);
        MockTimelineChanged?.Invoke(this);
    }

    /// <summary>
    /// 纯 C# 内存生成 BMP 图片字节流 (无任何外部依赖)
    /// </summary>
    private byte[] GenerateGradientBmpBytes(int width, int height, byte baseR, byte baseG, byte baseB)
    {
        // BMP 行字节数必须是 4 的倍数
        int padding = (4 - ((width * 3) % 4)) % 4;
        int rowSize = width * 3 + padding;
        int imageSize = rowSize * height;
        int fileSize = 54 + imageSize;

        byte[] bmp = new byte[fileSize];

        // 1. BMP 文件头 (14 bytes)
        bmp[0] = 0x42; bmp[1] = 0x4D; // "BM"
        BitConverter.GetBytes(fileSize).CopyTo(bmp, 2); // File size
        bmp[10] = 54; // Pixel data offset

        // 2. DIB 信息头 (40 bytes)
        bmp[14] = 40; // DIB Header size
        BitConverter.GetBytes(width).CopyTo(bmp, 18);
        BitConverter.GetBytes(height).CopyTo(bmp, 22); // Note: positive height means bottom-up pixel array
        bmp[26] = 1;  // Color planes
        bmp[28] = 24; // 24 bits per pixel (RGB)

        // 3. 写入像素数据 (绘制对角线渐变)
        int offset = 54;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // 计算渐变权重 (0.2 到 1.0 之间)，产生一点明暗过渡效果
                float factor = 0.2f + 0.8f * ((x + y) / (float)(width + height));

                // 注意：BMP 的像素排列顺序是 B, G, R
                bmp[offset++] = (byte)Math.Clamp(baseB * factor, 0, 255); // Blue
                bmp[offset++] = (byte)Math.Clamp(baseG * factor, 0, 255); // Green
                bmp[offset++] = (byte)Math.Clamp(baseR * factor, 0, 255); // Red
            }
            offset += padding; // 补齐 4 的倍数
        }

        return bmp;
    }

    public Task TryRefreshMediaPropsAsync() => Task.CompletedTask;
    public Task TryRefreshPlaybackStateAsync() => Task.CompletedTask;
    public Task TryRefreshTimelinePropsAsync() => Task.CompletedTask;

    public Task TryChangePlaybackPositionAsync(TimeSpan timeSpan)
    {
        CurrentTime = timeSpan;
        MockTimelineChanged?.Invoke(this);
        return Task.CompletedTask;
    }

    public Task TryPauseAsync()
    {
        PlaybackStatus = SessionPlaybackStatus.Paused;
        MockPlaybackStateChanged?.Invoke(this);
        return Task.CompletedTask;
    }

    public Task TryPlayAsync()
    {
        PlaybackStatus = SessionPlaybackStatus.Playing;
        MockPlaybackStateChanged?.Invoke(this);
        return Task.CompletedTask;
    }

    public Task TrySkipNextAsync()
    {
        LoadRandomSong();
        return Task.CompletedTask;
    }

    public Task TrySkipPreviousAsync()
    {
        if (CurrentTime.TotalSeconds > 3)
        {
            CurrentTime = TimeSpan.Zero;
            MockTimelineChanged?.Invoke(this);
        }
        else
        {
            LoadRandomSong();
        }
        return Task.CompletedTask;
    }

    public Task TryStopAsync()
    {
        PlaybackStatus = SessionPlaybackStatus.Stopped;
        CurrentTime = TimeSpan.Zero;
        MockPlaybackStateChanged?.Invoke(this);
        MockTimelineChanged?.Invoke(this);
        return Task.CompletedTask;
    }
}