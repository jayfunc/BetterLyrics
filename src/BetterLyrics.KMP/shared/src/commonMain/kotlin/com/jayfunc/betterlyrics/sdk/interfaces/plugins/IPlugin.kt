package com.jayfunc.betterlyrics.sdk.interfaces.plugins

import com.jayfunc.betterlyrics.sdk.models.settingsschema.SettingDef
import kotlinx.datetime.LocalDateTime

interface IPlugin {
    val title: String
    val description: String
    val author: String

    val id: String
    val version: String
    val lastUpdated: LocalDateTime

    val repositoryUrl: String

    suspend fun initializeAsync(context: IPluginContext)

    fun getSettingDefDict(): Map<String, SettingDef>

    // Equivalent to C#'s IAsyncDisposable.DisposeAsync()
    suspend fun disposeAsync()
}