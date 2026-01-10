using BetterLyrics.Core.Interfaces;
using BetterLyrics.Core.Models;

namespace BetterLyrics.Plugins.Demo
{
    public class DemoLyricsProvider : ILyricsSearchPlugin
    {
        public string Id => "f7acc86b-6e3d-42c3-a9a9-8c05c5339412";
        public string Name => "Plugin name";
        public string Author => "jayfunc";

        public string Description => "Plugin description";

        public void Initialize() { }

        public async Task<LyricsSearchResult> GetLyricsAsync(string title, string artist, string album, double duration)
        {
            await Task.Delay(300);

            string searchedTitle = "Demo Song";
            string searchedArtist = "Demo Artist";
            string searchedAlbum = "Demo Album";
            double searchedDuration = 25.0;

            string searchedRaw =
                $"[00:00.00]Welcome to use Demo Plugin\n" +
                $"[00:05.00]Playing: {title} now\n" +
                $"[00:10.00]Artist: {artist}\n" +
                $"[00:15.00]Album: {album}\n" +
                $"[00:20.00]Duration: {duration}\n" +
                $"[00:25.00]This is a test lyrics source...";

            string searchedTranslation =
                $"[00:00.00]欢迎使用演示插件\n" +
                $"[00:05.00]当前正在播放：{title}\n" +
                $"[00:10.00]歌手：{artist}\n" +
                $"[00:15.00]专辑：{album}\n" +
                $"[00:20.00]时长：{duration}\n" +
                $"[00:25.00]这是一个测试歌词源...";

            string searchedTransliteration =
                $"[00:00.00]ˈwɛlkəm tuː juːz ˈdɛmoʊ ˈplʌgɪn\n" +
                $"[00:05.00]ˈpleɪɪŋ: {title} naʊ\n" +
                $"[00:10.00]ˈɑːrtɪst: {artist}\n" +
                $"[00:15.00]ˈælbəm: {album}\n" +
                $"[00:20.00]dʊˈreɪʃən: {duration}\n" +
                $"[00:25.00]ðɪs ɪz ə tɛst ˈlɪrɪks sɔːrs...";

            string searchedReference = "https://path.to.lyrics/if.the.lyrics.was.originally.fetched.from.web";

            return new LyricsSearchResult(
                searchedTitle,
                searchedArtist,
                searchedAlbum,
                searchedDuration,
                searchedRaw,
                searchedTranslation,
                searchedTransliteration,
                searchedReference);

        }
    }
}