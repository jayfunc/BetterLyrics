package com.jayfunc.betterlyrics.sdk.helpers

import com.jayfunc.betterlyrics.sdk.interfaces.plugins.ILocalizer
import com.jayfunc.betterlyrics.sdk.models.settingsschema.ActionSettingDef
import com.jayfunc.betterlyrics.sdk.models.settingsschema.BoolSettingDef
import com.jayfunc.betterlyrics.sdk.models.settingsschema.ChoiceSettingDef
import com.jayfunc.betterlyrics.sdk.models.settingsschema.NumberSettingDef
import com.jayfunc.betterlyrics.sdk.models.settingsschema.TextSettingDef

object SettingBuilder {

    fun bool(propertyName: String, loc: ILocalizer, defaultValue: Boolean = false): BoolSettingDef {
        return BoolSettingDef().apply {
            key = propertyName
            header = loc["Settings.$propertyName.Label"]
            description = loc["Settings.$propertyName.Desc"]
            value = defaultValue
        }
    }

    fun text(propertyName: String, loc: ILocalizer, defaultValue: String = ""): TextSettingDef {
        return TextSettingDef().apply {
            key = propertyName
            header = loc["Settings.$propertyName.Label"]
            description = loc["Settings.$propertyName.Desc"]
            value = defaultValue
        }
    }

    fun password(propertyName: String, loc: ILocalizer, defaultValue: String = ""): TextSettingDef {
        return TextSettingDef().apply {
            key = propertyName
            header = loc["Settings.$propertyName.Label"]
            description = loc["Settings.$propertyName.Desc"]
            value = defaultValue
            isPassword = true
        }
    }

    // Note: Using Number instead of Double allows graceful handling of Int, Float,
    // and Double values without explicit casting from the caller.
    fun number(
        propertyName: String,
        loc: ILocalizer,
        defaultValue: Number,
        min: Number = 0.0,
        max: Number = 100.0,
        step: Number = 1.0
    ): NumberSettingDef {
        return NumberSettingDef().apply {
            key = propertyName
            header = loc["Settings.$propertyName.Label"]
            description = loc["Settings.$propertyName.Desc"]
            value = defaultValue.toDouble()
            this.min = min.toDouble()
            this.max = max.toDouble()
            this.step = step.toDouble()
        }
    }

    fun choice(
        propertyName: String,
        loc: ILocalizer,
        options: List<String>,
        defaultValue: String
    ): ChoiceSettingDef {
        return ChoiceSettingDef().apply {
            key = propertyName
            header = loc["Settings.$propertyName.Label"]
            description = loc["Settings.$propertyName.Desc"]
            this.options = options
            value = defaultValue
        }
    }

    // C# Action<string> translates to the Kotlin function type (String) -> Unit
    fun action(
        propertyName: String,
        loc: ILocalizer,
        action: (String) -> Unit
    ): ActionSettingDef {
        return ActionSettingDef().apply {
            key = propertyName
            header = loc["Settings.$propertyName.Label"]
            description = loc["Settings.$propertyName.Desc"]
            buttonText = loc["Settings.$propertyName.Button"]
            this.action = action
        }
    }
}