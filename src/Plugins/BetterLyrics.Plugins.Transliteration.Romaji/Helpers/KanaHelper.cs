using System.Text;

namespace BetterLyrics.Plugins.Transliteration.Romaji.Helpers
{
    /// <summary>
    /// 此类用于片假、平假互转
    /// </summary>
    public static class KanaHelper
    {
        /// <summary>
        /// 转为片假名
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string ToKatakana(string str)
        {
            var stringBuilder = new StringBuilder();
            foreach (var c in str)
            {
                var bytes = Encoding.Unicode.GetBytes(c.ToString());
                if (bytes.Length == 2 && bytes[1] == 0x30 && bytes[0] >= 0x40 && bytes[0] <= 0x9F)
                    stringBuilder.Append(Encoding.Unicode.GetString(new[] { (byte)(bytes[0] + 0x60), bytes[1] }));
                else
                    stringBuilder.Append(c);
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// 转为平假名
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string ToHiragana(string str)
        {
            var stringBuilder = new StringBuilder();
            foreach (var c in str)
            {
                var bytes = Encoding.Unicode.GetBytes(c.ToString());
                if (bytes.Length == 2 && bytes[1] == 0x30 && bytes[0] >= 0xA0 && bytes[0] <= 0xFA)
                    stringBuilder.Append(Encoding.Unicode.GetString(new[] { (byte)(bytes[0] - 0x60), bytes[1] }));
                else
                    stringBuilder.Append(c);
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// 假名转罗马音
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string KatakanaToRomaji(string str)
        {
            var result = new StringBuilder();

            for (var i = 0; i < str.Length;)
            {
                if (i < str.Length - 1)
                {
                    var extendedWord = str.Substring(i, 2);
                    if (ExtendedKanaDictionary.ContainsKey(extendedWord))
                    {
                        result.Append(ExtendedKanaDictionary[extendedWord]);
                        i += 2;
                        continue;
                    }
                }

                var word = str[i].ToString();
                if (KanaDictionary.ContainsKey(word))
                {
                    //正常转换
                    result.Append(KanaDictionary[word]);
                }
                else if (word == "ー")
                {
                    //长音,取前一个音
                    result.Append(result.Length > 0 ? result[result.Length - 1].ToString() : word);
                }
                else
                {
                    //不能识别,保持原样
                    result.Append(word);
                }

                i++;
            }

            //处理促音
            for (var i = 0; i < result.Length; i++)
            {
                if (result[i] == 'っ' || result[i] == 'ッ')
                    if (i < result.Length - 1)
                        if (result[i + 1] == 'c')
                            result[i] = 't';
                        else
                            result[i] = result[i + 1];
                    else
                        result.Remove(i, 1);
            }

            return result.ToString();
        }

        public static Dictionary<string, string> KanaDictionary = new Dictionary<string, string>
    {
        //平假
        { "あ", "a" }, { "い", "i" }, { "う", "u" }, { "え", "e" }, { "お", "o" },
        { "か", "ka" }, { "き", "ki" }, { "く", "ku" }, { "け", "ke" }, { "こ", "ko" },
        { "さ", "sa" }, { "し", "shi" }, { "す", "su" }, { "せ", "se" }, { "そ", "so" },
        { "た", "ta" }, { "ち", "chi" }, { "つ", "tsu" }, { "て", "te" }, { "と", "to" },
        { "な", "na" }, { "に", "ni" }, { "ぬ", "nu" }, { "ね", "ne" }, { "の", "no" },
        { "は", "ha" }, { "ひ", "hi" }, { "ふ", "fu" }, { "へ", "he" }, { "ほ", "ho" },
        { "ま", "ma" }, { "み", "mi" }, { "む", "mu" }, { "め", "me" }, { "も", "mo" },
        { "や", "ya" }, { "ゆ", "yu" }, { "よ", "yo" },
        { "ら", "ra" }, { "り", "ri" }, { "る", "ru" }, { "れ", "re" }, { "ろ", "ro" },
        { "わ", "wa" }, { "を", "wo" },
        { "が", "ga" }, { "ぎ", "gi" }, { "ぐ", "gu" }, { "げ", "ge" }, { "ご", "go" },
        { "ざ", "za" }, { "じ", "ji" }, { "ず", "zu" }, { "ぜ", "ze" }, { "ぞ", "zo" },
        { "だ", "da" }, { "ぢ", "ji" }, { "づ", "zu" }, { "で", "de" }, { "ど", "do" },
        { "ば", "ba" }, { "び", "bi" }, { "ぶ", "bu" }, { "べ", "be" }, { "ぼ", "bo" },
        { "ぱ", "pa" }, { "ぴ", "pi" }, { "ぷ", "pu" }, { "ぺ", "pe" }, { "ぽ", "po" },
        { "ん", "n" },

        //片假
        { "ア", "a" }, { "イ", "i" }, { "ウ", "u" }, { "エ", "e" }, { "オ", "o" },
        { "カ", "ka" }, { "キ", "ki" }, { "ク", "ku" }, { "ケ", "ke" }, { "コ", "ko" },
        { "サ", "sa" }, { "シ", "shi" }, { "ス", "su" }, { "セ", "se" }, { "ソ", "so" },
        { "タ", "ta" }, { "チ", "chi" }, { "ツ", "tsu" }, { "テ", "te" }, { "ト", "to" },
        { "ナ", "na" }, { "ニ", "ni" }, { "ヌ", "nu" }, { "ネ", "ne" }, { "ノ", "no" },
        { "ハ", "ha" }, { "ヒ", "hi" }, { "フ", "fu" }, { "ヘ", "he" }, { "ホ", "ho" },
        { "マ", "ma" }, { "ミ", "mi" }, { "ム", "mu" }, { "メ", "me" }, { "モ", "mo" },
        { "ヤ", "ya" }, { "ユ", "yu" }, { "ヨ", "yo" },
        { "ラ", "ra" }, { "リ", "ri" }, { "ル", "ru" }, { "レ", "re" }, { "ロ", "ro" },
        { "ワ", "wa" }, { "ヲ", "wo" },
        { "ガ", "ga" }, { "ギ", "gi" }, { "グ", "gu" }, { "ゲ", "ge" }, { "ゴ", "go" },
        { "ザ", "za" }, { "ジ", "ji" }, { "ズ", "zu" }, { "ゼ", "ze" }, { "ゾ", "zo" },
        { "ダ", "da" }, { "ヂ", "ji" }, { "ヅ", "zu" }, { "デ", "de" }, { "ド", "do" },
        { "バ", "ba" }, { "ビ", "bi" }, { "ブ", "bu" }, { "ベ", "be" }, { "ボ", "bo" },
        { "パ", "pa" }, { "ピ", "pi" }, { "プ", "pu" }, { "ペ", "pe" }, { "ポ", "po" },
        { "ン", "n" },

        //小版本
        { "ぁ", "a" }, { "ぃ", "i" }, { "ぅ", "u" }, { "ぇ", "e" }, { "ぉ", "o" },
        { "ゃ", "ya" }, { "ゅ", "yu" }, { "ょ", "yo" }, { "ゎ", "wa" },
        { "ァ", "a" }, { "ィ", "i" }, { "ゥ", "u" }, { "ェ", "e" }, { "ォ", "o" },
        { "ャ", "ya" }, { "ュ", "yu" }, { "ョ", "yo" }, { "ヮ", "wa" },
    };

        public static Dictionary<string, string> ExtendedKanaDictionary = new Dictionary<string, string>
    {
        //平假-拗音
        { "きゃ", "kya" }, { "きゅ", "kyu" }, { "きょ", "kyo" },
        { "しゃ", "sha" }, { "しゅ", "shu" }, { "しょ", "sho" },
        { "ちゃ", "cha" }, { "ちゅ", "chu" }, { "ちょ", "cho" },
        { "にゃ", "nya" }, { "にゅ", "nyu" }, { "にょ", "nyo" },
        { "ひゃ", "hya" }, { "ひゅ", "hyu" }, { "ひょ", "hyo" },
        { "みゃ", "mya" }, { "みゅ", "myu" }, { "みょ", "myo" },
        { "りゃ", "rya" }, { "りゅ", "ryu" }, { "りょ", "ryo" },
        { "ぎゃ", "gya" }, { "ぎゅ", "gyu" }, { "ぎょ", "gyo" },
        { "じゃ", "ja" }, { "じゅ", "ju" }, { "じょ", "jo" },
        { "ぢゃ", "ja" }, { "ぢゅ", "ju" }, { "ぢょ", "jo" },
        { "びゃ", "bya" }, { "びゅ", "byu" }, { "びょ", "byo" },
        { "ぴゃ", "pya" }, { "ぴゅ", "pyu" }, { "ぴょ", "pyo" },

        //片假-拗音
        { "キャ", "kya" }, { "キュ", "kyu" }, { "キョ", "kyo" },
        { "シャ", "sha" }, { "シュ", "shu" }, { "ショ", "sho" },
        { "チャ", "cha" }, { "チュ", "chu" }, { "チョ", "cho" },
        { "ニャ", "nya" }, { "ニュ", "nyu" }, { "ニョ", "nyo" },
        { "ヒャ", "hya" }, { "ヒュ", "hyu" }, { "ヒョ", "hyo" },
        { "ミャ", "mya" }, { "ミュ", "myu" }, { "ミョ", "myo" },
        { "リャ", "rya" }, { "リュ", "ryu" }, { "リョ", "ryo" },
        { "ギャ", "gya" }, { "ギュ", "gyu" }, { "ギョ", "gyo" },
        { "ジャ", "ja" }, { "ジュ", "ju" }, { "ジョ", "jo" },
        { "ヂャ", "ja" }, { "ヂュ", "ju" }, { "ヂョ", "jo" },
        { "ビャ", "bya" }, { "ビュ", "byu" }, { "ビョ", "byo" },
        { "ピャ", "pya" }, { "ピュ", "pyu" }, { "ピョ", "pyo" },

        //其他语言
        { "イェ", "ye" },
        { "ウィ", "wi" }, { "ウェ", "we" }, { "ウォ", "wo" },
        { "ヴァ", "va" }, { "ヴィ", "vi" }, { "ヴ", "vu" }, { "ヴェ", "ve" }, { "ヴォ", "vo" },
        { "ヴュ", "vyu" },
        { "クァ", "kwa" }, { "クィ", "kwi" }, { "クェ", "kwe" }, { "クォ", "kwo" },
        { "グァ", "gwa" },
        { "シェ", "she" },
        { "ジェ", "je" },
        { "チェ", "che" },
        { "ツァ", "tsa" }, { "ツィ", "tsi" }, { "ツェ", "tse" }, { "ツォ", "tso" },
        { "ティ", "ti" }, { "トゥ", "tu" },
        { "テュ", "tyu" },
        { "ディ", "di" }, { "ドゥ", "du" },
        { "デュ", "dyu" },
        { "ファ", "fa" }, { "フィ", "fi" }, { "フェ", "fe" }, { "フォ", "fo" },
        { "フュ", "fyu" },
    };
    }
}