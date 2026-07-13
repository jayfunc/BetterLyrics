package com.jayfunc.betterlyrics.models.settings

class AppSettings {
    var version: String = ""
    var translationSettings: TranslationSettings? = null
    var generalSettings: GeneralSettings? = null
    var musicGallerySettings: MusicGallerySettings? = null
    var advancedSettings: AdvancedSettings? = null
    var lyricsSaveConfig: Any? = null
    var systemTraySettings: SystemTraySettings? = null
    var localMediaFolders: List<Any> = emptyList()
    var mediaSourceProvidersInfo: List<Any> = emptyList()
    var mappedSongSearchQueries: List<Any> = emptyList()
    var windowBoundsRecords: List<Any> = emptyList()
    var starredPlaylists: List<Any> = emptyList()
    var pluginsInfo: List<Any> = emptyList()
    var lyricsCardConfigs: List<Any> = emptyList()
    var layoutProfiles: List<Any> = emptyList()
}
