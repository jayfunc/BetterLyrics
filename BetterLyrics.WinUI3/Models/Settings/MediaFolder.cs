using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Services.FileSystemService;
using BetterLyrics.WinUI3.Services.FileSystemService.Providers;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Text.Json.Serialization;

namespace BetterLyrics.WinUI3.Models.Settings
{
    public partial class MediaFolder : ObservableRecipient
    {
        [ObservableProperty] public partial string Id { get; set; } = Guid.NewGuid().ToString();

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsEnabled { get; set; } = true;

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        [NotifyPropertyChangedFor(nameof(IsLocal))]
        [NotifyPropertyChangedFor(nameof(ConnectionSummary))]
        [NotifyPropertyChangedFor(nameof(UriString))]
        public partial FileSourceType SourceType { get; set; } = FileSourceType.Local;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial string Name { get; set; }

        // 连接属性
        [ObservableProperty][NotifyPropertyChangedFor(nameof(UriString))] public partial string UserName { get; set; }
        [ObservableProperty][NotifyPropertyChangedFor(nameof(UriString))] public partial string UriScheme { get; set; }
        [ObservableProperty][NotifyPropertyChangedFor(nameof(UriString))] public partial string UriHost { get; set; }
        [ObservableProperty][NotifyPropertyChangedFor(nameof(UriString))] public partial int UriPort { get; set; } = -1;

        [JsonPropertyName("Path")]
        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        [NotifyPropertyChangedFor(nameof(ConnectionSummary))]
        [NotifyPropertyChangedFor(nameof(UriString))]
        public partial string UriPath { get; set; }

        [JsonIgnore] public string Password { get; set; }

        [JsonIgnore] public bool IsLocal => SourceType == FileSourceType.Local;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsProcessing { get; set; } = false;
        [ObservableProperty] public partial double IndexingProgress { get; set; } = 0;
        [ObservableProperty] public partial string StatusText { get; set; } = "";
        [ObservableProperty] public partial InfoBarSeverity StatusSeverity { get; set; } = InfoBarSeverity.Informational;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial DateTime? LastSyncTime { get; set; }
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial AutoScanInterval ScanInterval { get; set; } = AutoScanInterval.Disabled;

        public Uri GetStandardUri()
        {
            try
            {
                if (IsLocal)
                {
                    return new Uri(UriPath);
                }

                var builder = new UriBuilder
                {
                    Scheme = UriScheme ?? "file",
                    Host = UriHost,
                    Port = UriPort,
                };

                if (!string.IsNullOrEmpty(UriPath))
                {
                    string cleanPath = UriPath.Replace("\\", "/");
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

        // 例：smb://user@host:445/share/path
        [JsonIgnore]
        public string UriString => GetStandardUri().AbsoluteUri;

        [JsonIgnore]
        public string ConnectionSummary
        {
            get
            {
                if (IsLocal) return UriPath;
                return $"{UriScheme}://{UriHost}{(UriPort > 0 ? ":" + UriPort : "")}/{UriPath?.TrimStart('/', '\\')} {(string.IsNullOrEmpty(UserName) ? "" : $"({UserName})")}";
            }
        }

        [JsonIgnore] public string VaultKey => $"{Id}-{UserName}";

        public MediaFolder() { }

        public MediaFolder(string path)
        {
            UriPath = path;
            SourceType = FileSourceType.Local;
        }

        public IUnifiedFileSystem? CreateFileSystem()
        {
            if (!IsEnabled) return null;
            if (string.IsNullOrEmpty(Password) && !IsLocal)
            {
                Password = PasswordVaultHelper.Get(Constants.App.AppName, VaultKey) ?? "";
            }

            return SourceType switch
            {
                FileSourceType.Local => new LocalFileSystem(this),
                FileSourceType.SMB => new SMBFileSystem(this),
                FileSourceType.FTP => new FTPFileSystem(this),
                FileSourceType.WebDAV => new WebDavFileSystem(this),
                _ => throw new NotImplementedException()
            };
        }

    }
}