package com.jayfunc.betterlyrics.interfaces.services
interface ILyricsCacheService {
    suspend fun initAsync()
    suspend fun getLyricsAsync(songInfo: Any, token: Any): Any?
    suspend fun saveLyricsAsync(songInfo: Any, result: Any, token: Any)
    suspend fun saveLyricsManualOverrideAsync(title: String, artist: String, album: String, raw: String, translation: String)
    suspend fun removeLyricsAsync(songInfo: Any)
    suspend fun clearAllLyricsAsync()
    suspend fun clearEmptyLyricsAsync()
}
