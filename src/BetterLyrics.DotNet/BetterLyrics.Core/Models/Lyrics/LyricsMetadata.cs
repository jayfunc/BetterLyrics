namespace BetterLyrics.Core.Models.Lyrics;

public class LyricsMetadata
{
    public string Title { get; set; } = ""; // [ti] 标题
    public string Artist { get; set; } = ""; // [ar] 歌手
    public string Album { get; set; } = ""; // [al] 专辑

    public string Author { get; set; } = ""; // [au] 作曲/原作者
    public string Lyricist { get; set; } = ""; // [lr] 作词人
    public string LrcCreator { get; set; } = ""; // [by] LRC文件制作者

    public int Offset { get; set; } = 0; // [offset] 整体时间偏移量(ms)
    public string Length { get; set; } = "00:00"; // [length] 歌曲长度 (mm:ss)

    public double TotalSeconds
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Length)) return 0;

            try
            {
                var parts = Length.Split(':');

                if (parts.Length == 2)
                {
                    var minutes = double.Parse(parts[0]);
                    var seconds = double.Parse(parts[1]);
                    return minutes * 60 + seconds;
                }

                if (parts.Length == 3)
                {
                    var hours = double.Parse(parts[0]);
                    var minutes = double.Parse(parts[1]);
                    var seconds = double.Parse(parts[2]);
                    return hours * 3600 + minutes * 60 + seconds;
                }

                if (parts.Length == 1) return double.Parse(parts[0]);
            }
            catch
            {
                return 0;
            }

            return 0;
        }
    }

    public string Tool { get; set; } = ""; // [re] or [tool] 生成工具
    public string Version { get; set; } = ""; // [ve] 工具版本

    public List<string> Comments { get; set; } = []; // [#] 注释
}