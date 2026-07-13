package com.jayfunc.betterlyrics.services
import com.jayfunc.betterlyrics.interfaces.services.ILyricsSearchService
class LyricsSearchServiceImpl(val a: Any, val b: Any, val c: Any, val d: Any, val e: Any) : ILyricsSearchService {
    override suspend fun searchSmartlyAsync(songInfo: Any, token: Any): Any? = null
    override suspend fun searchAllAsync(songInfo: Any, token: Any): List<Any> = emptyList()
    override fun getActiveProviders(): List<Any> = emptyList()
    override suspend fun downloadAmllTtmlDbIndexAsync(token: Any) {}
}
