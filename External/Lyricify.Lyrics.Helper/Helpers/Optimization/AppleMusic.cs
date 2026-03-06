using Lyricify.Lyrics.Models;
using System.Text;

namespace Lyricify.Lyrics.Helpers.Optimization
{
    public static class AppleMusic
    {
        #region Prepare Lyrics

        /// <summary>
        /// 预处理歌词
        /// </summary>
        public static void PrepareLyrics(List<ILineInfo> list)
        {
            if (list == null || list.Count == 0) return;

            foreach (var line in list)
            {
                PrepareLyrics(line);
            }
        }

        public static void PrepareLyrics(ILineInfo line)
        {
            if (line == null) return;

            // 主行
            if (line is SyllableLineInfo syllableLine)
                PrepareLyrics(syllableLine);

            // SubLine（背景人声）
            var sub = line.SubLine;
            if (sub is SyllableLineInfo subSyllableLine)
                PrepareLyrics(subSyllableLine);

            // 清理冗余翻译：翻译与原文完全一致 -> 移除该翻译项
            RemoveRedundantTranslations(line, isBackground: false);
            if (sub != null)
                RemoveRedundantTranslations(sub, isBackground: true);
        }

        private static void PrepareLyrics(SyllableLineInfo line)
        {
            if (line?.Syllables == null || line.Syllables.Count == 0) return;

            TrimBoundaryWhitespaces(line);

            // Step 1: expand/split within a syllable
            var expanded = new List<ISyllableInfo>(line.Syllables.Count * 2);

            foreach (var s in line.Syllables)
            {
                var text = NormalizeText(s.Text ?? string.Empty);

                if (string.IsNullOrEmpty(text))
                {
                    expanded.Add(new SyllableInfo(string.Empty, s.StartTime, s.EndTime));
                    continue;
                }

                if (ShouldSplitToken(text))
                {
                    foreach (var part in SplitIntoTokens(text, s.StartTime, s.EndTime))
                        expanded.Add(part);
                }
                else
                {
                    expanded.Add(new SyllableInfo(text, s.StartTime, s.EndTime));
                }
            }

            // Step 2: merge consecutive syllables into one word via FullSyllableInfo.SubItems
            var merged = new List<ISyllableInfo>(expanded.Count);

            foreach (var cur in expanded)
            {
                if (merged.Count == 0)
                {
                    merged.Add(cur);
                    continue;
                }

                var prev = merged[^1];

                if (ShouldMergeAsSameWord(prev.Text, cur.Text))
                {
                    merged[^1] = MergeToFullSyllable(prev, cur);
                }
                else
                {
                    merged.Add(cur);
                }
            }

            line.Syllables = merged;
            line.RefreshProperties();
        }

        private static void RemoveRedundantTranslations(ILineInfo line, bool isBackground)
        {
            // 只有 FullLineInfo / FullSyllableLineInfo 才有 Translations
            IDictionary<string, string>? translations = null;

            if (line is FullLineInfo fli) translations = fli.Translations;
            else if (line is FullSyllableLineInfo fsli) translations = fsli.Translations;

            if (translations == null || translations.Count == 0) return;

            var original = line.Text ?? string.Empty;
            var originalNorm = NormalizeForCompare(original, isBackground);

            // 遍历时不能直接修改字典，先收集要删的 key
            var toRemove = new List<string>();

            foreach (var kv in translations)
            {
                var transNorm = NormalizeForCompare(kv.Value ?? string.Empty, isBackground);

                // “完全一致”用 Ordinal 比较；你要忽略大小写可改成 OrdinalIgnoreCase
                if (string.Equals(originalNorm, transNorm, StringComparison.Ordinal))
                {
                    toRemove.Add(kv.Key);
                }
            }

            foreach (var key in toRemove)
                translations.Remove(key);
        }

        /// <summary>
        /// 统一比较用文本：
        /// - Trim + 折叠空白
        /// - 背景人声额外：去掉外层括号（半角/全角），不要求必须整句都是括号，但会优先处理“首尾括号包裹”的情况
        /// </summary>
        private static string NormalizeForCompare(string s, bool isBackground)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;

            s = s.Replace("\r", "").Replace("\n", "");
            s = CollapseSpaces(s.Trim());

            if (!isBackground) return s;

            // 背景：去掉外层括号（支持 () 与 （））
            s = StripOuterBracketsIfWrapped(s);

            // 再做一次空白归一化
            s = CollapseSpaces(s.Trim());
            return s;
        }

        private static string CollapseSpaces(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;

            // 折叠所有空白为单个空格
            var sb = new StringBuilder(s.Length);
            bool prevWs = false;

            foreach (var ch in s)
            {
                if (char.IsWhiteSpace(ch))
                {
                    if (!prevWs)
                    {
                        sb.Append(' ');
                        prevWs = true;
                    }
                }
                else
                {
                    sb.Append(ch);
                    prevWs = false;
                }
            }

            return sb.ToString().Trim();
        }

        private static string StripOuterBracketsIfWrapped(string s)
        {
            if (s.Length < 2) return s;

            // 优先处理整句被括号包裹的最常见形式：(xxx) / （xxx）
            bool half = s[0] == '(' && s[^1] == ')';
            bool full = s[0] == '（' && s[^1] == '）';

            if (half || full)
            {
                return s[1..^1].Trim();
            }

            // 若不是整句包裹，就不强行剥离（避免误伤如：hello (yeah)）
            return s;
        }

        private static void TrimBoundaryWhitespaces(SyllableLineInfo line)
        {
            var syllables = line.Syllables;
            if (syllables.Count == 0) return;

            // ---- TrimStart on first syllable ----
            syllables[0] = TrimStartSafe(syllables[0]);

            // ---- TrimEnd on last syllable ----
            int last = syllables.Count - 1;
            syllables[last] = TrimEndSafe(syllables[last]);
        }

        private static ISyllableInfo TrimStartSafe(ISyllableInfo s)
        {
            if (s is SyllableInfo si)
            {
                var text = si.Text?.TrimStart() ?? string.Empty;
                if (text == si.Text) return s;

                return new SyllableInfo(text, si.StartTime, si.EndTime);
            }

            if (s is FullSyllableInfo fi && fi.SubItems.Count > 0)
            {
                var first = fi.SubItems[0];
                var trimmed = first.Text?.TrimStart() ?? string.Empty;

                if (trimmed != first.Text)
                {
                    fi.SubItems[0] = new SyllableInfo(trimmed, first.StartTime, first.EndTime);
                    fi.RefreshProperties();
                }
                return fi;
            }

            return s;
        }

        private static ISyllableInfo TrimEndSafe(ISyllableInfo s)
        {
            if (s is SyllableInfo si)
            {
                var text = si.Text?.TrimEnd() ?? string.Empty;
                if (text == si.Text) return s;

                return new SyllableInfo(text, si.StartTime, si.EndTime);
            }

            if (s is FullSyllableInfo fi && fi.SubItems.Count > 0)
            {
                int last = fi.SubItems.Count - 1;
                var item = fi.SubItems[last];
                var trimmed = item.Text?.TrimEnd() ?? string.Empty;

                if (trimmed != item.Text)
                {
                    fi.SubItems[last] = new SyllableInfo(trimmed, item.StartTime, item.EndTime);
                    fi.RefreshProperties();
                }
                return fi;
            }

            return s;
        }

        // =========================
        // Split rules
        // =========================

        private static bool ShouldSplitToken(string text)
        {
            // 触发拆分：
            // - 中文/日文可拆字符 >= 2（逐字拆）
            // - 或 中文/日文 + 拉丁混合
            // - 或 拉丁文本里出现空格 / 连字符分段
            int zhja = 0;
            bool hasLatin = false;
            bool hasSpace = false;
            bool hasHyphen = false;

            foreach (var ch in text)
            {
                if (ch == ' ') hasSpace = true;
                if (ch == '-') hasHyphen = true;

                if (IsZhJaSplitChar(ch)) zhja++;
                else if (IsLatinWordChar(ch)) hasLatin = true;
            }

            if (zhja >= 2) return true;
            if (zhja >= 1 && hasLatin) return true;
            if (hasLatin && (hasSpace || hasHyphen)) return true;

            return false;
        }

        private static IEnumerable<SyllableInfo> SplitIntoTokens(string text, int startTime, int endTime)
        {
            var tokens = TokenizeMixed(text);

            if (tokens.Count <= 1)
            {
                yield return new SyllableInfo(text, startTime, endTime);
                yield break;
            }

            // 加权分配时间：含中文/日文 token 权重=1；拉丁 token 权重=字母数字数(>=1)
            var weights = tokens.Select(TokenWeight).ToList();
            int wSum = weights.Sum();
            if (wSum <= 0) wSum = tokens.Count;

            int total = endTime - startTime;
            if (total <= 0)
            {
                foreach (var t in tokens)
                    yield return new SyllableInfo(t, startTime, endTime);
                yield break;
            }

            int allocated = 0;
            int curStart = startTime;

            for (int i = 0; i < tokens.Count; i++)
            {
                int dur;
                if (i == tokens.Count - 1)
                {
                    dur = total - allocated;
                }
                else
                {
                    dur = (int)Math.Round(total * (weights[i] / (double)wSum), MidpointRounding.AwayFromZero);
                    if (dur < 1) dur = 1;

                    // 给剩余 token 留至少 1ms
                    int minRemain = tokens.Count - 1 - i;
                    if (allocated + dur > total - minRemain)
                        dur = total - allocated - minRemain;
                }

                int curEnd = curStart + dur;
                allocated += dur;

                yield return new SyllableInfo(tokens[i], curStart, curEnd);
                curStart = curEnd;
            }
        }

        /// <summary>
        /// Tokenize with "separators belong to previous token":
        /// - space ' ' appended to previous token and ends token => "word "
        /// - hyphen '-' appended to previous token and ends token => "Ooh-"
        /// - Zh/Ja split char => each char as token
        /// - Latin word => grouped
        /// - Others (including Hangul) => attach to current token if exists, else attach to previous token if any
        /// </summary>
        private static List<string> TokenizeMixed(string text)
        {
            var tokens = new List<string>();
            var sb = new StringBuilder();

            void Flush()
            {
                if (sb.Length > 0)
                {
                    tokens.Add(sb.ToString());
                    sb.Clear();
                }
            }

            int i = 0;
            while (i < text.Length)
            {
                char ch = text[i];

                if (ch == '\r' || ch == '\n') { i++; continue; }

                // Zh/Ja: per char token
                if (IsZhJaSplitChar(ch))
                {
                    Flush();
                    tokens.Add(ch.ToString());
                    i++;
                    continue;
                }

                // space -> belongs to previous token end
                if (ch == ' ')
                {
                    if (sb.Length > 0)
                    {
                        sb.Append(' ');
                        Flush();
                    }
                    else if (tokens.Count > 0)
                    {
                        tokens[^1] += " ";
                    }
                    else
                    {
                        sb.Append(' ');
                    }
                    i++;
                    continue;
                }

                // hyphen -> belongs to previous token end, and boundary
                if (ch == '-')
                {
                    if (sb.Length > 0)
                    {
                        sb.Append('-');
                        Flush();
                    }
                    else if (tokens.Count > 0)
                    {
                        tokens[^1] += "-";
                    }
                    else
                    {
                        sb.Append('-');
                    }
                    i++;
                    continue;
                }

                // Latin word chunk
                if (IsLatinWordChar(ch))
                {
                    sb.Append(ch);
                    i++;

                    while (i < text.Length)
                    {
                        char c2 = text[i];
                        if (c2 == '\r' || c2 == '\n') { i++; continue; }
                        if (IsLatinWordChar(c2))
                        {
                            sb.Append(c2);
                            i++;
                            continue;
                        }
                        break;
                    }
                    continue;
                }

                // other chars (punctuation / Hangul / emoji ...)
                if (sb.Length > 0)
                {
                    sb.Append(ch);
                }
                else if (tokens.Count > 0)
                {
                    // prefer attach to previous token (separator-to-previous style)
                    tokens[^1] += ch;
                }
                else
                {
                    sb.Append(ch);
                }

                i++;
            }

            Flush();

            return tokens.Where(t => t.Length > 0).ToList();
        }

        private static int TokenWeight(string token)
        {
            if (token.Any(IsZhJaSplitChar)) return 1;

            int w = 0;
            foreach (var ch in token)
            {
                if (char.IsLetterOrDigit(ch)) w++;
            }
            return Math.Max(1, w);
        }

        // =========================
        // Merge rules (FullSyllableInfo.SubItems)
        // =========================

        private static bool ShouldMergeAsSameWord(string? prevText, string? curText)
        {
            if (string.IsNullOrEmpty(prevText) || string.IsNullOrEmpty(curText)) return false;

            // don't merge across whitespace boundary
            if (char.IsWhiteSpace(prevText[^1])) return false;
            if (char.IsWhiteSpace(curText[0])) return false;

            // don't merge if contains zh/ja split chars (avoid merging back)
            if (prevText.Any(IsZhJaSplitChar) || curText.Any(IsZhJaSplitChar)) return false;

            // both should look like word parts
            return LooksLikeWordPart(prevText) && LooksLikeWordPart(curText);
        }

        private static bool LooksLikeWordPart(string text)
        {
            foreach (var ch in text)
            {
                if (char.IsLetterOrDigit(ch)) return true;
            }
            return false;
        }

        private static ISyllableInfo MergeToFullSyllable(ISyllableInfo prev, ISyllableInfo cur)
        {
            // flatten both into SyllableInfo list
            var prevItems = FlattenToSyllableInfos(prev);
            var curItems = FlattenToSyllableInfos(cur);

            if (prev is FullSyllableInfo fullPrev)
            {
                // mutate existing object: append and refresh cached properties
                fullPrev.SubItems.AddRange(curItems);
                fullPrev.RefreshProperties();
                return fullPrev;
            }

            // create new FullSyllableInfo from two lists
            var all = new List<SyllableInfo>(prevItems.Count + curItems.Count);
            all.AddRange(prevItems);
            all.AddRange(curItems);

            var full = new FullSyllableInfo(all);
            // FullSyllableInfo has cached computed props, but newly created is fine;
            // no need to set Text/Start/End (readonly).
            return full;
        }

        private static List<SyllableInfo> FlattenToSyllableInfos(ISyllableInfo s)
        {
            if (s is SyllableInfo si)
            {
                return new List<SyllableInfo> { new(NormalizeText(si.Text), si.StartTime, si.EndTime) };
            }

            if (s is FullSyllableInfo fi)
            {
                // copy to new instances to keep everything writable/isolated
                return fi.SubItems
                    .Select(x => new SyllableInfo(NormalizeText(x.Text), x.StartTime, x.EndTime))
                    .ToList();
            }

            // fallback for other implementations
            return new List<SyllableInfo>
            {
                new(NormalizeText(s.Text ?? string.Empty), s.StartTime, s.EndTime)
            };
        }

        // =========================
        // Char classification
        // =========================

        /// <summary>
        /// only split Chinese Hanzi + Japanese Kana. Korean Hangul is NOT split.
        /// </summary>
        private static bool IsZhJaSplitChar(char ch)
        {
            // Hanzi
            if (ch >= '\u4E00' && ch <= '\u9FFF') return true;
            if (ch >= '\u3400' && ch <= '\u4DBF') return true;

            // Kana
            if (ch >= '\u3040' && ch <= '\u309F') return true; // Hiragana
            if (ch >= '\u30A0' && ch <= '\u30FF') return true; // Katakana
            if (ch >= '\u31F0' && ch <= '\u31FF') return true; // Katakana Extensions

            return false;
        }

        private static bool IsLatinWordChar(char ch)
        {
            if (char.IsLetterOrDigit(ch)) return true;
            return ch == '\'' || ch == '’';
        }

        private static string NormalizeText(string text)
            => text.Replace("\r", "").Replace("\n", "");

        #endregion

        #region Capitalization Normalization

        public static void CapitalizationNormalization(List<ILineInfo> list)
        {
            if (list == null || list.Count == 0) return;

            // 1) line-by-line classification
            int lowerLines = 0;
            int upperLines = 0;
            int otherLines = 0; // mixed-case or no-letter lines

            void CountLine(string? text)
            {
                var kind = GetLineCaseKind(text);
                switch (kind)
                {
                    case LineCaseKind.AllLower:
                        lowerLines++;
                        break;
                    case LineCaseKind.AllUpper:
                        upperLines++;
                        break;
                    default:
                        otherLines++;
                        break;
                }
            }

            for (int i = 0; i < list.Count; i++)
            {
                var line = list[i];
                if (line == null) continue;

                CountLine(line.Text);

                if (line.SubLine != null)
                    CountLine(line.SubLine.Text);
            }

            int totalLines = lowerLines + upperLines + otherLines;
            if (totalLines == 0) return;

            double lowerRatio = (double)lowerLines / totalLines;
            double upperRatio = (double)upperLines / totalLines;

            const double Threshold = 0.9;

            bool doLowerCaseFirst;
            if (upperRatio >= Threshold)
                doLowerCaseFirst = true;          // mostly ALL-UPPER -> lower first
            else if (lowerRatio >= Threshold)
                doLowerCaseFirst = false;         // mostly ALL-lower -> sentence-cap only
            else
            {
                // Not “mostly uniform” -> do nothing (optional: still fix standalone "i")
                // If you want, uncomment the next loop to always fix standalone i:
                /*
                for (int i = 0; i < list.Count; i++)
                    list[i] = ApplyStandaloneIFix(list[i]);
                */
                return;
            }

            // 2) apply normalization to all lines (main + sub)
            for (int i = 0; i < list.Count; i++)
            {
                list[i] = ApplyCapitalization(list[i], doLowerCaseFirst);
            }
        }

        private enum LineCaseKind
        {
            NoLettersOrMixed = 0,
            AllLower = 1,
            AllUpper = 2
        }

        private static LineCaseKind GetLineCaseKind(string? s)
        {
            if (string.IsNullOrEmpty(s)) return LineCaseKind.NoLettersOrMixed;

            bool hasUpper = false;
            bool hasLower = false;
            bool hasLetter = false;

            for (int i = 0; i < s.Length; i++)
            {
                char ch = s[i];
                if (!char.IsLetter(ch)) continue;

                hasLetter = true;
                if (char.IsUpper(ch)) hasUpper = true;
                else if (char.IsLower(ch)) hasLower = true;

                if (hasUpper && hasLower) return LineCaseKind.NoLettersOrMixed;
            }

            if (!hasLetter) return LineCaseKind.NoLettersOrMixed;
            if (hasUpper && !hasLower) return LineCaseKind.AllUpper;
            if (hasLower && !hasUpper) return LineCaseKind.AllLower;

            return LineCaseKind.NoLettersOrMixed;
        }

        private static ILineInfo ApplyCapitalization(ILineInfo line, bool lowerFirst)
        {
            if (line == null) return line!;

            // main
            var updated = ApplyCapitalizationMainOnly(line, lowerFirst);

            // sub
            if (updated.SubLine != null)
            {
                var sub = ApplyCapitalizationMainOnly(updated.SubLine, lowerFirst);

                // best-effort set back if mutable in your models:
                if (updated is LineInfo li) li.SubLine = sub;
                else if (updated is SyllableLineInfo sli) sli.SubLine = sub;
                else if (updated is FullLineInfo fli) fli.SubLine = sub;
                else if (updated is FullSyllableLineInfo fsli) fsli.SubLine = sub;
            }

            return updated;
        }

        private static ILineInfo ApplyCapitalizationMainOnly(ILineInfo line, bool lowerFirst)
        {
            if (line == null) return line!;

            // syllable line
            if (line is SyllableLineInfo syllableLine && syllableLine.Syllables is { Count: > 0 })
            {
                ApplyCapitalizationToSyllables(syllableLine, lowerFirst);
                return syllableLine;
            }

            var text = line.Text ?? string.Empty;
            if (text.Length == 0) return line;

            var normalized = NormalizeSentenceCasePreserveLength(text, lowerFirst);

            // mutable common types
            if (line is LineInfo li)
            {
                li.Text = normalized;
                return li;
            }
            if (line is FullLineInfo fli)
            {
                fli.Text = normalized;
                return fli;
            }

            // fallback replace (keep timing/alignment/subline best-effort)
            var repl = new LineInfo
            {
                Text = normalized,
                StartTime = line.StartTime,
                EndTime = line.EndTime,
                LyricsAlignment = line.LyricsAlignment,
                SubLine = line.SubLine
            };

            if (line is IFullLineInfo full)
            {
                var fullRepl = new FullLineInfo(repl)
                {
                    Pronunciation = full.Pronunciation,
                    Translations = new Dictionary<string, string>(full.Translations)
                };
                return fullRepl;
            }

            return repl;
        }

        private static void ApplyCapitalizationToSyllables(SyllableLineInfo line, bool lowerFirst)
        {
            var syllables = line.Syllables;
            if (syllables == null || syllables.Count == 0) return;

            var sb = new StringBuilder();
            for (int i = 0; i < syllables.Count; i++)
                sb.Append(syllables[i].Text ?? string.Empty);

            var full = sb.ToString();
            if (full.Length == 0) return;

            var normalized = NormalizeSentenceCasePreserveLength(full, lowerFirst);

            int pos = 0;
            for (int i = 0; i < syllables.Count; i++)
            {
                var s = syllables[i];
                var oldText = s.Text ?? string.Empty;
                int len = oldText.Length;

                if (len <= 0) continue;
                if (pos + len > normalized.Length) break;

                var part = normalized.Substring(pos, len);
                pos += len;

                syllables[i] = RewriteSyllableTextPreserveStructure(s, part);
            }

            line.RefreshProperties();
        }

        private static ISyllableInfo RewriteSyllableTextPreserveStructure(ISyllableInfo syllable, string newText)
        {
            if (syllable is SyllableInfo si)
            {
                si.Text = newText;
                return si;
            }

            if (syllable is FullSyllableInfo fi && fi.SubItems is { Count: > 0 })
            {
                var sub = fi.SubItems;
                int pos = 0;

                for (int j = 0; j < sub.Count; j++)
                {
                    var t = sub[j].Text ?? string.Empty;
                    int len = t.Length;

                    if (len <= 0) continue;
                    if (pos + len > newText.Length) break;

                    sub[j].Text = newText.Substring(pos, len);
                    pos += len;
                }

                fi.RefreshProperties();
                return fi;
            }

            return new SyllableInfo(newText, syllable.StartTime, syllable.EndTime);
        }

        private static string NormalizeSentenceCasePreserveLength(string s, bool lowerFirst)
        {
            if (string.IsNullOrEmpty(s)) return s;

            string src = lowerFirst ? s.ToLowerInvariant() : s;
            var sb = new StringBuilder(src.Length);

            bool capNext = true;

            for (int i = 0; i < src.Length; i++)
            {
                char ch = src[i];

                // optional: always fix standalone i -> I (safe, length-preserving)
                if (ch == 'i' && IsStandaloneI(src, i))
                {
                    sb.Append('I');
                    capNext = false;
                    continue;
                }

                if (char.IsLetter(ch))
                {
                    if (capNext)
                    {
                        sb.Append(char.ToUpperInvariant(ch));
                        capNext = false;
                    }
                    else
                    {
                        sb.Append(ch);
                    }
                }
                else
                {
                    sb.Append(ch);
                }

                if (ch == '.' || ch == '?' || ch == '!' ||
                    ch == '。' || ch == '？' || ch == '！')
                {
                    capNext = true;
                }
            }

            return sb.ToString();
        }

        private static bool IsStandaloneI(string s, int index)
        {
            bool leftOk = index == 0 || !char.IsLetter(s[index - 1]);
            bool rightOk = index == s.Length - 1 || !char.IsLetter(s[index + 1]);
            return leftOk && rightOk;
        }

        #endregion
    }
}