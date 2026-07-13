package com.jayfunc.betterlyrics.viewmodels

import androidx.lifecycle.ViewModel
import com.jayfunc.betterlyrics.models.Contributor
import com.jayfunc.betterlyrics.models.Donor
import com.jayfunc.betterlyrics.models.settings.AppSettings
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow

data class AboutControlUiState(
    val counter: Number = 0
)

class AboutControlViewModel() : ViewModel() {
    val uiState: StateFlow<AboutControlUiState>
        field = MutableStateFlow(AboutControlUiState())

    fun add(){
        uiState.apply { counter }
    }
}
