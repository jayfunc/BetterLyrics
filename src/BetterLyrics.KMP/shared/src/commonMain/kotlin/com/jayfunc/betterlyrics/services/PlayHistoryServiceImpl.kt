package com.jayfunc.betterlyrics.services
import com.jayfunc.betterlyrics.interfaces.services.IPlayHistoryService


class PlayHistoryServiceImpl(logger: Any) : IPlayHistoryService {
    override suspend fun initAsync() = TODO()
    override suspend fun getLogsByDateRangeAsync(start: Any, end: Any): List<Any> = TODO()
    override suspend fun addPlayLogAsync(id: String, playerId: String, title: String, artist: String, album: String, startedAt: Any, totalDurationMs: Long, durationPlayedMs: Long) = TODO()
    override suspend fun getTopSongsAsync(start: Any, end: Any, limit: Int): List<Any> = TODO()
    override suspend fun getTopArtistsAsync(start: Any, end: Any, limit: Int): List<Any> = TODO()
    override suspend fun getTotalListeningDurationAsync(start: Any, end: Any): Any = TODO()
    override suspend fun getPlayerDistributionAsync(start: Any, end: Any): List<Any> = TODO()
    override suspend fun incrementPlayerPlayCountAsync(playerId: String, count: Int) = TODO()
}
