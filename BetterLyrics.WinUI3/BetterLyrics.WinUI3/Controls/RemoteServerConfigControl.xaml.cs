using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services.ResourceService;
using CommunityToolkit.Mvvm.DependencyInjection;
using DevWinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.Storage;

namespace BetterLyrics.WinUI3.Controls
{
    public sealed partial class RemoteServerConfigControl : UserControl
    {
        private readonly string _protocolType;
        private readonly IResourceService _resourceService = Ioc.Default.GetRequiredService<IResourceService>();

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

        public MediaFolder GetConfig()
        {
            if (string.IsNullOrWhiteSpace(HostBox.Text))
                throw new ArgumentException(_resourceService.GetLocalizedString("RemoteServerConfigControlServerAddressRequired"));

            string name = $"{_protocolType} - {HostBox.Text}";

            Enum.TryParse(_protocolType, true, out FileSourceType sourceType);

            var folder = new MediaFolder
            {
                Name = name,
                Path = HostBox.Text, // 这里 Path 存的是 IP/Host
                Port = (int)PortBox.Value,
                UserName = UserBox.Text,
                Password = PwdBox.Password, // 从 PasswordBox 获取密码
                SourceType = sourceType,
                IsRealTimeWatchEnabled = false
            };

            // 特殊处理路径：
            // 我们需要把"远程路径"拼接到 Path 里，或者用另一个字段存
            // 为了简单，我们遵循上面的 MediaFolder 定义：
            // 建议 MediaFolder 类里再加一个 RemotePath 字段，或者在这里把 Path 组合起来

            // *修正建议*：如果不改 MediaFolder 定义，我们可以这样约定：
            // Path 字段存储格式： "192.168.1.5/Music"

            var rawPath = PathBox.Text.Trim().TrimStart('/', '\\'); // 去掉开头的斜杠
            if (!string.IsNullOrEmpty(rawPath))
            {
                // 简单的路径拼接逻辑
                if (sourceType == FileSourceType.SMB)
                {
                    // SMBLibrary 的逻辑通常是 Host 分开，ShareName 分开
                    // 如果你把 IP 存在 Path 属性里，那你需要把 ShareName 拼在后面或者用新字段
                    // 为了方便，这里把 IP 和 ShareName 拼在一起存入 Path
                    // 比如: 192.168.1.5/Music
                    folder.Path = $"{HostBox.Text}/{rawPath}";
                }
                else
                {
                    // FTP/WebDAV: 192.168.1.5/pub/music
                    folder.Path = $"{HostBox.Text}/{rawPath}";
                }
            }

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