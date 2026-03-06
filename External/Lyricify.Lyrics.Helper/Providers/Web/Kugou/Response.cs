using Newtonsoft.Json;

#nullable disable
namespace Lyricify.Lyrics.Providers.Web.Kugou
{
    public class SearchSongResponse
    {
        public int Status { get; set; }

        public string Error { get; set; }

        public DataItem Data { get; set; }

        public class DataItem
        {
            public int Timestamp { get; set; }

            public int Total { get; set; }

            public List<InfoItem> Info { get; set; }

            public class InfoItem
            {
                public string Hash { get; set; }

                [JsonProperty("songname")]
                public string SongName { get; set; }

                [JsonProperty("album_name")]
                public string AlbumName { get; set; }

                [JsonProperty("songname_original")]
                public string SongNameOriginal { get; set; }

                [JsonProperty("singername")]
                public string SingerName { get; set; }

                public int Duration { get; set; }

                [JsonProperty("filename")]
                public string Filename { get; set; }

                /// <summary>
                /// 歌曲组 (同歌曲的多个版本)
                /// </summary>
                public List<InfoItem> Group { get; set; }
            }
        }

        [JsonProperty("errcode")]
        public int ErrorCode { get; set; }
    }

    public class SearchLyricsResponse
    {
        public int Status { get; set; }

        public string Info { get; set; }

        [JsonProperty("errcode")]
        public int ErrorCode { get; set; }

        [JsonProperty("errmsg")]
        public string ErrorMessage { get; set; }

        public string Keywork { get; set; }

        public string Proposal { get; set; }

        [JsonProperty("has_complete_right")]
        public int HasCompleteRight { get; set; }

        public int Ugc { get; set; }

        [JsonProperty("ugccount")]
        public int UgcCount { get; set; }

        public int Expire { get; set; }

        public List<Candidate> Candidates { get; set; }

        public class Candidate
        {
            public string Id { get; set; }

            [JsonProperty("product_from")]
            public string ProductFrom { get; set; }

            [JsonProperty("accesskey")]
            public string AccessKey { get; set; }

            [JsonProperty("can_score")]
            public bool CanScore { get; set; }

            public string Singer { get; set; }

            public string Song { get; set; }

            public int Duration { get; set; }

            public string Uid { get; set; }

            public string Nickname { get; set; }

            public string Origiuid { get; set; }

            public string Originame { get; set; }

            public string Transuid { get; set; }

            public string Transname { get; set; }

            public string Sounduid { get; set; }

            public string Soundname { get; set; }

            public string Language { get; set; }

            [JsonProperty("krctype")]
            public int KrcType { get; set; }

            public int Hitlayer { get; set; }

            public int Hitcasemask { get; set; }

            public int Adjust { get; set; }

            public int Score { get; set; }

            [JsonProperty("contenttype")]
            public int ContentType { get; set; }

            [JsonProperty("content_format")]
            public int ContentFormat { get; set; }

            [JsonProperty("trans_id")]
            public string TransId { get; set; }
        }
    }
}
