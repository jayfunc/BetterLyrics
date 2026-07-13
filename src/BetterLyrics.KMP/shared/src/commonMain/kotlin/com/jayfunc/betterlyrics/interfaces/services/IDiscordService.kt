package com.jayfunc.betterlyrics.interfaces.services
interface IDiscordService {
    fun start()
    fun updateRichPresence(songInfo: Any)
    fun clearRichPresence()
}