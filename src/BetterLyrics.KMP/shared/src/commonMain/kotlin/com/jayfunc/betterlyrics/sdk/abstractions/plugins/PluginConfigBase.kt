package com.jayfunc.betterlyrics.sdk.abstractions.plugins

import com.jayfunc.betterlyrics.sdk.enums.ConfigChangedBy
import com.jayfunc.betterlyrics.sdk.interfaces.plugins.IConfigurator
import kotlin.properties.ReadWriteProperty
import kotlin.reflect.KProperty

abstract class PluginConfigBase {

    protected var _configurator: IConfigurator? = null

    fun bindConfigurator(configurator: IConfigurator) {
        _configurator = configurator
    }

    /**
     * Direct equivalent to C# Get<T>, but because Kotlin lacks [CallerMemberName],
     * you must explicitly pass the 'key' string if calling this directly.
     * We use `inline` and `reified` to safely check generic types at runtime.
     */
    protected inline fun <reified T> get(key: String, defaultValue: T): T {
        val configurator = _configurator ?: return defaultValue

        return try {
            val rawValue = configurator.get(key, defaultValue)
            // as? is a safe cast. If it fails, it returns null, triggering the ?: fallback.
            rawValue as? T ?: defaultValue
        } catch (e: Exception) {
            defaultValue
        }
    }

    /**
     * Direct equivalent to C# Set<T>. Requires explicit 'key'.
     */
    protected fun <T> set(key: String, value: T) {
        _configurator?.set(key, value, ConfigChangedBy.Plugin) // Assuming ConfigChangedBy.Plugin exists
    }

    /**
     * IDIOMATIC KOTLIN ALTERNATIVE TO [CallerMemberName]: Property Delegates
     * * By returning a ReadWriteProperty, Kotlin automatically injects the `KProperty`
     * metadata, allowing us to read `property.name` under the hood!
     */
    protected inline fun <reified T> configProperty(defaultValue: T): ReadWriteProperty<PluginConfigBase, T> {
        return object : ReadWriteProperty<PluginConfigBase, T> {
            override fun getValue(thisRef: PluginConfigBase, property: KProperty<*>): T {
                return thisRef.get(property.name, defaultValue)
            }

            override fun setValue(thisRef: PluginConfigBase, property: KProperty<*>, value: T) {
                thisRef.set(property.name, value)
            }
        }
    }
}