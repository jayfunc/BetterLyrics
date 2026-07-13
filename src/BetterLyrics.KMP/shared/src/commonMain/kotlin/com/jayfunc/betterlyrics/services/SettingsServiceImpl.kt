package com.jayfunc.betterlyrics.services
import com.jayfunc.betterlyrics.interfaces.services.ISettingsService
import com.jayfunc.betterlyrics.models.settings.AppSettings
import com.jayfunc.betterlyrics.models.settings.LyricsSaveConfig
class SettingsServiceImpl(a: Any, b: Any, c: Any) : ISettingsService {
    override var appSettings: AppSettings? = null
    override var lyricsSaveConfig: LyricsSaveConfig? = null
    override fun loadSettings() {}
    override fun saveSettings() {}
    override fun exportSettings(path: String) {}
    override fun importSettings(path: String): Boolean = true
    override fun resetSettings() {}
}
