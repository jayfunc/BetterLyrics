package com.jayfunc.betterlyrics

interface Platform {
    val name: String
}

expect fun getPlatform(): Platform