using CommunityToolkit.Mvvm.Messaging.Messages;

namespace BetterLyrics.Core.Messages.AppSettings;

public sealed class ColorfulSystemTrayIconChangedMessage(bool value) : ValueChangedMessage<bool>(value);

