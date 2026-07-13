package com.jayfunc.betterlyrics.interfaces.services
interface IAlbumArtSearchService {
    suspend fun searchAsync(songInfo: Any, ignoreCache: Boolean = false, width: Int = 300, height: Int = 300): Any?
}