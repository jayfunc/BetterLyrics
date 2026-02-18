using BetterLyrics.Plugins.Transliteration.Romaji.Extensions;
using BetterLyrics.Plugins.Transliteration.Romaji.Models;
using BetterLyrics.Plugins.Transliteration.Romaji.Options;
using MeCab;
using MeCab.Extension.UniDic;
using System.Collections.ObjectModel;
using System.Text;
using System.Text.RegularExpressions;

namespace BetterLyrics.Plugins.Transliteration.Romaji.Helpers
{
    public static class RomajiHelper
    {
        /// <summary>
        /// 分词器
        /// </summary>
        private static MeCabTagger _tagger;

        /// <summary>
        /// 自定义词典<原文, 假名>
        /// </summary>
        private static Dictionary<string, string> _customizeDict;

        public static void Init(string baseDirectory = null)
        {
            string rootPath = !string.IsNullOrEmpty(baseDirectory)
                      ? baseDirectory
                      : AppDomain.CurrentDomain.BaseDirectory;

            //词典路径
            var dicPath = Path.Combine(rootPath, "unidic");
            var parameter = new MeCabParam
            {
                DicDir = dicPath,
                LatticeLevel = MeCabLatticeLevel.Zero
            };
            _tagger = MeCabTagger.Create(parameter);

            var str = File.ReadAllText(Path.Combine(rootPath, "customizeDict.txt"));
            var list = str.Split(Environment.NewLine.ToArray());
            _customizeDict = new Dictionary<string, string>();
            foreach (var item in list)
            {
                if (string.IsNullOrWhiteSpace(item)) continue;
                var array = item.Split(' ');
                if (array.Length < 2) continue;
                if (!_customizeDict.ContainsKey(array[0]))
                    _customizeDict.Add(array[0], array[1]);
            }
        }

        #region 主逻辑

        /// <summary>
        /// 生成转换结果列表
        /// </summary>
        /// <param name="text"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static IEnumerable<ConvertedLine> ToRomaji(string text, ToRomajiOptions options = null)
        {
            options = options ?? new ToRomajiOptions();

            var timeSpans = new List<TimeSpan?>();
            var lineTextList = text.Split(Environment.NewLine.ToArray()).Where(p => !string.IsNullOrWhiteSpace(p)).ToList();

            for (var i = 0; i < lineTextList.Count; i++)
            {
                if (LrcParser.LrcLineRegex.IsMatch(lineTextList[i]))
                {
                    var lyric = LrcParser.Parse(lineTextList[i]).FirstOrDefault();
                    timeSpans.Add(lyric.Time);
                    lineTextList[i] = lyric.Text;
                }
                else
                {
                    timeSpans.Add(null);
                }
            }

            ushort lineIndex = 0;
            for (var index = 0; index < lineTextList.Count; index++)
            {
                var line = lineTextList[index];

                if (IsChinese(line, options.ChineseRate)) continue;

                var convertedLine = new ConvertedLine
                {
                    Index = lineIndex,
                    Time = index < timeSpans.Count ? timeSpans[index] : null,
                    Japanese = line.Replace("\0", "")
                };

                foreach (var sentence in convertedLine.Japanese.LineToUnits())
                {
                    if (IsEnglish(sentence))
                    {
                        convertedLine.Units.Add(new ConvertedUnit(lineIndex, sentence, sentence, sentence, false));
                    }
                    else
                        foreach (var unit in SentenceToRomaji(lineIndex, sentence, options.IsParticleAsPronunciation))
                            convertedLine.Units.Add(unit);
                }

                if (index + 1 < lineTextList.Count && IsChinese(lineTextList[index + 1], options.ChineseRate))
                    convertedLine.Chinese = lineTextList[index + 1];

                lineIndex++;
                yield return convertedLine;
            }
        }

        /// <summary>
        /// 分句转为罗马音
        /// </summary>
        /// <param name="lineIndex"></param>
        /// <param name="str"></param>
        /// <param name="isParticleAsPronunciation"></param>
        /// <returns></returns>
        public static IEnumerable<ConvertedUnit> SentenceToRomaji(ushort lineIndex, string str, bool isParticleAsPronunciation)
        {
            foreach (var item in _tagger.ParseToNodes(str))
            {
                var unit = MeCabNodeToUnit(lineIndex, item, isParticleAsPronunciation);

                if (unit != null)
                    yield return unit;
            }
        }

        public static ConvertedUnit MeCabNodeToUnit(ushort lineIndex, MeCabNode item, bool isParticleAsPronunciation)
        {
            ConvertedUnit unit = null;
            if (item.CharType > 0)
            {
                var features = CustomSplit(item.Feature);
                if (TryCustomConvert(item.Surface, out var customResult))
                {
                    //用户自定义词典
                    unit = new ConvertedUnit(lineIndex,
                        item.Surface,
                        customResult,
                        KanaHelper.KatakanaToRomaji(customResult),
                        true);
                }
                else if (features.Length > 0 && (!isParticleAsPronunciation || item.GetPos1() != "助詞") && IsJapanese(item.Surface))
                {
                    //纯假名
                    unit = new ConvertedUnit(lineIndex,
                        item.Surface,
                        KanaHelper.ToHiragana(item.Surface),
                        KanaHelper.KatakanaToRomaji(item.Surface),
                        false);
                }
                else if (features.Length <= 6 || new[] { "補助記号" }.Contains(item.GetPos1()))
                {
                    //标点符号或无法识别的字
                    unit = new ConvertedUnit(lineIndex,
                        item.Surface,
                        item.Surface,
                        item.Surface,
                        false);
                }
                else if (IsEnglish(item.Surface))
                {
                    //英文
                    unit = new ConvertedUnit(lineIndex,
                        item.Surface,
                        item.Surface,
                        item.Surface,
                        false);
                }
                else
                {
                    //汉字或助词
                    var kana = GetKana(item);

                    unit = new ConvertedUnit(lineIndex,
                        item.Surface,
                        KanaHelper.ToHiragana(kana),
                        KanaHelper.KatakanaToRomaji(kana),
                        !IsJapanese(item.Surface));
                    var (replaceHiragana, replaceRomaji) = GetReplaceData(item);
                    unit.ReplaceHiragana = replaceHiragana;
                    unit.ReplaceRomaji = replaceRomaji;
                }
            }
            else if (item.Stat != MeCabNodeStat.Bos && item.Stat != MeCabNodeStat.Eos)
            {
                unit = new ConvertedUnit(lineIndex,
                    item.Surface,
                    item.Surface,
                    item.Surface,
                    false);
            }

            return unit;
        }

        #endregion

        #region 帮助方法

        /// <summary>
        /// 自定义分隔方法(Feature可能存在如 a,b,c,"d,e",f 格式的数据,此处不能把双引号中的内容也分隔开)
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private static string[] CustomSplit(string str)
        {
            var list = new List<string>();
            var item = new List<char>();
            var haveMark = false;
            foreach (var c in str)
                if (c == ',' && !haveMark)
                {
                    list.Add(new string(item.ToArray()));
                    item.Clear();
                }
                else if (c == '"')
                {
                    item.Add(c);
                    haveMark = !haveMark;
                }
                else
                {
                    item.Add(c);
                }

            return list.ToArray();
        }

        /// <summary>
        /// 获取所有发音
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private static (ObservableCollection<ReplaceString> replaceHiragana, ObservableCollection<ReplaceString>
            replaceRomaji) GetReplaceData(MeCabNode node)
        {
            var length = node.Length;
            var replaceNodeList = new List<MeCabNode>();

            GetAllReplaceNode(replaceNodeList, node);

            void GetAllReplaceNode(List<MeCabNode> list, MeCabNode n)
            {
                if (n != null && !list.Contains(n) && n.Length == length)
                {
                    list.Add(n);
                    GetAllReplaceNode(list, n.BNext);
                    GetAllReplaceNode(list, n.ENext);
                }
            }

            var replaceHiragana = new ObservableCollection<ReplaceString>();
            var replaceRomaji = new ObservableCollection<ReplaceString>();

            ushort i = 1;
            foreach (var meCabNode in replaceNodeList
                         .GroupBy(GetKana)
                         .Select(g => g.First()))
            {
                var kana = GetKana(meCabNode);
                if (kana != null)
                {
                    replaceHiragana.Add(new ReplaceString(i, KanaHelper.ToHiragana(kana), true));
                    replaceRomaji.Add(new ReplaceString(i, KanaHelper.KatakanaToRomaji(kana), true));
                    i++;
                }
            }

            return (replaceHiragana, replaceRomaji);
        }

        private static string GetKana(MeCabNode node)
        {
            return node.GetPos1() == "助詞" ? node.GetPron() : node.GetKana();
        }

        /// <summary>
        /// 自定义转换规则
        /// </summary>
        /// <param name="str"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private static bool TryCustomConvert(string str, out string result)
        {
            if (_customizeDict.ContainsKey(str))
            {
                result = _customizeDict[str];
                return true;
            }

            result = "";
            return false;
        }

        /// <summary>
        /// 判断字符串(句子)是否简体中文
        /// </summary>
        /// <param name="str"></param>
        /// <param name="rate">容错率(0-1)</param>
        /// <returns></returns>
        public static bool IsChinese(string str, float rate)
        {
            if (str.Length < 2)
                return false;

            var wordArray = str.ToCharArray();
            var total = wordArray.Length;
            var chCount = 0f;
            var enCount = 0f;

            foreach (var word in wordArray)
            {
                if (word != 'ー' && IsJapanese(word.ToString()))
                    //含有日文直接返回否
                    return false;

                var gbBytes = Encoding.Unicode.GetBytes(word.ToString());

                if (gbBytes.Length == 2) // double bytes char.  
                {
                    if (gbBytes[1] >= 0x4E && gbBytes[1] <= 0x9F) //中文
                        chCount++;
                    else
                        total--;
                }
                else if (gbBytes.Length == 1)
                {
                    var byteAscii = int.Parse(gbBytes[0].ToString());
                    if ((byteAscii >= 65 && byteAscii <= 90) || (byteAscii >= 97 && byteAscii <= 122)) //英文字母
                        enCount++;
                    else
                        total--;
                }
            }

            if (chCount == 0) return false; //一个简体中文都没有

            return (chCount + enCount) / total >= rate;
        }

        /// <summary>
        /// 判断字符串是否全为单字节
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool IsEnglish(string str)
        {
            return new Regex("^[\x20-\x7E]+$", RegexOptions.Compiled).IsMatch(str);
        }

        /// <summary>
        /// 判断字符串是否全为假名
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private static bool IsJapanese(string str)
        {
            return Regex.IsMatch(str, @"^[\u3040-\u30ff]+$", RegexOptions.Compiled);
        }

        #endregion
    }
}