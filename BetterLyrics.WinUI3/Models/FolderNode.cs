using BetterLyrics.WinUI3.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace BetterLyrics.WinUI3.Models
{
    public partial class FolderNode : ObservableObject
    {
        public FileSourceType SourceType { get; set; } = FileSourceType.Local;

        public string FolderName { get; set; } = "";

        public string FolderPath { get; set; } = "";

        public string MediaFolderId { get; set; } = "";

        public ObservableCollection<FolderNode> SubFolders { get; set; } = new();

        [ObservableProperty] public partial bool IsExpanded { get; set; }
    }
}
