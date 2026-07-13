package com.jayfunc.betterlyrics.sdk.interfaces.plugins

interface ILyricsTransliterator {
    suspend fun getTransliterationAsync(text: String, targetLanguageCode: String): String?
}
