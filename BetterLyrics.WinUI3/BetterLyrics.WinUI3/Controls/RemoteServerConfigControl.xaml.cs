using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services.LocalizationService;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace BetterLyrics.WinUI3.Controls
{
    public sealed partial class RemoteServerConfigControl : UserControl
    {
        private readonly string _protocolType;
        private readonly ILocalizationService _localizationService = Ioc.Default.GetRequiredService<ILocalizationService>();

        public RemoteServerConfigControl(string protocolType)
        {
            this.InitializeComponent();
            _protocolType = protocolType;

            SetupDefaults();
            CheckPathForWarning();
        }

        private void SetupDefaults()
        {
            if (_protocolType.Equals("Local", StringComparison.OrdinalIgnoreCase))
            {
                RemoteFieldsPanel.Visibility = Visibility.Collapsed;
                AuthFieldsPanel.Visibility = Visibility.Collapsed;

                BrowseButton.Visibility = Visibility.Visible;

                PathBox.PlaceholderText = @"D:\Music";
            }
            else
            {
                BrowseButton.Visibility = Visibility.Collapsed;
                RemoteFieldsPanel.Visibility = Visibility.Visible;
                AuthFieldsPanel.Visibility = Visibility.Visible;

                switch (_protocolType.ToUpper())
                {
                    case "SMB":
                        PortBox.Value = 445;
                        PathBox.PlaceholderText = "SharedMusic";
                        break;
                    case "FTP":
                        PortBox.Value = 21;
                        PathBox.PlaceholderText = "/pub/music";
                        break;
                    case "WEBDAV":
                        PortBox.Value = 80;
                        PathBox.PlaceholderText = "/dav/music";
                        break;
                }
            }
        }

        private string GetScheme()
        {
            string scheme = string.Empty;
            switch (_protocolType.ToUpper())
            {
                case "SMB":
                    scheme = "smb";
                    break;
                case "FTP":
                    scheme = "ftp";
                    break;
                case "WEBDAV":
                    scheme = "https";
                    break;
            }
            return scheme;
        }

        public MediaFolder GetConfig()
        {
            string finalName = HostBox.Text.Trim();

            if (_protocolType.Equals("Local", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(PathBox.Text))
                    throw new ArgumentException(_localizationService.GetLocalizedString("RemoteServerConfigControlPathRequired"));

                if (!string.IsNullOrWhiteSpace(NameBox.Text))
                    finalName = NameBox.Text.Trim();
                else
                    finalName = PathBox.Text.TrimEnd(System.IO.Path.DirectorySeparatorChar);

                return new MediaFolder
                {
                    Name = finalName,
                    SourceType = FileSourceType.Local,
                    UriScheme = "file",
                    UriPath = PathBox.Text.Trim(),
                };
            }

            if (string.IsNullOrWhiteSpace(HostBox.Text))
                throw new ArgumentException(_localizationService.GetLocalizedString("RemoteServerConfigControlServerAddressRequired"));

            if (!string.IsNullOrWhiteSpace(NameBox.Text))
            {
                finalName = NameBox.Text.Trim();
            }
            else
            {
                finalName = $"{_protocolType} - {HostBox.Text}";
            }

            Enum.TryParse(_protocolType, true, out FileSourceType sourceType);

            string scheme = GetScheme();

            var folder = new MediaFolder
            {
                Name = finalName,
                SourceType = sourceType,

                UriScheme = scheme,
                UriHost = HostBox.Text.Trim(), // È¥³ýÊ×Î²¿Õ¸ñ
                UriPort = (int)PortBox.Value,

                UriPath = PathBox.Text.Trim(),

                UserName = UserBox.Text.Trim(),
                Password = PwdBox.Password,
            };

            return folder;
        }

        public void ShowError(string? message)
        {
            ErrorInfoBar.Message = message;
            ErrorInfoBar.IsOpen = !string.IsNullOrWhiteSpace(message);
        }

        public void SetProgressBarVisibility(Visibility visibility)
        {
            ProgressBar.Visibility = visibility;
        }

        private void PathBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            CheckPathForWarning();
        }

        private void CheckPathForWarning()
        {
            string? path = PathBox.Text?.Trim();

            bool isSymbolRoot = string.IsNullOrEmpty(path) ||
                                path == "/" ||
                                path == "\\";

            bool isDriveRoot = false;
            if (!string.IsNullOrEmpty(path))
            {
                var normalized = path.TrimEnd('\\', '/');
                isDriveRoot = normalized.EndsWith(":") && normalized.Length == 2;
            }

            bool isRoot = isSymbolRoot || isDriveRoot;

            if (isRoot)
            {
                PathWarningBar.Message = _localizationService.GetLocalizedString("FileSystemServiceRootDirectoryWarning");
                PathWarningBar.IsOpen = true;
            }
            else
            {
                PathWarningBar.IsOpen = false;
            }
        }

        private async void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var folder = await PickerHelper.PickSingleFolderAsync<SettingsWindow>();
                if (folder != null)
                {
                    PathBox.Text = folder.Path;
                }
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }
    }
}