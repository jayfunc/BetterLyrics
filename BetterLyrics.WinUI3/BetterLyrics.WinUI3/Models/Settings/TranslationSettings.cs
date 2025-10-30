using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Models.Settings
{
    public partial class TranslationSettings : ObservableRecipient
    {
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsLibreTranslateEnabled { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial string LibreTranslateServer { get; set; } = string.Empty;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsTranslationEnabled { get; set; } = true;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial string SelectedTargetLanguageCode { get; set; } = LanguageHelper.GetDefaultTargetTranslationLanguageCode();
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsJyutpingEnabled { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial ChineseRomanization ChineseRomanization { get; set; }
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsChineseRomanizationEnabled { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsJapaneseRomanizationEnabled { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsTraditionalChineseEnabled { get; set; } = false;

        public TranslationSettings() { }
    }
}
