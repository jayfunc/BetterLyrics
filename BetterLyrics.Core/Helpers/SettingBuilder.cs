using BetterLyrics.Core.Interfaces;
using BetterLyrics.Core.Models.SettingsSchema;
using System.Linq.Expressions;

namespace BetterLyrics.Core.Helpers
{
    public static class SettingBuilder
    {
        private static string GetKeyName<T>(Expression<Func<T>> expression)
        {
            MemberExpression? member = expression.Body as MemberExpression;

            if (member == null && expression.Body is UnaryExpression unary)
            {
                member = unary.Operand as MemberExpression;
            }

            if (member == null)
                throw new ArgumentException("Expression must be a member access (e.g. () => Config.Prop)");

            return member.Member.Name;
        }

        public static BoolSettingDef Bool<T>(Expression<Func<T>> keySelector, ILocalizer loc, bool defaultValue = false)
        {
            string key = GetKeyName(keySelector);
            return new BoolSettingDef
            {
                Key = key,
                Label = loc[$"Settings.{key}.Label"],
                Description = loc[$"Settings.{key}.Desc"],
                DefaultValue = defaultValue
            };
        }

        public static TextSettingDef Text<T>(Expression<Func<T>> keySelector, ILocalizer loc, string defaultValue = "")
        {
            string key = GetKeyName(keySelector);
            return new TextSettingDef
            {
                Key = key,
                Label = loc[$"Settings.{key}.Label"],
                Description = loc[$"Settings.{key}.Desc"],
                DefaultValue = defaultValue
            };
        }

        public static TextSettingDef Password<T>(Expression<Func<T>> keySelector, ILocalizer loc, string defaultValue = "")
        {
            string key = GetKeyName(keySelector);
            return new TextSettingDef
            {
                Key = key,
                Label = loc[$"Settings.{key}.Label"],
                Description = loc[$"Settings.{key}.Desc"],
                DefaultValue = defaultValue,
                IsPassword = true
            };
        }

        public static NumberSettingDef Number<T>(Expression<Func<T>> keySelector, ILocalizer loc, double defaultValue, double min = 0, double max = 100, double step = 1)
        {
            string key = GetKeyName(keySelector);
            return new NumberSettingDef
            {
                Key = key,
                Label = loc[$"Settings.{key}.Label"],
                Description = loc[$"Settings.{key}.Desc"],
                DefaultValue = defaultValue,
                Min = min,
                Max = max,
                Step = step
            };
        }

        public static ChoiceSettingDef Choice<T>(Expression<Func<T>> keySelector, ILocalizer loc, List<string> options, string defaultValue)
        {
            string key = GetKeyName(keySelector);
            return new ChoiceSettingDef
            {
                Key = key,
                Label = loc[$"Settings.{key}.Label"],
                Description = loc[$"Settings.{key}.Desc"],
                Options = options,
                DefaultValue = defaultValue
            };
        }

        public static ChoiceSettingDef EnumChoice<TEnum>(Expression<Func<TEnum>> keySelector, ILocalizer loc, TEnum defaultValue) where TEnum : struct, Enum
        {
            string key = GetKeyName(keySelector);
            return new ChoiceSettingDef
            {
                Key = key,
                Label = loc[$"Settings.{key}.Label"],
                Description = loc[$"Settings.{key}.Desc"],
                Options = Enum.GetNames(typeof(TEnum)).ToList(),
                DefaultValue = defaultValue.ToString()
            };
        }

        public static ActionSettingDef Action(string key, ILocalizer loc, Action<Dictionary<string, object>> action)
        {
            return new ActionSettingDef
            {
                Key = key,
                Label = loc[$"Settings.{key}.Label"],
                Description = loc[$"Settings.{key}.Desc"],
                ButtonText = loc[$"Settings.{key}.Button"],
                Action = action
            };
        }

    }
}
