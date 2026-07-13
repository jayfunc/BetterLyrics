package com.jayfunc.betterlyrics.viewmodels

import androidx.lifecycle.ViewModel
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.getValue
import androidx.compose.runtime.setValue
import com.jayfunc.betterlyrics.interfaces.services.ISmtcService
import com.jayfunc.betterlyrics.interfaces.services.ISettingsService
import com.jayfunc.betterlyrics.models.settings.AppSettings

class PlayQueueViewModel(
    val smtcService: ISmtcService,
    private val settingsService: ISettingsService
) : ViewModel() {
    var appSettings: AppSettings? by mutableStateOf(settingsService.appSettings)
}
