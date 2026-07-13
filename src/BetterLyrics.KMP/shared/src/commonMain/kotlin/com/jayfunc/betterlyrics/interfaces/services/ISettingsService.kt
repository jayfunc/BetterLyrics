package com.jayfunc.betterlyrics.interfaces.services
import com.jayfunc.betterlyrics.models.settings.AppSettings
import com.jayfunc.betterlyrics.models.settings.LyricsSaveConfig
interface ISettingsService {
    var appSettings: AppSettings?
    var lyricsSaveConfig: LyricsSaveConfig?
    fun loadSettings()
    fun saveSettings()
    fun exportSettings(path: String)
    fun importSettings(path: String): Boolean
    fun resetSettings()
}
