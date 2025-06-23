// 2025/6/23 by Zhe Fang

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Models
{
    /// <summary>
    /// Defines the <see cref="Notification" />
    /// </summary>
    public partial class Notification : ObservableObject
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Notification"/> class.
        /// </summary>
        /// <param name="message">The message<see cref="string?"/></param>
        /// <param name="severity">The severity<see cref="InfoBarSeverity"/></param>
        /// <param name="isForeverDismissable">The isForeverDismissable<see cref="bool"/></param>
        /// <param name="relatedSettingsKeyName">The relatedSettingsKeyName<see cref="string?"/></param>
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

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets a value indicating whether IsForeverDismissable
        /// </summary>
        [ObservableProperty]
        public partial bool IsForeverDismissable { get; set; }

        /// <summary>
        /// Gets or sets the Message
        /// </summary>
        [ObservableProperty]
        public partial string? Message { get; set; }

        /// <summary>
        /// Gets or sets the RelatedSettingsKeyName
        /// </summary>
        [ObservableProperty]
        public partial string? RelatedSettingsKeyName { get; set; }

        /// <summary>
        /// Gets or sets the Severity
        /// </summary>
        [ObservableProperty]
        public partial InfoBarSeverity Severity { get; set; }

        /// <summary>
        /// Gets or sets the Visibility
        /// </summary>
        [ObservableProperty]
        public partial Visibility Visibility { get; set; }

        #endregion
    }
}
