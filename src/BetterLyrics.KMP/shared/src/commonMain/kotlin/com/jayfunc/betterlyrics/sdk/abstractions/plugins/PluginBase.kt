package com.jayfunc.betterlyrics.sdk.abstractions.plugins

import com.jayfunc.betterlyrics.sdk.helpers.SettingBuilder
import com.jayfunc.betterlyrics.sdk.interfaces.plugins.IPlugin
import com.jayfunc.betterlyrics.sdk.interfaces.plugins.IPluginContext
import com.jayfunc.betterlyrics.sdk.models.settingsschema.SettingDef

import kotlinx.datetime.LocalDateTime

abstract class PluginBase<TConfig : PluginConfigBase>(
    val config: TConfig
) : IPlugin {

    private var _context: IPluginContext? = null
    private var _isDisposed: Boolean = false

    protected val context: IPluginContext
        get() = _context ?: throw IllegalStateException(
            "Plugin is not initialized yet! Do not access Context in the constructor."
        )

    // 1. Added 'override' to all interface members
    abstract override var title: String

    // 2. JVM package inspection removed. Child classes should override these if needed.
    override val description: String
        get() = ""

    override val author: String
        get() = "Unknown Author"

    override val id: String
        get() = this::class.simpleName ?: "UnknownPlugin"

    override val lastUpdated: LocalDateTime
        get() = LocalDateTime(1970, 1, 1, 0, 0, 0) // KMP-safe default date

    override val version: String
        get() = "0.0.0"

    override val repositoryUrl: String
        get() = ""

    override suspend fun initializeAsync(pluginContext: IPluginContext) {
        _context = pluginContext

        // Note: Ensure your PluginConfigBase class actually has a 'bindConfigurator' method
        config.bindConfigurator(context.configurator)

        onInitializeAsync()
    }

    override suspend fun disposeAsync() {
        if (_isDisposed) return

        onShutdownAsync()
        _context = null
        _isDisposed = true
    }

    override fun getSettingDefDict(): Map<String, SettingDef> {
        // 3. Reflection Removed.
        // Kotlin Multiplatform does not support iterating over properties at runtime natively.
        // It is recommended to either:
        // A) Override this method in your specific Plugin classes and build the Map manually.
        // B) Have `PluginConfigBase` implement a function that explicitly returns its settings.
        return emptyMap()
    }

    protected open suspend fun onInitializeAsync() {
        // Default empty implementation
    }

    protected open suspend fun onShutdownAsync() {
        // Default empty implementation
    }
}