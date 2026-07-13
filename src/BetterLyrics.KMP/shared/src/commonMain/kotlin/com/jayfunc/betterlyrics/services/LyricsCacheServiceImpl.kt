package com.jayfunc.betterlyrics.services
import com.jayfunc.betterlyrics.interfaces.services.ILyricsCacheService
class LyricsCacheServiceImpl(private val x: Any) : ILyricsCacheService {
    override suspend fun initAsync() {}
    override suspend fun getLyricsAsync(songInfo: Any, token: Any): Any? = null
    override suspend fun saveLyricsAsync(songInfo: Any, result: Any, token: Any) {}
    override suspend fun saveLyricsManualOverrideAsync(title: String, artist: String, album: String, raw: String, translation: String) {}
    override suspend fun removeLyricsAsync(songInfo: Any) {}
    override suspend fun clearAllLyricsAsync() {}
    override suspend fun clearEmptyLyricsAsync() {}
}
