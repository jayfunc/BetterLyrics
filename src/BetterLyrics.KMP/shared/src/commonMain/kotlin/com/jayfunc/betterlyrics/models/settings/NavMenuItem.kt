package com.jayfunc.betterlyrics.models.settings

import com.jayfunc.betterlyrics.enums.SettingsSection

data class NavMenuItem(
    var label: String,
    var glyph: String,
    var section: SettingsSection
)
