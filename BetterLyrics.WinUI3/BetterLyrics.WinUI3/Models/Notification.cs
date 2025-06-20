using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace BetterLyrics.WinUI3.Models
{
    public partial class Notification : ObservableObject
    {
        [ObservableProperty]
        public partial InfoBarSeverity Severity { get; set; }

        [ObservableProperty]
        public partial string? Message { get; set; }

        [ObservableProperty]
        public partial bool IsForeverDismissable { get; set; }

        [ObservableProperty]
        public partial Visibility Visibility { get; set; }

        [ObservableProperty]
        public partial string? RelatedSettingsKeyName { get; set; }

        public Notification(
            string? message = null,
            InfoBarSeverity severity = InfoBarSeverity.Informational,
            bool isForeverDismissable = false,
            string? relatedSettingsKeyName = null
        )
        {
            Message = message;
            Severity = severity;
            IsForeverDismissable = isForeverDismissable;
            Visibility = IsForeverDismissable ? Visibility.Visible : Visibility.Collapsed;
            RelatedSettingsKeyName = relatedSettingsKeyName;
        }
    }
}
