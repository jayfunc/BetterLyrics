using System.Text.Json.Serialization;
using BetterLyrics.Core.Enums;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterLyrics.Core.Models.Settings;

public partial class MediaFolder : ObservableRecipient
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
    [NotifyPropertyChangedRecipients]
    public partial bool IsEnabled { get; set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    [NotifyPropertyChangedFor(nameof(IsLocal))]
    [NotifyPropertyChangedFor(nameof(ConnectionSummary))]
    [NotifyPropertyChangedFor(nameof(UriString))]
    public partial FileSourceType SourceType { get; set; } = FileSourceType.Local;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
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
    [NotifyPropertyChangedRecipients]
    [NotifyPropertyChangedFor(nameof(ConnectionSummary))]
    [NotifyPropertyChangedFor(nameof(UriString))]
    public partial string UriPath { get; set; }

    [JsonIgnore] public string Password { get; set; }

    [JsonIgnore] public bool IsLocal => SourceType == FileSourceType.Local;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool IsProcessing { get; set; } = false;

    [ObservableProperty] public partial double IndexingProgress { get; set; } = 0;
    [ObservableProperty] public partial string StatusText { get; set; } = "";
    [ObservableProperty] public partial MessageSeverity StatusSeverity { get; set; } = MessageSeverity.Informational;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial DateTime? LastSyncTime { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial AutoScanInterval ScanInterval { get; set; } = AutoScanInterval.Disabled;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool IsRealTimeScanEnabled { get; set; } = false;

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