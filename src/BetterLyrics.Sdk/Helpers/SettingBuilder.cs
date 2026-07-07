using System.Reflection;
using BetterLyrics.Sdk.Interfaces.Plugins;
using BetterLyrics.Sdk.Models.SettingsSchema;

namespace BetterLyrics.Sdk.Helpers;

public static class SettingBuilder
{
    public static BoolSettingDef Bool(PropertyInfo propertyInfo, ILocalizer loc, bool defaultValue = false)
    {
        var key = propertyInfo.Name;
        return new BoolSettingDef
        {
            Key = key,
            Header = loc[$"Settings.{key}.Label"],
            Description = loc[$"Settings.{key}.Desc"],
            Value = defaultValue
        };
    }

    public static TextSettingDef Text(PropertyInfo propertyInfo, ILocalizer loc, string defaultValue = "")
    {
        var key = propertyInfo.Name;
        return new TextSettingDef
        {
            Key = key,
            Header = loc[$"Settings.{key}.Label"],
            Description = loc[$"Settings.{key}.Desc"],
            Value = defaultValue
        };
    }

    public static TextSettingDef Password(PropertyInfo propertyInfo, ILocalizer loc, string defaultValue = "")
    {
        var key = propertyInfo.Name;
        return new TextSettingDef
        {
            Key = key,
            Header = loc[$"Settings.{key}.Label"],
            Description = loc[$"Settings.{key}.Desc"],
            Value = defaultValue,
            IsPassword = true
        };
    }

    public static NumberSettingDef Number(PropertyInfo propertyInfo, ILocalizer loc, double defaultValue,
        double min = 0, double max = 100, double step = 1)
    {
        var key = propertyInfo.Name;
        return new NumberSettingDef
        {
            Key = key,
            Header = loc[$"Settings.{key}.Label"],
            Description = loc[$"Settings.{key}.Desc"],
            Value = defaultValue,
            Min = min,
            Max = max,
            Step = step
        };
    }

    public static ChoiceSettingDef Choice(PropertyInfo propertyInfo, ILocalizer loc, List<string> options,
        string defaultValue)
    {
        var key = propertyInfo.Name;
        return new ChoiceSettingDef
        {
            Key = key,
            Header = loc[$"Settings.{key}.Label"],
            Description = loc[$"Settings.{key}.Desc"],
            Options = options,
            Value = defaultValue
        };
    }

    public static ActionSettingDef Action(PropertyInfo propertyInfo, ILocalizer loc, Action<string> action)
    {
        var key = propertyInfo.Name;
        return new ActionSettingDef
        {
            Key = key,
            Header = loc[$"Settings.{key}.Label"],
            Description = loc[$"Settings.{key}.Desc"],
            ButtonText = loc[$"Settings.{key}.Button"],
            Action = action
        };
    }
}