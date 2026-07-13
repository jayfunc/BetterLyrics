package com.jayfunc.betterlyrics.interfaces.services
interface ILyricsSearchService {
    suspend fun searchSmartlyAsync(songInfo: Any, token: Any): Any?
    suspend fun searchAllAsync(songInfo: Any, token: Any): List<Any>
    fun getActiveProviders(): List<Any>
    suspend fun downloadAmllTtmlDbIndexAsync(token: Any)
}
