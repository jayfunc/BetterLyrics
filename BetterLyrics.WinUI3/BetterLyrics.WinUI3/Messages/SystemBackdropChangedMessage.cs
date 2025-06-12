using BetterLyrics.WinUI3.Models;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace BetterLyrics.WinUI3.Messages
{
    /// <summary>
    ///
    /// </summary>
    /// <param name="value">If value is null, it means to refresh/update from settings</param>
    public class SystemBackdropChangedMessage(BackdropType? value)
        : ValueChangedMessage<BackdropType?>(value) { }
}
