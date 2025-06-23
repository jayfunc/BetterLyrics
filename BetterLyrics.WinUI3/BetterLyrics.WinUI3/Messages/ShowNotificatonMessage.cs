// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Models;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Messages
{
    /// <summary>
    /// Defines the <see cref="ShowNotificatonMessage" />
    /// </summary>
    public class ShowNotificatonMessage(Notification value)
        : ValueChangedMessage<Notification>(value)
    {
    }
}
