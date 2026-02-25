using Lyricify.Lyrics.Helpers.General;
using System.Text;

namespace Lyricify.Lyrics.Models
{
    public interface ILineInfo : IComparable
    {
        public string Text { get; }

        public int? StartTime { get; }

        public int? EndTime { get; }

        public int? Duration => EndTime - StartTime;

        public int? StartTimeWithSubLine => MathHelper.Min(StartTime, SubLine?.StartTime);

        public int? EndTimeWithSubLine => MathHelper.Max(EndTime, SubLine?.EndTime);

        public int? DurationWithSubLine => EndTimeWithSubLine - StartTimeWithSubLine;

        public LyricsAlignment LyricsAlignment { get; }

        public ILineInfo? SubLine { get; }

        /// <summary>
        /// <see cref="Text"/> if SubLine not exist, or full lyrics with bracketed subline lyrics
        /// </summary>
        public string FullText
        {
            get
            {
                if (SubLine == null)
                {
                    return Text;
                }
                else
                {
                    var sb = new StringBuilder();
                    if (SubLine.StartTime < StartTime)
                    {
                        sb.Append('(');
                        sb.Append(SubLine.Text.RemoveFrontBackBrackets());
                        sb.Append(") ");
                        sb.Append(Text.Trim());
                    }
                    else
                    {
                        sb.Append(Text.Trim());
                        sb.Append(" (");
                        sb.Append(SubLine.Text.RemoveFrontBackBrackets());
                        sb.Append(')');
                    }
                    return sb.ToString();
                }
            }
        }
    }

    public interface IFullLineInfo : ILineInfo
    {
        public Dictionary<string, string> Translations { get; }

        public string? ChineseTranslation
        {
            get => Translations.ContainsKey("zh") ? Translations["zh"] : null;
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    Translations.Remove("zh");
                }
                else
                {
                    Translations["zh"] = value;
                }
            }
        }

        public string? Pronunciation { get; set; }
    }
}
