package com.jayfunc.betterlyrics.sdk.interfaces.plugins

interface IPluginContext {
    val pluginDirectory: String

    val aiService: IAIService?
    val localizer: ILocalizer

    /**
     * If you are modifying config on the plugin side, please use
     * [BetterLyrics.Sdk.Abstractions.Plugins.PluginBase.config]
     * directly.
     */
    val configurator: IConfigurator
}