using System.Collections.ObjectModel;
using BetterLyrics.Core.Enums;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterLyrics.Core.Models;

public partial class FolderNode : ObservableObject
{
    public FileSourceType SourceType { get; set; } = FileSourceType.Local;

    public string FolderName { get; set; } = "";

    public string FolderPath { get; set; } = "";

    public string MediaFolderId { get; set; } = "";

    public ObservableCollection<FolderNode> SubFolders { get; set; } = new();

    [ObservableProperty] public partial bool IsExpanded { get; set; }
}