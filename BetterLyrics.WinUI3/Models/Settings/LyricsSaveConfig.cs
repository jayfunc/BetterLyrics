using BetterLyrics.WinUI3.Helper;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterLyrics.WinUI3.Models.Settings
{
    public partial class LyricsSaveConfig : ObservableRecipient
    {
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool InSyllablesFormat { get; set; } = true;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IncludeTranslation { get; set; } = true;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IncludeTransliteration { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool InOneLine { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial string SaveLocation { get; set; } = PathHelper.DocumentsFolder;
    }
}
