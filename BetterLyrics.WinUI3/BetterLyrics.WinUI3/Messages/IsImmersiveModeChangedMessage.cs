using CommunityToolkit.Mvvm.Messaging.Messages;

namespace BetterLyrics.WinUI3.Messages
{
    public class IsImmersiveModeChangedMessage(bool value) : ValueChangedMessage<bool>(value) { }
}
