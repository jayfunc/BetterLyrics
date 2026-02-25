using System.Text.RegularExpressions;

namespace Lyricify.Lyrics.Helpers.Optimization
{
    public static class Explicit
    {
        /// <summary>
        /// 处理字符串中的 Explicit 内容
        /// </summary>
        /// <param name="str">要处理的字符串</param>
        /// <param name="strong">是否是增强处理 (完全屏蔽为星号)</param>
        /// <returns>处理后的字符串</returns>
        public static string Clean(string str, bool strong = false)
        {
            if (strong)
            {
                str = FixExplicit(str)
                    .Replace("bitches", "*****")
                    .Replace("Bitches", "*****")
                    .Replace("bitch", "*****")
                    .Replace("Bitch", "*****")
                    .Replace("damn", "****")
                    .Replace("Damn", "****")
                    .Replace("dammit", "******")
                    .Replace("Dammit", "******")
                    .Replace("dick", "****")
                    .Replace("Dick", "****")
                    .Replace("dope", "****")
                    .Replace("Dope", "****")
                    .Replace("fuck", "****")
                    .Replace("Fuck", "****")
                    .Replace("nigga", "*****")
                    .Replace("Nigga", "*****")
                    .Replace("nigras", "******")
                    .Replace("Nigras", "******")
                    .Replace("pussy", "*****")
                    .Replace("Pussy", "*****")
                    .Replace("sex", "***")
                    .Replace("Sex", "***")
                    .Replace("shit", "****")
                    .Replace("Shit", "****")
                    .Replace("weed", "****")
                    .Replace("Weed", "****")
                    .Replace("whore", "*****")
                    .Replace("Whore", "*****")
                    .Replace("cocaine", "*******")
                    .Replace("Cocaine", "*******")
                    .Replace("drug", "****")
                    .Replace("Drug", "****");
                // Fix ass
                for (int i = 0; i < str.Length - 2; i++)
                {
                    if (str[i..(i + 3)] == "ass")
                    {
                        if (i > 0 && str[i - 1] != ' ' && str[i - 1] != '-')
                        {
                            i += 2;
                            continue;
                        }
                        else if (i + 3 < str.Length && str[i + 3] != ' ' && str[i + 3] != '-')
                        {
                            i += 2;
                            continue;
                        }
                        str = str.Remove(i, 3);
                        str = str.Insert(i, "***");
                        i += 2;
                    }
                }
                // Fix Ass
                for (int i = 0; i < str.Length - 2; i++)
                {
                    if (str[i..(i + 3)] == "Ass")
                    {
                        if (i > 0 && str[i - 1] != ' ' && str[i - 1] != '-')
                        {
                            i += 2;
                            continue;
                        }
                        else if (i + 3 < str.Length && str[i + 3] != ' ' && str[i + 3] != '-')
                        {
                            i += 2;
                            continue;
                        }
                        str = str.Remove(i, 3);
                        str = str.Insert(i, "***");
                        i += 2;
                    }
                }
                // Fix hoe
                for (int i = 0; i < str.Length - 2; i++)
                {
                    if (str[i..(i + 3)] == "hoe")
                    {
                        if (i > 0 && str[i - 1] != ' ' && str[i - 1] != '-')
                        {
                            i += 2;
                            continue;
                        }
                        else if (i + 3 < str.Length && str[i + 3] != ' ' && str[i + 3] != '-')
                        {
                            i += 2;
                            continue;
                        }
                        str = str.Remove(i, 3);
                        str = str.Insert(i, "***");
                        i += 2;
                    }
                }
            }
            else
            {
                str = str
                    .Replace("bitch", "b***h")
                    .Replace("Bitch", "B***h")
                    .Replace("damn", "d**n")
                    .Replace("Damn", "D**n")
                    .Replace("dammit", "D**mit")
                    .Replace("Dammit", "D**mit")
                    .Replace("dick", "d**k")
                    .Replace("Dick", "D**k")
                    .Replace("dope", "d**e")
                    .Replace("Dope", "D**e")
                    .Replace("fuck", "f**k")
                    .Replace("Fuck", "F**k")
                    .Replace("nigga", "n***a")
                    .Replace("Nigga", "N***a")
                    .Replace("nigras", "n***as")
                    .Replace("Nigras", "N***as")
                    .Replace("pussy", "p***y")
                    .Replace("Pussy", "P***y")
                    .Replace("sex", "s*x")
                    .Replace("Sex", "S*x")
                    .Replace("shit", "s**t")
                    .Replace("Shit", "S**t")
                    .Replace("weed", "w**d")
                    .Replace("Weed", "W**d")
                    .Replace("whore", "w***e")
                    .Replace("Whore", "W***e");
                // Fix ass
                for (int i = 0; i < str.Length - 2; i++)
                {
                    if (str[i..(i + 3)] == "ass")
                    {
                        if (i > 0 && str[i - 1] != ' ' && str[i - 1] != '-')
                        {
                            i += 2;
                            continue;
                        }
                        else if (i + 3 < str.Length && str[i + 3] != ' ' && str[i + 3] != '-')
                        {
                            i += 2;
                            continue;
                        }
                        str = str.Remove(i, 3);
                        str = str.Insert(i, "a*s");
                        i += 2;
                    }
                }
                // Fix Ass
                for (int i = 0; i < str.Length - 2; i++)
                {
                    if (str[i..(i + 3)] == "Ass")
                    {
                        if (i > 0 && str[i - 1] != ' ' && str[i - 1] != '-')
                        {
                            i += 2;
                            continue;
                        }
                        else if (i + 3 < str.Length && str[i + 3] != ' ' && str[i + 3] != '-')
                        {
                            i += 2;
                            continue;
                        }
                        str = str.Remove(i, 3);
                        str = str.Insert(i, "A*s");
                        i += 2;
                    }
                }
                // Fix hoe
                for (int i = 0; i < str.Length - 2; i++)
                {
                    if (str[i..(i + 3)] == "hoe")
                    {
                        if (i > 0 && str[i - 1] != ' ' && str[i - 1] != '-')
                        {
                            i += 2;
                            continue;
                        }
                        else if (i + 3 < str.Length && str[i + 3] != ' ' && str[i + 3] != '-')
                        {
                            i += 2;
                            continue;
                        }
                        str = str.Remove(i, 3);
                        str = str.Insert(i, "h*e");
                        i += 2;
                    }
                }
            }
            return str;
        }

        /// <summary>
        /// 修复字符串，恢复到 Explicit 状态
        /// </summary>
        /// <param name="str">要修复的字符串</param>
        /// <returns>修护后的字符串</returns>
        public static string FixExplicit(string str)
        {
            foreach (var (pattern, replacement) in Replacements)
            {
                str = Regex.Replace(str, pattern, match =>
                {
                    string original = match.Value;
                    return char.IsUpper(original[0]) ?
                        char.ToUpper(replacement[0]) + replacement[1..] : replacement;
                }, RegexOptions.IgnoreCase);
            }
            return str;
        }

        private static readonly IReadOnlyList<(string Pattern, string Replacement)> Replacements = new List<(string Pattern, string Replacement)>
        {
            ("a\\*s", "ass"), ("a\\*\\*", "ass"),
            ("b\\*{3}h", "bitch"), ("b\\*{2}ch", "bitch"),
            ("b\\*{5}s", "bitches"),
            ("d\\*{2}n", "damn"), ("d\\*{2}mit", "dammit"),
            ("d\\*{2}k", "dick"), ("d\\*{2}e", "dope"),
            ("f\\*{2}k", "fuck"), ("f\\*ck", "fuck"), ("fu\\*k", "fuck"),
            ("h\\*e", "hoe"), ("h\\*{2}s", "hoes"),
            ("mother\\*{4}", "motherfuck"),
            ("n\\*{3}a", "nigga"), ("n\\*{2}ga", "nigga"), ("ni\\*{2}a", "nigga"),
            ("n\\*{3}as", "nigras"),
            ("p\\*{3}y", "pussy"), ("p\\*{2}sy", "pussy"),
            ("sh\\*t", "shit"), ("s\\*x", "sex"), ("s\\*{2}t", "shit"),
            ("w\\*{2}d", "weed"), ("w\\*{3}e", "whore"),
            ("c\\*{4}e", "cocaine"), ("d\\*{2}g", "drug")
        };
    }
}
