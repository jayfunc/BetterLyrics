// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models.FileSystem;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Text.Json.Serialization;

namespace BetterLyrics.WinUI3.Models
{
    public partial class MediaFolder : ObservableRecipient
    {
        [ObservableProperty] public partial string Id { get; set; } = Guid.NewGuid().ToString();

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsEnabled { get; set; } = true;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsRealTimeWatchEnabled { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients][NotifyPropertyChangedFor(nameof(ConnectionSummary))] public partial string Path { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        [NotifyPropertyChangedFor(nameof(IsLocal))]
        [NotifyPropertyChangedFor(nameof(ConnectionSummary))]
        public partial FileSourceType SourceType { get; set; } = FileSourceType.Local;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial string Name { get; set; }

        [ObservableProperty] public partial string UserName { get; set; }

        [ObservableProperty] public partial int Port { get; set; } = -1;

        [JsonIgnore] public string Password { get; set; }

        [JsonIgnore] public bool IsLocal => SourceType == FileSourceType.Local;

        [JsonIgnore]
        public string ConnectionSummary
        {
            get
            {
                if (IsLocal) return Path;
                return $"{SourceType} - {Path} {(string.IsNullOrEmpty(UserName) ? "" : $"({UserName})")}";
            }
        }

        [JsonIgnore] public string VaultKey => $"{Id}-{UserName}";

        public MediaFolder() { }

        public MediaFolder(string path)
        {
            Path = path;
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
                FileSourceType.Local => new LocalFileSystem(Path),
                FileSourceType.SMB => new SMBFileSystem(Path, UserName, Password),
                FileSourceType.FTP => new FTPFileSystem(Path, UserName, Password, Port, Path),
                FileSourceType.WebDav => new WebDavFileSystem(Path, UserName, Password, Port, Path),
                _ => throw new NotImplementedException()
            };
        }
    }
}
