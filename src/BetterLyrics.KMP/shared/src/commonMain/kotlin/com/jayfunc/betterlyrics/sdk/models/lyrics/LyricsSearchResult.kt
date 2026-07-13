package com.jayfunc.betterlyrics.sdk.models.lyrics

data class LyricsSearchResult(
    val title: String?,
    val artist: String?,
    val album: String?,
    val duration: Double?,
    val raw: String?,
    val translation: String? = null,
    val transliteration: String? = null,
    val reference: String? = null
)