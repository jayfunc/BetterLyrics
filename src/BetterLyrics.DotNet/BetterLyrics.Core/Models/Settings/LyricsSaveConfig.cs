using BetterLyrics.Core.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterLyrics.Core.Models.Settings;

public partial class LyricsSaveConfig : ObservableObject
{
    [ObservableProperty]
    public partial bool InSyllablesFormat { get; set; } = true;

    [ObservableProperty]
    public partial string FileNamePattern { get; set; } = "{Artist} - {Title}";

    [ObservableProperty]
    public partial bool IncludeTranslation { get; set; } = true;

    [ObservableProperty]
    public partial bool IncludeTransliteration { get; set; } = false;

    [ObservableProperty]
    public partial bool InOneLine { get; set; } = false;

    [ObservableProperty]
    public partial string SaveLocation { get; set; } = PathHelper.DocumentsFolderPath;
}