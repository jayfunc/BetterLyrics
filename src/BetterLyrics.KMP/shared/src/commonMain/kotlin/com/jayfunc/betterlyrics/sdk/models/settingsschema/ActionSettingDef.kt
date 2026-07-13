package com.jayfunc.betterlyrics.sdk.models.settingsschema

class ActionSettingDef : SettingDef() {
    var buttonText: String = ""
    var action: (String) -> Unit = {}
}