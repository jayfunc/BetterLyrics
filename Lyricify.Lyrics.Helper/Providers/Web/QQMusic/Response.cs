using Newtonsoft.Json;
using System.Text;

#nullable disable
namespace Lyricify.Lyrics.Providers.Web.QQMusic
{
    public class MusicFcgApiResult
    {
        public long Code { get; set; }

        public long Ts { get; set; }

        public long StartTs { get; set; }

        public string Traceid { get; set; }

        public MusicFcgReq Req { get; set; }

        public MusicFcgReq0 Req_0 { get; set; }

        public MusicFcgReq1 Req_1 { get; set; }

        public class MusicFcgReq
        {
            public long Code { get; set; }

            public MusicFcgReqData Data { get; set; }

            public class MusicFcgReqData
            {
                public string[] Sip { get; set; }

                public string Keepalivefile { get; set; }

                public string Testfile2g { get; set; }

                public string Testfilewifi { get; set; }
            }
        }

        public class MusicFcgReq0
        {
            public long Code { get; set; }

            public MusicFcgReq0Data Data { get; set; }

            public class MusicFcgReq0Data
            {
                public string[] Sip { get; set; }

                public string Testfile2g { get; set; }

                public string Testfilewifi { get; set; }

                public MusicFcgReq0DataMidurlinfo[] Midurlinfo { get; set; }
            }

            public class MusicFcgReq0DataMidurlinfo
            {
                public string Songmid { get; set; }

                public string Purl { get; set; }
            }
        }

        public class MusicFcgReq1
        {
            public long Code { get; set; }

            public MusicFcgReq1Data Data { get; set; }

            public class MusicFcgReq1Data
            {
                public long Code { get; set; }

                public long Ver { get; set; }

                public MusicFcgReq1DataBody Body { get; set; }

                public MusicFcgReq1DataMeta Meta { get; set; }

                public class MusicFcgReq1DataBody
                {
                    /// <summary>
                    /// 专辑查询结果
                    /// </summary>
                    public MusicFcgReq1DataBodyAlbum Album { get; set; }

                    /// <summary>
                    /// 单曲查询结果
                    /// </summary>
                    public MusicFcgReq1DataBodySong Song { get; set; }

                    /// <summary>
                    /// 歌单查询结果
                    /// </summary>
                    public MusicFcgReq1DataBodyPlayList Songlist { get; set; }

                    public class MusicFcgReq1DataBodyAlbum
                    {
                        public MusicFcgReq1DataBodyAlbumInfo[] List { get; set; }

                        public class MusicFcgReq1DataBodyAlbumInfo
                        {
                            public long AlbumID { get; set; }

                            public string AlbumMID { get; set; }

                            /// <summary>
                            /// 专辑名
                            /// </summary>
                            public string AlbumName { get; set; }

                            public long Song_count { get; set; }

                            /// <summary>
                            /// 发布时间 eg 2011-04-27
                            /// </summary>
                            public string PublicTime { get; set; }

                            public Singer[] Singer_list { get; set; }
                        }
                    }

                    public class MusicFcgReq1DataBodySong
                    {
                        public Song[] List { get; set; }
                    }

                    public class MusicFcgReq1DataBodyPlayList
                    {
                        public MusicFcgReq1DataBodyPlayListInfo[] List { get; set; }

                        public class MusicFcgReq1DataBodyPlayListInfo
                        {
                            /// <summary>
                            /// 歌单 ID
                            /// </summary>
                            public string Dissid { get; set; }

                            /// <summary>
                            /// 歌单名
                            /// </summary>
                            public string Dissname { get; set; }

                            /// <summary>
                            /// 歌单封面
                            /// </summary>
                            public string Imgurl { get; set; }

                            /// <summary>
                            /// 歌单介绍
                            /// </summary>
                            public string Introduction { get; set; }

                            /// <summary>
                            /// 歌单数量
                            /// </summary>
                            public long Song_Count { get; set; }

                            /// <summary>
                            /// 歌单播放量
                            /// </summary>
                            public long Listennum { get; set; }

                            public MusicFcgReq1DataBodyPlayListCreator Creator { get; set; }
                        }

                        public class MusicFcgReq1DataBodyPlayListCreator
                        {
                            public string Name { get; set; }

                            public long Qq { get; set; }
                        }
                    }
                }

                public class MusicFcgReq1DataMeta
                {
                    /// <summary>
                    /// 当前页数
                    /// </summary>
                    public long Curpage { get; set; }

                    /// <summary>
                    /// 下一页
                    /// </summary>
                    public long Nextpage { get; set; }

                    /// <summary>
                    /// 每页数量
                    /// </summary>
                    public long Perpage { get; set; }

                    /// <summary>
                    /// 查询关键字
                    /// </summary>
                    public string Query { get; set; }

                    /// <summary>
                    /// 累计数量
                    /// </summary>
                    public long Sum { get; set; }
                }
            }
        }
    }

    public class MusicFcgApiAlternativeResult
    {
        public long Code { get; set; }

        public long Ts { get; set; }

        public long StartTs { get; set; }

        public string Traceid { get; set; }

        [JsonProperty("music.search.SearchCgiService")]
        public SearchCgiService Search { get; set; }

        public class SearchCgiService
        {
            public DataBody Data { get; set; }
        }

        public class DataBody
        {
            public SearchData Body { get; set; }
        }

        public class SearchData
        {
            public Song Song { get; set; }
        }

        public class Song
        {
            public QQMusic.Song[] List { get; set; }
        }
    }

    /// <summary>
    /// 专辑查询接口返回结果
    /// </summary>
    public class AlbumResult
    {
        public long Code { get; set; }

        public AlbumInfo Data { get; set; }

        public string Message { get; set; }
    }

    /// <summary>
    /// 歌单查询接口返回结果
    /// </summary>
    public class PlaylistResult
    {
        public long Code { get; set; }

        public Playlist[] Cdlist { get; set; }
    }

    /// <summary>
    /// 歌曲查询接口返回结果
    /// </summary>
    public class SongResult
    {
        public long Code { get; set; }

        public Song[] Data { get; set; }

        public bool IsIllegal()
        {
            return Code != 0 || Data.Length == 0;
        }
    }

    /// <summary>
    /// 歌词查询接口返回结果
    /// </summary>
    public class LyricResult
    {
        public long Code { get; set; }

        public string Lyric { get; set; }

        public string Trans { get; set; }

        public LyricResult Decode()
        {
            if (Lyric != null)
            {
                Lyric = Encoding.UTF8.GetString(Convert.FromBase64String(Lyric));
            }

            if (Trans != null)
            {
                Trans = Encoding.UTF8.GetString(Convert.FromBase64String(Trans));
            }

            return this;
        }
    }

    public class Playlist
    {
        /// <summary>
        /// 歌单 ID
        /// </summary>
        public string Disstid { get; set; }

        /// <summary>
        /// 歌单名
        /// </summary>
        public string Dissname { get; set; }

        /// <summary>
        /// 作者名
        /// </summary>
        public string Nickname { get; set; }

        /// <summary>
        /// 歌单封面
        /// </summary>
        public string Logo { get; set; }

        /// <summary>
        /// 歌单描述
        /// </summary>
        public string Desc { get; set; }

        /// <summary>
        /// 标签
        /// </summary>
        public Tag[] Tags { get; set; }

        /// <summary>
        /// 歌曲数量
        /// </summary>
        public long Songnum { get; set; }

        /// <summary>
        /// 歌曲 ID 列表，使用英文逗号分割
        /// </summary>
        public string Songids { get; set; }

        /// <summary>
        /// 歌曲列表
        /// </summary>
        public Song[] SongList { get; set; }

        /// <summary>
        /// 播放量
        /// </summary>
        public long Visitnum { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public long CTime { get; set; }
    }

    public class Tag
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public long Pid { get; set; }
    }

    public class Song
    {
        /// <summary>
        /// 所属专辑
        /// </summary>
        public Album Album { get; set; }

        public string Id { get; set; }

        /// <summary>
        /// 时长，单位 s
        /// </summary>
        public int Interval { get; set; }

        public string Mid { get; set; }

        /// <summary>
        /// 歌曲名
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 歌曲描述
        /// </summary>
        public string Desc { get; set; }

        public Singer[] Singer { get; set; }

        /// <summary>
        /// 标题
        /// </summary>
        public string Title { get; set; }

        public string Subtitle { get; set; }

        /// <summary>
        /// 发布时间，eg: 2005-07-08
        /// </summary>
        public string Time_public { get; set; }

        /// <summary>
        /// 同版本的曲目
        /// </summary>
        [JsonProperty("grp")]
        public List<Song> Group { get; set; }
        public int Language { get; set; }
        public int Genre { get; set; }
    }

    public class AlbumInfo
    {
        /// <summary>
        /// 专辑时间，eg 2022-03-28
        /// </summary>
        public string ADate { get; set; }

        /// <summary>
        /// 发行公司
        /// </summary>
        public string Company { get; set; }

        /// <summary>
        /// 专辑描述
        /// </summary>
        public string Desc { get; set; }

        public long Id { get; set; }

        public string Mid { get; set; }

        /// <summary>
        /// 专辑语言，eg 韩语
        /// </summary>
        public string Lan { get; set; }

        public AlbumSong[] List { get; set; }

        /// <summary>
        /// 专辑名
        /// </summary>
        public string Name { get; set; }

        public long Singerid { get; set; }

        public string Singermid { get; set; }

        public string Singername { get; set; }

        /// <summary>
        /// 包含的歌曲数量
        /// </summary>
        public int Total { get; set; }

        public class AlbumSong
        {
            public Singer[] Singer { get; set; }

            public long Songid { get; set; }

            public string Songmid { get; set; }

            public string Songname { get; set; }
        }
    }

    public class SingerSongResult
    {
        public int Code { get; set; }
        public long Ts { get; set; }
        public long Start_Ts { get; set; }
        public string Traceid { get; set; }
        public SingerData Singer { get; set; }

        public class SingerData
        {
            public int Code { get; set; }
            public SingerInfo Data { get; set; }
        }

        public class SingerInfo
        {
            public List<SongInfo> Songlist { get; set; }
            public string Singer_brief { get; set; }
            public List<object> Music_grp { get; set; }
            public int Total_album { get; set; }
            public int Total_mv { get; set; }
            public int Total_song { get; set; }
            public string Yinyueren { get; set; }
            public bool Show_singer_desc { get; set; }
        }
    }

    public class ToplistResult
    {
        public int Code { get; set; }
        public long Ts { get; set; }
        public long Start_Ts { get; set; }
        public string Traceid { get; set; }
        public DetailData Detail { get; set; }

        public class DetailData
        {
            public int Code { get; set; }
            public ToplistData Data { get; set; }
        }

        public class ToplistData
        {
            public ToplistInfo Data { get; set; }
            public List<object> SongInfoList { get; set; }
            public List<object> ExtInfoList { get; set; }
            public object SongTagInfoList { get; set; }
            public object IndexInfoList { get; set; }
        }

        public class ToplistInfo
        {
            public int TopId { get; set; }
            public int RecType { get; set; }
            public int TopType { get; set; }
            public int UpdateType { get; set; }
            public string Title { get; set; }
            public string TitleDetail { get; set; }
            public string TitleShare { get; set; }
            public string TitleSub { get; set; }
            public string Intro { get; set; }
            public int CornerMark { get; set; }
            public string Period { get; set; }
            public string UpdateTime { get; set; }
            public HistoryData History { get; set; }
            public int ListenNum { get; set; }
            public int TotalNum { get; set; }
            public List<SongData> Song { get; set; }
            public string HeadPicUrl { get; set; }
            public string FrontPicUrl { get; set; }
            public string MbFrontPicUrl { get; set; }
            public string MbHeadPicUrl { get; set; }
            public List<object> PcSubTopIds { get; set; }
            public List<object> PcSubTopTitles { get; set; }
            public List<object> SubTopIds { get; set; }
            public string AdJumpUrl { get; set; }
            public string H5JumpUrl { get; set; }
            public string Url_Key { get; set; }
            public string Url_Params { get; set; }
            public string Tjreport { get; set; }
            public int Rt { get; set; }
            public string UpdateTips { get; set; }
            public string BannerText { get; set; }
            public string AdShareContent { get; set; }
            public string Abt { get; set; }
            public int CityId { get; set; }
            public int ProvId { get; set; }
            public int SinceCV { get; set; }
            public string MusichallTitle { get; set; }
            public string MusichallSubtitle { get; set; }
            public string MusichallPicUrl { get; set; }
            public string SpecialScheme { get; set; }
            public string MbFrontLogoUrl { get; set; }
            public string MbHeadLogoUrl { get; set; }
            public string CityName { get; set; }
            public MagicColor MagicColor { get; set; }
            public string TopAlbumURL { get; set; }
            public int GroupType { get; set; }
            public int Icon { get; set; }
            public int AdID { get; set; }
            public string MbIntroWebUrl { get; set; }
            public string MbLogoUrl { get; set; }
        }

        public class HistoryData
        {
            public List<object> Year { get; set; }
            public List<object> SubPeriod { get; set; }
        }

        public class SongData
        {
            public int Rank { get; set; }
            public int RankType { get; set; }
            public string RankValue { get; set; }
            public int RecType { get; set; }
            public int SongId { get; set; }
            public string Vid { get; set; }
            public string AlbumMid { get; set; }
            public string Title { get; set; }
            public string SingerName { get; set; }
            public string SingerMid { get; set; }
            public int SongType { get; set; }
            public int UuidCnt { get; set; }
            public string Cover { get; set; }
            public int Mvid { get; set; }
        }

        public class MagicColor
        {
            public int R { get; set; }
            public int G { get; set; }
            public int B { get; set; }
        }
    }

    public class AlbumSongListResult
    {
        public int Code { get; set; }
        public long Ts { get; set; }
        public long Start_ts { get; set; }
        public string TraceId { get; set; }
        public AlbumSonglistInfo AlbumSonglist { get; set; }

        public class AlbumSonglistInfo
        {
            public int Code { get; set; }
            public DataInfo Data { get; set; }
        }

        public class DataInfo
        {
            public string AlbumMid { get; set; }
            public int TotalNum { get; set; }
            public List<SongItem> SongList { get; set; }
            public List<object> ClassicList { get; set; }
            public int Sort { get; set; }
            public string AlbumTips { get; set; }
            public int Index { get; set; }
            public int ScheduleStatus { get; set; }
            public int CurBegin { get; set; }
            public int CdNewStyle { get; set; }
            public object CdNameMap { get; set; }
        }

        public class SongItem
        {
            public SongInfo SongInfo { get; set; }
            public int ListenCount { get; set; }
            public string UploadTime { get; set; }
            public int IsThemeSong { get; set; }
            public string TeamStr { get; set; }
        }

        public class SongInfo
        {
            public int Id { get; set; }
            public int Type { get; set; }
            public string Mid { get; set; }
            public string Name { get; set; }
            public string Title { get; set; }
            public string Subtitle { get; set; }
            public List<Singer> Singer { get; set; }
            public Album Album { get; set; }
            public Mv MV { get; set; }
            public int Interval { get; set; }
            public int IsOnly { get; set; }
            public int Language { get; set; }
            public int Genre { get; set; }
            public int Index_cd { get; set; }
            public int Index_album { get; set; }
            public string Time_public { get; set; }
            public int Status { get; set; }
            public int Fnote { get; set; }
            public FileInfo File { get; set; }
            public Pay Pay { get; set; }
            public Action Action { get; set; }
            public Ksong KSong { get; set; }
            public Volume Volume { get; set; }
            public int Label { get; set; }
            public string Url { get; set; }
            public int Bpm { get; set; }
            public int Version { get; set; }
            public string Trace { get; set; }
            public int Data_Type { get; set; }
            public int Modify_stamp { get; set; }
            public string PingPong { get; set; }
            public int Aid { get; set; }
            public string PpUrl { get; set; }
            public int Tid { get; set; }
            public int Ov { get; set; }
            public int Sa { get; set; }
            public string Es { get; set; }
            public List<string> Vs { get; set; }
            public List<int> Vi { get; set; }
            public string KTag { get; set; }
        }
    }

    public class SongInfo
    {
        public int Id { get; set; }
        public int Type { get; set; }
        public string Mid { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public List<Singer> Singer { get; set; }
        public Album Album { get; set; }
        public Mv Mv { get; set; }
        public int Interval { get; set; }
        public int Isonly { get; set; }
        public int Language { get; set; }
        public int Genre { get; set; }
        public int Index_cd { get; set; }
        public int Index_album { get; set; }
        public string Time_public { get; set; }
        public int Status { get; set; }
        public int Fnote { get; set; }
        public FileInfo File { get; set; }
        public Pay Pay { get; set; }
        public Action Action { get; set; }
        public Ksong Ksong { get; set; }
        public Volume Volume { get; set; }
        public string Label { get; set; }
        public string Url { get; set; }
        public int Bpm { get; set; }
        public int Version { get; set; }
        public string Trace { get; set; }
        public int Data_type { get; set; }
        public int Modify_stamp { get; set; }
        public string Pingpong { get; set; }
        public string Ppurl { get; set; }
        public int Tid { get; set; }
        public int Ov { get; set; }
    }

    public class Singer
    {
        public long Id { get; set; }
        public string Mid { get; set; }
        public string Name { get; set; }
        public string Pmid { get; set; }
        public string Title { get; set; }
        public int Type { get; set; }
    }

    public class Album
    {
        public int Id { get; set; }
        public string Mid { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public string Time_public { get; set; }
        public string Pmid { get; set; }
    }

    public class Mv
    {
        public int Id { get; set; }
        public string Vid { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public int Vt { get; set; }
    }

    public class FileInfo
    {
        public string Media_mid { get; set; }
        public int Size_24aac { get; set; }
        public int Size_48aac { get; set; }
        public int Size_96aac { get; set; }
        public int Size_192ogg { get; set; }
        public int Size_192aac { get; set; }
        public int Size_128mp3 { get; set; }
        public int Size_320mp3 { get; set; }
        public int Size_ape { get; set; }
        public int Size_flac { get; set; }
        public int Size_dts { get; set; }
        public int Size_try { get; set; }
        public int Try_begin { get; set; }
        public int Try_end { get; set; }
        public string Url { get; set; }
        public int Size_hires { get; set; }
        public int Hires_sample { get; set; }
        public int Hires_bitdepth { get; set; }
        public int B_30s { get; set; }
        public int E_30s { get; set; }
        public int Size_96ogg { get; set; }
    }

    public class Pay
    {
        public int Pay_month { get; set; }
        public int Price_track { get; set; }
        public int Price_album { get; set; }
        public int Pay_play { get; set; }
        public int Pay_down { get; set; }
        public int Pay_status { get; set; }
        public int Time_free { get; set; }
    }

    public class Action
    {
        public int Switch { get; set; }
        public int Msgid { get; set; }
        public int Alert { get; set; }
        public int Icons { get; set; }
        public int Msgshare { get; set; }
        public int Msgfav { get; set; }
        public int Msgdown { get; set; }
        public int Msgpay { get; set; }
    }

    public class Ksong
    {
        public int Id { get; set; }
        public string Mid { get; set; }
    }

    public class Volume
    {
        public float Gain { get; set; }
        public float Peak { get; set; }
        public float Lra { get; set; }
    }
}
