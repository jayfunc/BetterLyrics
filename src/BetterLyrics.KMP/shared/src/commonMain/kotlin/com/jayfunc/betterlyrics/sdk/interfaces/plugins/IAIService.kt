package com.jayfunc.betterlyrics.sdk.interfaces.plugins

interface IAIService {
    suspend fun chatAsync(systemPrompt: String, userPrompt: String): String
}
