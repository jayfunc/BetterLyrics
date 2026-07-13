package com.jayfunc.betterlyrics.services
import com.jayfunc.betterlyrics.interfaces.services.INavigationService
import com.jayfunc.betterlyrics.interfaces.providers.IWindowManagerProvider
class NavigationServiceImpl(private val windowManagerProvider: IWindowManagerProvider) : INavigationService {
    override fun openSettingsWindow() {}
    override fun openMusicGalleryWindow() {}
    override fun openLyricsWindowSwitchWindow() {}
    override fun openLyricsSearchWindow() {}
    override fun openLyricsShareWindow() {}
    override fun openStatsDashboardWindow() {}
}
