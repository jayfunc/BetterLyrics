using CommunityToolkit.Mvvm.Messaging.Messages;

namespace BetterLyrics.Core.Messages.AppSettings;

public sealed class WriteDebugLogsChangedMessage(bool value) : ValueChangedMessage<bool>(value);
