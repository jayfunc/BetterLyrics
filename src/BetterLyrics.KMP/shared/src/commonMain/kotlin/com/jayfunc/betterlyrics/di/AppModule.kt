package com.jayfunc.betterlyrics.di

import com.jayfunc.betterlyrics.interfaces.services.ILyricsCacheService
import com.jayfunc.betterlyrics.interfaces.services.IAppLifecycleService
import com.jayfunc.betterlyrics.interfaces.services.INavigationService
import com.jayfunc.betterlyrics.interfaces.services.ISettingsService
import com.jayfunc.betterlyrics.interfaces.services.IAlbumArtSearchService
import com.jayfunc.betterlyrics.interfaces.services.IDiscordService
import com.jayfunc.betterlyrics.services.NavigationServiceImpl
import com.jayfunc.betterlyrics.services.SettingsServiceImpl
import com.jayfunc.betterlyrics.services.LyricsCacheServiceImpl
import com.jayfunc.betterlyrics.services.AppLifecycleServiceImpl
import com.jayfunc.betterlyrics.services.AlbumArtSearchServiceImpl
import com.jayfunc.betterlyrics.services.DiscordServiceImpl
import com.jayfunc.betterlyrics.interfaces.services.ILyricsSearchService
import com.jayfunc.betterlyrics.interfaces.services.IPlayHistoryService
import com.jayfunc.betterlyrics.services.LyricsSearchServiceImpl
import com.jayfunc.betterlyrics.services.PlayHistoryServiceImpl
import org.koin.dsl.module

val appModule = module {
    single<ISettingsService> { SettingsServiceImpl(get(), get(), get()) }
    single<ILyricsCacheService> { LyricsCacheServiceImpl(get()) }
    single<INavigationService> { NavigationServiceImpl(get()) }
    single<IAppLifecycleService> { AppLifecycleServiceImpl(get()) }
    single<IAlbumArtSearchService> { AlbumArtSearchServiceImpl(get(), get()) }
    single<IDiscordService> { DiscordServiceImpl(get()) }
    single<ILyricsSearchService> { LyricsSearchServiceImpl(get(), get(), get(), get(), get()) }
    single<IPlayHistoryService> { PlayHistoryServiceImpl(get()) }
}
