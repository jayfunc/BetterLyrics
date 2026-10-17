using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Messages.MediaLibrary;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using System.Text.Json.Serialization;

namespace BetterLyrics.Core.Models.Settings;

public partial class MediaFolder : ObservableObject
{
    public MediaFolder()
    {
    }

    public MediaFolder(string path)
    {
        UriPath = path;
        SourceType = FileSourceType.Local;
    }

    [ObservableProperty] public partial string Id { get; set; } = Guid.NewGuid().ToString();

    [ObservableProperty]
    public partial bool IsEnabled { get; set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLocal))]
    [NotifyPropertyChangedFor(nameof(ConnectionSummary))]
    [NotifyPropertyChangedFor(nameof(UriString))]
    public partial FileSourceType SourceType { get; set; } = FileSourceType.Local;

    [ObservableProperty]
    public partial string Name { get; set; }

    // 连接属性
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UriString))]
    public partial string UserName { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UriString))]
    public partial string UriScheme { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UriString))]
    public partial string UriHost { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UriString))]
    public partial int UriPort { get; set; } = -1;

    [JsonPropertyName("Path")]
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ConnectionSummary))]
    [NotifyPropertyChangedFor(nameof(UriString))]
    public partial string UriPath { get; set; }

    [ObservableProperty]
    public partial string LocalLyricsFilePattern { get; set; } = "";

    partial void OnLocalLyricsFilePatternChanged(string value)
    {
        ForceNextSync = true;
    }

    [ObservableProperty]
    public partial string LocalMusicFilePattern { get; set; } = "";

    partial void OnLocalMusicFilePatternChanged(string value)
    {
        ForceNextSync = true;
    }

    [JsonIgnore] public bool ForceNextSync { get; set; } = false;

    [JsonIgnore] public string Password { get; set; }

    [JsonIgnore] public bool IsLocal => SourceType == FileSourceType.Local;

    [ObservableProperty]
    public partial bool IsProcessing { get; set; } = false;

    [ObservableProperty] public partial double IndexingProgress { get; set; } = 0;
    [ObservableProperty] public partial string StatusText { get; set; } = "";
    [ObservableProperty] public partial MessageSeverity StatusSeverity { get; set; } = MessageSeverity.Informational;

    [ObservableProperty]
    public partial DateTime? LastSyncTime { get; set; }

    [ObservableProperty]
    public partial AutoScanInterval ScanInterval { get; set; } = AutoScanInterval.Disabled;

    [ObservableProperty]
    public partial bool IsRealTimeScanEnabled { get; set; } = false;

    [ObservableProperty]
    public partial bool ScanSubDirectories { get; set; } = true;

    partial void OnIsEnabledChanged(bool value) =>
        WeakReferenceMessenger.Default.Send(new MediaFolderEnabledChangedMessage(this, value));

    partial void OnNameChanged(string value) =>
        WeakReferenceMessenger.Default.Send(new MediaFolderNameChangedMessage(this, value));

    partial void OnIsProcessingChanged(bool value) =>
        WeakReferenceMessenger.Default.Send(new MediaFolderProcessingChangedMessage(this, value));

    partial void OnLastSyncTimeChanged(DateTime? value) =>
        WeakReferenceMessenger.Default.Send(new MediaFolderLastSyncTimeChangedMessage(this, value));

    partial void OnScanIntervalChanged(AutoScanInterval value) =>
        WeakReferenceMessenger.Default.Send(new MediaFolderScanIntervalChangedMessage(this, value));

    partial void OnIsRealTimeScanEnabledChanged(bool value) =>
        WeakReferenceMessenger.Default.Send(new MediaFolderRealTimeScanChangedMessage(this, value));

    partial void OnScanSubDirectoriesChanged(bool value) =>
        WeakReferenceMessenger.Default.Send(new MediaFolderScanSubDirectoriesChangedMessage(this, value));

    // 例：smb://user@host:445/share/path
    [JsonIgnore] public string UriString => GetStandardUri().AbsoluteUri;

    [JsonIgnore]
    public string ConnectionSummary
    {
        get
        {
            if (IsLocal) return UriPath;
            return
                $"{UriScheme}://{UriHost}{(UriPort > 0 ? ":" + UriPort : "")}/{UriPath?.TrimStart('/', '\\')} {(string.IsNullOrEmpty(UserName) ? "" : $"({UserName})")}";
        }
    }

    [JsonIgnore] public string VaultKey => $"{Id}-{UserName}";

    public Uri GetStandardUri()
    {
        try
        {
            if (IsLocal) return new Uri(UriPath);

            var builder = new UriBuilder
            {
                Scheme = UriScheme ?? "file",
                Host = UriHost,
                Port = UriPort
            };

            if (!string.IsNullOrEmpty(UriPath))
            {
                var cleanPath = UriPath.Replace("\\", "/");
                if (!cleanPath.StartsWith("/")) cleanPath = "/" + cleanPath;
                builder.Path = cleanPath;
            }

            return builder.Uri;
        }
        catch (Exception)
        {
            return new Uri("about:blank");
        }
    }
}