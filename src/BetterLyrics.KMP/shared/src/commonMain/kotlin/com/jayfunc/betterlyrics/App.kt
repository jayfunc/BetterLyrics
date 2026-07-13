package com.jayfunc.betterlyrics

import androidx.compose.runtime.Composable
import androidx.compose.ui.tooling.preview.Preview
import com.jayfunc.betterlyrics.controls.AboutControl
import com.jayfunc.betterlyrics.viewmodels.AboutControlViewModel
import io.github.composefluent.FluentTheme
import org.koin.dsl.module
import org.koin.plugin.module.dsl.single

val appModule = module {
    single<AboutControlViewModel>()
}

@Composable
@Preview
fun App() {

    FluentTheme {
        AboutControl()
    }
}