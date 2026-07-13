package com.jayfunc.betterlyrics.sdk.interfaces.plugins

import com.jayfunc.betterlyrics.sdk.models.lyrics.LyricsSearchResult

interface ILyricsSource {
    /**
     * @param title
     * @param artist
     * @param album
     * @param duration
     * @throws CancellationException if the coroutine is cancelled during the operation
     * @return LyricsSearchResult
     */
    suspend fun getLyricsAsync(
        title: String,
        artist: String,
        album: String,
        duration: Double
    ): LyricsSearchResult
}
