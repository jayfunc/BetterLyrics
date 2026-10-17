using CommunityToolkit.Mvvm.Messaging.Messages;

namespace BetterLyrics.Core.Messages.AppSettings;

public sealed class EnhanceControlInteractiveAnimationsChangedMessage(bool value) : ValueChangedMessage<bool>(value);

