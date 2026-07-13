package com.jayfunc.betterlyrics.sdk.interfaces.plugins

import com.jayfunc.betterlyrics.sdk.enums.ConfigChangedBy

interface IConfigurator {
    fun get(key: String, defaultValue: Any?): Any?

    fun set(key: String, value: Any?, configChangedBy: ConfigChangedBy)

    // Translating C# 'event' to standard Kotlin listener registration
    fun addOnConfigChangedListener(listener: (String, ConfigChangedBy) -> Unit)

    fun removeOnConfigChangedListener(listener: (String, ConfigChangedBy) -> Unit)
}
