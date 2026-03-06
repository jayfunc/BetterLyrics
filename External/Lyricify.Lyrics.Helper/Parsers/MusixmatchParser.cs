using Lyricify.Lyrics.Models;
using Lyricify.Lyrics.Parsers.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Lyricify.Lyrics.Parsers
{
    public static class MusixmatchParser
    {
        public static LyricsData? Parse(string rawJson)
        {
            return Parse(rawJson, false);
        }

        /// <param name="ignoreSyllable">忽略逐字歌词</param>
        public static LyricsData? Parse(string rawJson, bool ignoreSyllable)
        {
            var jsonObj = JObject.Parse(rawJson);
            if (jsonObj?["message"]?["body"]?["macro_calls"] is not JObject calls) return null;

            static bool CheckHeader200(JObject? getObj)
            {
                if (getObj?["message"]?["header"]?["status_code"]?.Type != JTokenType.Integer) return false;
                if (getObj?["message"]?["header"]?.Value<int>("status_code") != 200) return false;
                return true;
            }

            var track_get = calls["track.richsync.get"] as JObject;
            if (!ignoreSyllable && CheckHeader200(track_get))
            {
                var lyrics = track_get?["message"]?["body"]?["richsync"]?["richsync_body"]?.Value<string>();

                if (!string.IsNullOrEmpty(lyrics) && JsonConvert.DeserializeObject<List<RichSyncedLine>>(lyrics) is List<RichSyncedLine> list)
                {
                    var lines = new List<ILineInfo>();
                    foreach (var line in list)
                    {
                        var syllables = new List<SyllableInfo>();
                        var start = (int)(line.TimeStart * 1000);
                        for (int i = 0; i < line.Words.Count; i++)
                        {
                            syllables.Add(new()
                            {
                                StartTime = start + (int)(line.Words[i].Position * 1000),
                                EndTime = i + 1 < line.Words.Count ? start + (int)(line.Words[i + 1].Position * 1000) : (int)(line.TimeEnd * 1000),
                                Text = line.Words[i].Chars,
                            });
                        }
                        lines.Add(new SyllableLineInfo()
                        {
                            Syllables = syllables.Cast<ISyllableInfo>().ToList(),
                        });
                    }

                    var lyricsData = new LyricsData
                    {
                        File = new(),
                        Lines = lines,
                        TrackMetadata = new TrackMetadata(),
                    };
                    lyricsData.File.Type = LyricsTypes.Musixmatch;
                    lyricsData.File.SyncTypes = SyncTypes.SyllableSynced;
                    var language = track_get?["message"]?["body"]?["richsync"]?["richssync_language"]?.Value<string>()
                        ?? track_get?["message"]?["body"]?["richsync"]?["richsync_language"]?.Value<string>();
                    if (language is not null)
                    {
                        lyricsData.TrackMetadata.Language = new() { language };
                    }
                    return lyricsData;
                }
            }

            track_get = calls["track.subtitles.get"] as JObject;
            if (CheckHeader200(track_get))
            {
                var list = track_get?["message"]?["body"]?["subtitle_list"] as JArray;
                if (list is { Count: > 0 })
                {
                    var subtitle = list[0]["subtitle"]?["subtitle_body"]?.Value<string>();
                    if (!string.IsNullOrEmpty(subtitle))
                    {
                        var lines = LrcParser.ParseLyrics(subtitle);
                        var lyricsData = new LyricsData
                        {
                            File = new(),
                            Lines = lines,
                            TrackMetadata = new TrackMetadata(),
                        };
                        lyricsData.File.Type = LyricsTypes.Musixmatch;
                        lyricsData.File.SyncTypes = SyncTypes.LineSynced;
                        var language = list[0]["subtitle"]?["subtitle_language"]?.Value<string>();
                        if (language is not null)
                        {
                            lyricsData.TrackMetadata.Language = new() { language };
                        }
                        return lyricsData;
                    }
                }
            }

            track_get = calls["track.lyrics.get"] as JObject;
            if (CheckHeader200(track_get))
            {
                var lyrics = track_get?["message"]?["body"]?["lyrics"]?["lyrics_body"]?.Value<string>();

                if (!string.IsNullOrEmpty(lyrics))
                {
                    var list = lyrics.Trim()
                        .Split('\n')
                        .Select(line => new LineInfo { Text = line })
                        .Cast<ILineInfo>()
                        .ToList();

                    var lyricsData = new LyricsData
                    {
                        File = new(),
                        Lines = list,
                    };
                    lyricsData.File.Type = LyricsTypes.Musixmatch;
                    lyricsData.File.SyncTypes = SyncTypes.Unsynced;
                    return lyricsData;
                }
            }

            return null;
        }
    }
}
