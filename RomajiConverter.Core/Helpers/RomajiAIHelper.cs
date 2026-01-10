using OpenAI;
using OpenAI.Chat;
using RomajiConverter.Core.Models;
using RomajiConverter.Core.Options;
using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace RomajiConverter.Core.Helpers
{
    public static class RomajiAIHelper
    {
        public const string Prompt = @"用户将输入一段日文歌词，你需要逐词转换为以下格式：
- 每行输出必须严格对应每行输入，禁止额外添加换行，禁止输出空行，不能因为遇到标点符号而换行，换行符必须使用单个\n
- 对每行日文进行分词处理，分词应以现代日语常规形态（助词、助动词、词尾变化）为最小单位
- 如果一个分词是日文且包含汉字，则需要给出平假名，用小括号在原文后标注，格式为：日文分词(平假名)。禁止在分词中间标注假名（例：x(xx)xx），要么标注整个分词的假名，要么将标注之后的部分拆分为新的分词
- 纯假名分词不添加任何假名标注
- 遇到仅当 は/へ/を 作为独立分词并起语法助词作用时，在后面添加“|”以及它的口语化假名，非助词情况下只输出原文
- 遇到英文单词/字母、数字、标点符号、特殊符号、等非日文的unicode字符时，必须保留且单独作为一个分词，必须只输出原文，不能给出平假名
- 每个分词之间必须用半角空格分隔
- 如果无法确定某分词是否为助词或其读音，请优先保持原文不转换
- 不要包含任何解释、注释、Markdown、额外字段或文本
- 示例仅供参考，不能直接输出，任何时候都需要根据上面给出的文本进行转换
示例：
输入：昨日はColdな夜へ行を歌った
输出：昨日(きのう) は|わ Cold な 夜(よる) へ|え 行(い) を|お 歌った(うたった)";

        private static Regex _formatRegex = new Regex(@"^(.*?)(\((.*?)\))*?(\|(.*?))*?$", RegexOptions.Compiled);

        private static ChatCompletionOptions _chatCompletionOptions = new ChatCompletionOptions
        {
            Temperature = 0.2f
        };

        public static async Task LoadRomajiAsync(ICollection<ConvertedLine> convertedLines, string text, ToRomajiAIOptions options, CancellationToken cancellationToken = default)
        {
            //预处理为ConvertedLine列表, 其中会包含空行
            var cacheList = GetCacheList(options, text);

            if (cacheList.Count == 0) return;

            //获取ai结果
            var client = new ChatClient(
                model: options.Model,
                credential: new ApiKeyCredential(options.ApiKey),
                options: new OpenAIClientOptions
                {
                    Endpoint = new Uri(options.BaseUrl)
                }
            );

            var prompt = string.IsNullOrEmpty(options.Prompt) ? Prompt : options.Prompt;
            //发送的内容不包含空行
            var content = string.Join("\n", cacheList.Where(p => !string.IsNullOrWhiteSpace(p.Japanese)).Select(p => p.Japanese));

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(prompt),
                new UserChatMessage(content)
            };

            Debug.WriteLine(prompt);
            Debug.WriteLine(content);

            var completionUpdates = client.CompleteChatStreamingAsync(messages, _chatCompletionOptions, cancellationToken: cancellationToken);

            var stringBuilder = new StringBuilder();
            ushort lineIndex = 0;
            var l = 0;
            var r = 0;

            //插入直到下一个非空行
            AddNextNotEmptyLine();

            //处理流式返回
            var enumerator = completionUpdates.GetAsyncEnumerator(cancellationToken);
            try
            {
                while (await enumerator.MoveNextAsync())
                {
                    var completionUpdate = enumerator.Current;
                    if (completionUpdate.ContentUpdate.Count > 0)
                    {
                        var delta = FixFormat(completionUpdate.ContentUpdate[0].Text);
                        if (string.IsNullOrEmpty(delta)) continue;
                        stringBuilder.Append(delta);
                        Debug.Write(completionUpdate.ContentUpdate[0].Text);

                        while (r < stringBuilder.Length)
                        {
                            if (stringBuilder[r] == '\n')
                            {
                                InsertUnit();
                                //插入直到下一个非空行
                                AddNextNotEmptyLine();
                                r++;
                                l = r;
                            }
                            else if (stringBuilder[r] == ' ')
                            {
                                InsertUnit();
                                r++;
                                l = r;
                            }
                            else
                            {
                                r++;
                            }
                        }
                    }
                }
            }
            finally
            {
                await enumerator.DisposeAsync();
            }

            //处理完成,手动插入最后一个分词
            if (l != r)
            {
                InsertUnit();
            }

            return;

            void AddNextNotEmptyLine()
            {
                do
                {
                    var newLine = new ConvertedLine
                    {
                        Time = lineIndex >= cacheList.Count ? (TimeSpan?)null : cacheList[lineIndex].Time,
                        Chinese = lineIndex >= cacheList.Count ? string.Empty : cacheList[lineIndex].Chinese,
                        Index = lineIndex,
                        Japanese = lineIndex >= cacheList.Count ? string.Empty : cacheList[lineIndex].Japanese
                    };
                    convertedLines.Add(newLine);
                    lineIndex++;
                } while (string.IsNullOrWhiteSpace(convertedLines.Last().Japanese) && lineIndex < cacheList.Count);
            }

            void InsertUnit()
            {
                var lastLine = convertedLines.Last();
                var lastUnitStr = stringBuilder.ToString(l, r - l);
                if (!string.IsNullOrEmpty(lastUnitStr))
                    lastLine.Units.Add(GetUnit(lastLine.Index, lastUnitStr, options.IsParticleAsPronunciation));
            }
        }

        private static List<ConvertedLine> GetCacheList(ToRomajiAIOptions options, string text)
        {
            var timeSpans = new List<TimeSpan?>();
            var lineTextList = text.Split(Environment.NewLine.ToArray()).Where(p => !string.IsNullOrWhiteSpace(p)).ToList();

            for (var i = 0; i < lineTextList.Count; i++)
            {
                if (LrcParser.LrcLineRegex.IsMatch(lineTextList[i]))
                {
                    var lyrics = LrcParser.Parse(lineTextList[i]);
                    timeSpans.Add(lyrics.Count > 0 ? lyrics[0].Time : (TimeSpan?)null);
                    lineTextList[i] = lyrics.Count > 0 ? lyrics[0].Text : lineTextList[i];
                }
                else
                {
                    timeSpans.Add(null);
                }
            }

            var cacheList = new List<ConvertedLine>();
            for (var index = 0; index < lineTextList.Count; index++)
            {
                var line = lineTextList[index];

                if (RomajiHelper.IsChinese(line, options.ChineseRate)) continue;

                var convertedLine = new ConvertedLine
                {
                    Time = index < timeSpans.Count ? timeSpans[index] : null,
                    Japanese = line.Replace("\0", "")
                };

                if (index + 1 < lineTextList.Count &&
                    RomajiHelper.IsChinese(lineTextList[index + 1], options.ChineseRate))
                    convertedLine.Chinese = lineTextList[index + 1];

                convertedLine.Index = (ushort)cacheList.Count;
                cacheList.Add(convertedLine);
            }

            return cacheList;
        }

        private static string FixFormat(string content)
        {
            content = content.Replace("\r", "");
            content = content.Replace("\\n", "\n");

            return content;
        }

        private static ConvertedUnit GetUnit(ushort lineIndex, string unitString, bool isParticleAsPronunciation)
        {
            var match = _formatRegex.Match(unitString);

            if (!match.Success)
            {
                return new ConvertedUnit(lineIndex, unitString, KanaHelper.ToHiragana(unitString),
                    KanaHelper.KatakanaToRomaji(unitString), false);
            }

            var origin = match.Groups[1].Value;
            var kanji_gana = match.Groups[3].Value;
            var particle_gana = match.Groups[5].Value;

            if (!string.IsNullOrEmpty(kanji_gana))
            {
                return new ConvertedUnit(lineIndex, origin, kanji_gana,
                    KanaHelper.KatakanaToRomaji(kanji_gana), true);
            }
            else if (isParticleAsPronunciation && !string.IsNullOrEmpty(particle_gana))
            {
                return new ConvertedUnit(lineIndex, origin, particle_gana,
                    KanaHelper.KatakanaToRomaji(particle_gana), false);
            }
            else
            {
                return new ConvertedUnit(lineIndex, origin, KanaHelper.ToHiragana(origin),
                    KanaHelper.KatakanaToRomaji(origin), false);
            }
        }
    }
}
