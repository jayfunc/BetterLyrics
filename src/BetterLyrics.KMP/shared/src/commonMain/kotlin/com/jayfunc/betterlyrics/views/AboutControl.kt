package com.jayfunc.betterlyrics.views

import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import com.jayfunc.betterlyrics.viewmodels.AboutControlViewModel
import io.github.composefluent.component.Expander
import io.github.composefluent.component.Text
import org.koin.compose.viewmodel.koinViewModel

@Composable
fun AboutControl(
    viewModel: AboutControlViewModel = koinViewModel()
) {
    val uiState by viewModel.uiState.collectAsState()

    Expander(heading = { Text(uiState.) })
}