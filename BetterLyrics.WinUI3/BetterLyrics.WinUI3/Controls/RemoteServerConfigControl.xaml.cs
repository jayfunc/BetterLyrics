using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services.LocalizationService;
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
        }

        private void SetupDefaults()
        {
            switch (_protocolType.ToUpper())
            {
                case "SMB":
                    PortBox.Value = 445; // SMB 默认端口
                    PathBox.PlaceholderText = "SharedMusic";
                    break;
                case "FTP":
                    PortBox.Value = 21; // FTP 默认端口
                    PathBox.PlaceholderText = "/pub/music";
                    break;
                case "WEBDAV":
                    PortBox.Value = 80; // WebDAV 默认端口
                    PathBox.PlaceholderText = "/dav/music";
                    break;
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
            if (string.IsNullOrWhiteSpace(HostBox.Text))
                throw new ArgumentException(_localizationService.GetLocalizedString("RemoteServerConfigControlServerAddressRequired"));

            string name = $"{_protocolType} - {HostBox.Text}";
            Enum.TryParse(_protocolType, true, out FileSourceType sourceType);

            string scheme = GetScheme();

            var folder = new MediaFolder
            {
                Name = name,
                SourceType = sourceType,

                UriScheme = scheme,
                UriHost = HostBox.Text.Trim(), // 去除首尾空格
                UriPort = (int)PortBox.Value,

                UriPath = PathBox.Text.Trim(),

                UserName = UserBox.Text.Trim(),
                Password = PwdBox.Password,
            };

            return folder;
        }

        public void ShowError(string message)
        {
            ErrorInfoBar.Message = message;
            ErrorInfoBar.IsOpen = true;
        }

        public void SetProgressBarVisibility(Visibility visibility)
        {
            ProgressBar.Visibility = visibility;
        }

    }
}