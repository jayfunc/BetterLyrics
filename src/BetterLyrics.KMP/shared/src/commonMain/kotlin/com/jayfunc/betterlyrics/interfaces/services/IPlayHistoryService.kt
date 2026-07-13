package com.jayfunc.betterlyrics.interfaces.services

interface IPlayHistoryService {
    suspend fun initAsync()
    suspend fun getLogsByDateRangeAsync(start: Any, end: Any): List<Any>
    suspend fun addPlayLogAsync(id: String, playerId: String, title: String, artist: String, album: String, startedAt: Any, totalDurationMs: Long, durationPlayedMs: Long)
    suspend fun getTopSongsAsync(start: Any, end: Any, limit: Int = 10): List<Any>
    suspend fun getTopArtistsAsync(start: Any, end: Any, limit: Int = 10): List<Any>
    suspend fun getTotalListeningDurationAsync(start: Any, end: Any): Any
    suspend fun getPlayerDistributionAsync(start: Any, end: Any): List<Any>
    suspend fun incrementPlayerPlayCountAsync(playerId: String, count: Int = 1)
}
