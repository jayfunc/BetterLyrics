package com.jayfunc.betterlyrics.sdk.models.settingsschema

abstract class SettingDef {
    var key: String = ""
    var header: String = ""
    var description: String = ""
    var value: Any? = null
}