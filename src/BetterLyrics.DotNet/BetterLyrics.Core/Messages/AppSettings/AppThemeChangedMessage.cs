using BetterLyrics.Core.Enums;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace BetterLyrics.Core.Messages.AppSettings;

public sealed class AppThemeChangedMessage(AppTheme value) : ValueChangedMessage<AppTheme>(value);

