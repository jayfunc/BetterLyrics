using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace BetterLyrics.WinUI3.Models
{
    public partial class NotificationItem : INotifyPropertyChanged
    {
        public string Title { get; set; }
        public string? Message { get; set; }
        public InfoBarSeverity Severity { get; set; } = InfoBarSeverity.Informational;
        public bool IsClosable { get; set; } = true;
        public TimeSpan? Duration { get; set; }
        public bool IsRemoving { get; set; } = false;

        public Action<NotificationItem> OnCloseRequest { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
