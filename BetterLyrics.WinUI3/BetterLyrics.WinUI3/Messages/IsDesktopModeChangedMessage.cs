using CommunityToolkit.Mvvm.Messaging.Messages;

namespace BetterLyrics.WinUI3.Messages
{
    public class IsDesktopModeChangedMessage(bool value) : ValueChangedMessage<bool>(value) { }
}
