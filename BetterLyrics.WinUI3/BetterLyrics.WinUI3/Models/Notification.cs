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
        private InfoBarSeverity _severity;

        [ObservableProperty]
        private string? _message;

        [ObservableProperty]
        private bool _isForeverDismissable;

        [ObservableProperty]
        private Visibility _visibility;

        [ObservableProperty]
        private string? _relatedSettingsKeyName;

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
