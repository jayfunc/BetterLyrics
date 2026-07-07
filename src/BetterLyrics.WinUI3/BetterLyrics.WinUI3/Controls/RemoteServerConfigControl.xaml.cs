using System;
using System.IO;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.WinUI3.Helpers;
using BetterLyrics.WinUI3.Providers;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace BetterLyrics.WinUI3.Controls;

public sealed partial class RemoteServerConfigControl : UserControl
{
    private readonly FileSourceType _fileSourceType;
    private readonly ILocalizationService _localizationService = Ioc.Default.GetRequiredService<ILocalizationService>();
    private readonly IFilePickerProvider _filePickerProvider = Ioc.Default.GetRequiredService<IFilePickerProvider>();

    public RemoteServerConfigControl(FileSourceType fileSourceType)
    {
        InitializeComponent();
        _fileSourceType = fileSourceType;

        SetupDefaults();
        CheckPathForWarning();
    }

    private void SetupDefaults()
    {
        if (_fileSourceType == FileSourceType.Local)
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

            switch (_fileSourceType)
            {
                case FileSourceType.SMB:
                    PortBox.Value = 445;
                    PathBox.PlaceholderText = "SharedMusic";
                    break;
                case FileSourceType.FTP:
                    PortBox.Value = 21;
                    PathBox.PlaceholderText = "/pub/music";
                    break;
                case FileSourceType.WebDAV:
                    PortBox.Value = 80;
                    PathBox.PlaceholderText = "/dav/music";
                    break;
            }
        }
    }

    private string GetScheme()
    {
        var scheme = string.Empty;
        switch (_fileSourceType)
        {
            case FileSourceType.SMB:
                scheme = "smb";
                break;
            case FileSourceType.FTP:
                scheme = "ftp";
                break;
            case FileSourceType.WebDAV:
                scheme = "https";
                break;
        }

        return scheme;
    }

    public MediaFolder GetConfig()
    {
        var finalName = HostBox.Text.Trim();

        if (_fileSourceType == FileSourceType.Local)
        {
            if (string.IsNullOrWhiteSpace(PathBox.Text))
                throw new ArgumentException(
                    _localizationService.GetLocalizedString("RemoteServerConfigControlPathRequired"));

            if (!string.IsNullOrWhiteSpace(NameBox.Text))
                finalName = NameBox.Text.Trim();
            else
                finalName = PathBox.Text.TrimEnd(Path.DirectorySeparatorChar);

            return new MediaFolder
            {
                Name = finalName,
                SourceType = FileSourceType.Local,
                UriScheme = "file",
                UriPath = PathBox.Text.Trim()
            };
        }

        if (string.IsNullOrWhiteSpace(HostBox.Text))
            throw new ArgumentException(
                _localizationService.GetLocalizedString("RemoteServerConfigControlServerAddressRequired"));

        if (!string.IsNullOrWhiteSpace(NameBox.Text))
            finalName = NameBox.Text.Trim();
        else
            finalName = $"{_fileSourceType} - {HostBox.Text}";

        var scheme = GetScheme();

        var folder = new MediaFolder
        {
            Name = finalName,
            SourceType = _fileSourceType,

            UriScheme = scheme,
            UriHost = HostBox.Text.Trim(), // ȥ����β�ո�
            UriPort = (int)PortBox.Value,

            UriPath = PathBox.Text.Trim(),

            UserName = UserBox.Text.Trim(),
            Password = PwdBox.Password
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

    private void CheckPathForWarning()
    {
        var path = PathBox.Text?.Trim();

        var isSymbolRoot = string.IsNullOrEmpty(path) ||
                           path == "/" ||
                           path == "\\";

        var isDriveRoot = false;
        if (!string.IsNullOrEmpty(path))
        {
            var normalized = path.TrimEnd('\\', '/');
            isDriveRoot = normalized.EndsWith(":") && normalized.Length == 2;
        }

        var isRoot = isSymbolRoot || isDriveRoot;

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

    private void PathBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        CheckPathForWarning();
    }

    private async void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var (_, folderPath) = await _filePickerProvider.PickSingleFolderAsync(WindowType.SettingsWindow);
            if (folderPath != null) PathBox.Text = folderPath;
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }
}