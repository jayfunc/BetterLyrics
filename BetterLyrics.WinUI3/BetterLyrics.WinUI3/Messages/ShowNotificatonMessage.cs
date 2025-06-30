// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Models;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace BetterLyrics.WinUI3.Messages
{
    public class ShowNotificatonMessage(Notification value) : ValueChangedMessage<Notification>(value) { }
}
