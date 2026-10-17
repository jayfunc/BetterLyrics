namespace BetterLyrics.Core.Messages.AppSettings;

public sealed class TranslationSettingsChangedMessage(string propertyName)
{
    public string PropertyName { get; } = propertyName;
}

