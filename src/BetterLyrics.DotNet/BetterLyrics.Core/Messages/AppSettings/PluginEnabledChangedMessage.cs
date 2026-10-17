using BetterLyrics.Core.Models.Settings;

namespace BetterLyrics.Core.Messages.AppSettings;

public sealed class PluginEnabledChangedMessage(PluginInfo pluginInfo, bool isEnabled)
{
    public PluginInfo PluginInfo { get; } = pluginInfo;
    public bool IsEnabled { get; } = isEnabled;
}

