package com.jayfunc.betterlyrics.sdk.interfaces.plugins

interface ILocalizer {
    operator fun get(key: String): String
    val currentLanguage: String
    fun getString(key: String): String
}