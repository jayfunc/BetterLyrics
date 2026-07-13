package com.jayfunc.betterlyrics.sdk.models.settingsschema

class NumberSettingDef : SettingDef() {
    var min: Double = 0.0
    var max: Double = 100.0
    var step: Double = 1.0
}