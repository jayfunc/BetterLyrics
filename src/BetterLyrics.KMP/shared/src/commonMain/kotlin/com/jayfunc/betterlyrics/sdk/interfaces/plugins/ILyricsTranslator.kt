package com.jayfunc.betterlyrics.sdk.interfaces.plugins

interface ILyricsTranslator {
    suspend fun getTranslationAsync(text: String, targetLanguageCode: String): String?
}
