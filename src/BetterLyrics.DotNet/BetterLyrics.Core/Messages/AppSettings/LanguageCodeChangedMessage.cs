using CommunityToolkit.Mvvm.Messaging.Messages;

namespace BetterLyrics.Core.Messages.AppSettings;

public sealed class LanguageCodeChangedMessage(string value) : ValueChangedMessage<string>(value);

