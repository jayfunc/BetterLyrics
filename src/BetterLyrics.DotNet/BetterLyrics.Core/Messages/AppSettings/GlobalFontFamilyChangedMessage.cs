using CommunityToolkit.Mvvm.Messaging.Messages;

namespace BetterLyrics.Core.Messages.AppSettings;

public sealed class GlobalFontFamilyChangedMessage(string value) : ValueChangedMessage<string>(value);

