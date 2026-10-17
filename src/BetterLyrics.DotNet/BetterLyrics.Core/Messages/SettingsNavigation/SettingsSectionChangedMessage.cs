namespace BetterLyrics.Core.Messages.SettingsNavigation;

public sealed class SettingsSectionChangedMessage(object sender)
{
    public object Sender { get; } = sender;
}

