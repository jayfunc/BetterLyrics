using CommunityToolkit.Mvvm.Messaging.Messages;

namespace BetterLyrics.Core.Messages.SettingsNavigation;

public sealed class AppSettingsNavigationRequestedMessage(Enums.AppSettingsSection value) : ValueChangedMessage<Enums.AppSettingsSection>(value);