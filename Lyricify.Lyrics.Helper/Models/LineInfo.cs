namespace Lyricify.Lyrics.Models
{
    public class LineInfo : ILineInfo
    {
#pragma warning disable CS8618
        public LineInfo() { }
#pragma warning restore CS8618

        public LineInfo(string text)
        {
            Text = text;
        }

        public LineInfo(string text, int startTime)
        {
            Text = text;
            StartTime = startTime;
        }

        public LineInfo(string text, int startTime, int? endTime) : this(text, startTime)
        {
            EndTime = endTime;
        }

        public string Text { get; set; }

        public int? StartTime { get; set; }

        public int? EndTime { get; set; }

        public LyricsAlignment LyricsAlignment { get; set; } = LyricsAlignment.Unspecified;

        public ILineInfo? SubLine { get; set; }

        public int CompareTo(object obj)
        {
            if (obj is ILineInfo line)
            {
                if (StartTime is null || line.StartTime is null) return 0;
                if (StartTime == line.StartTime) return 0;
                if (StartTime < line.StartTime) return -1;
                else return 1;
            }
            return 0;
        }
    }

    public class SyllableLineInfo : ILineInfo
    {
#pragma warning disable CS8618
        public SyllableLineInfo() { }
#pragma warning restore CS8618

        public SyllableLineInfo(IEnumerable<ISyllableInfo> syllables)
        {
            Syllables = syllables.ToList();
        }

        private string? _text = null;
        public string Text => _text ??= SyllableHelper.GetTextFromSyllableList(Syllables);

        private int? _startTime = null;
        public int? StartTime => _startTime ??= Syllables.First().StartTime;

        private int? _endTime = null;
        public int? EndTime => _endTime ??= Syllables.Last().EndTime;

        public LyricsAlignment LyricsAlignment { get; set; } = LyricsAlignment.Unspecified;

        public ILineInfo? SubLine { get; set; }

        public List<ISyllableInfo> Syllables { get; set; }

        public bool IsSyllable => Syllables is { Count: > 0 };

        public int CompareTo(object obj)
        {
            if (obj is ILineInfo line)
            {
                if (StartTime is null || line.StartTime is null) return 0;
                if (StartTime == line.StartTime) return 0;
                if (StartTime < line.StartTime) return -1;
                else return 1;
            }
            return 0;
        }

        /// <summary>
        /// Refresh preloaded properties if Syllables have been updated
        /// </summary>
        public void RefreshProperties()
        {
            _text = null;
            _startTime = null;
            _endTime = null;
        }
    }

    public class FullLineInfo : LineInfo, IFullLineInfo
    {
        public FullLineInfo() { }

        public FullLineInfo(LineInfo lineInfo)
        {
            Text = lineInfo.Text;
            StartTime = lineInfo.StartTime;
            EndTime = lineInfo.EndTime;
            LyricsAlignment = lineInfo.LyricsAlignment;
            SubLine = lineInfo.SubLine;
        }

        public Dictionary<string, string> Translations { get; set; } = new();

        public string? Pronunciation { get; set; }
    }

    public class FullSyllableLineInfo : SyllableLineInfo, IFullLineInfo
    {
        public FullSyllableLineInfo() { }

        public FullSyllableLineInfo(SyllableLineInfo lineInfo)
        {
            LyricsAlignment = lineInfo.LyricsAlignment;
            SubLine = lineInfo.SubLine;
            Syllables = lineInfo.Syllables;
        }

        public FullSyllableLineInfo(SyllableLineInfo lineInfo, string? chineseTranslation = null, string? pronunciation = null) : this(lineInfo)
        {
            if (!string.IsNullOrEmpty(chineseTranslation))
            {
                Translations["zh"] = chineseTranslation;
            }

            if (!string.IsNullOrEmpty(pronunciation))
            {
                Pronunciation = pronunciation;
            }
        }

        public Dictionary<string, string> Translations { get; set; } = new();

        public string? Pronunciation { get; set; }
    }

    public enum LyricsAlignment
    {
        Unspecified,
        Left,
        Right
    }
}
