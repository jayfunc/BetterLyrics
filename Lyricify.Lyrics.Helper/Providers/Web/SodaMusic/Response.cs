using Newtonsoft.Json;

#nullable disable
namespace Lyricify.Lyrics.Providers.Web.SodaMusic
{
    /// <summary>
    /// Soda Music 搜索（tracks）接口返回
    /// 仅覆盖当前使用到的字段，未使用字段可按需扩展
    /// </summary>
    public class SearchResult
    {
        [JsonProperty("status_info")]
        public StatusInfo StatusInfo { get; set; }

        [JsonProperty("result_groups")]
        public List<ResultGroup> ResultGroups { get; set; }

        [JsonProperty("extra")]
        public Extra Extra { get; set; }
    }

    public class StatusInfo
    {
        [JsonProperty("log_id")]
        public string LogId { get; set; }

        /// <summary>
        /// unix 秒
        /// </summary>
        [JsonProperty("now")]
        public long Now { get; set; }

        /// <summary>
        /// unix 毫秒
        /// </summary>
        [JsonProperty("now_ts_ms")]
        public long NowTsMs { get; set; }
    }

    public class Extra
    {
        [JsonProperty("log_extra")]
        public string LogExtra { get; set; }
    }

    public class ResultGroup
    {
        public string Id { get; set; }

        [JsonProperty("next_cursor")]
        public string NextCursor { get; set; }

        [JsonProperty("has_more")]
        public bool HasMore { get; set; }

        public List<ResultGroupItem> Data { get; set; }

        [JsonProperty("display_view_all")]
        public bool? DisplayViewAll { get; set; }

        [JsonProperty("display_title")]
        public string DisplayTitle { get; set; }

        public string Description { get; set; }
    }

    public class ResultGroupItem
    {
        public Meta Meta { get; set; }
        public Entity Entity { get; set; }
    }

    public class Meta
    {
        [JsonProperty("item_type")]
        public string ItemType { get; set; }
    }

    public class Entity
    {
        public TrackContainer Track { get; set; }
    }

    public class TrackContainer
    {
        public string Id { get; set; }

        public Album Album { get; set; }

        public List<Artist> Artists { get; set; }

        /// <summary>
        /// 时长(ms)
        /// </summary>
        public long Duration { get; set; }

        public string Name { get; set; }

        public Preview Preview { get; set; }

        public State State { get; set; }

        public Stats Stats { get; set; }

        public string Vid { get; set; }

        [JsonProperty("label_info")]
        public LabelInfo LabelInfo { get; set; }

        [JsonProperty("sim_id")]
        public long? SimId { get; set; }

        /// <summary>
        /// 可选码率集合（文件级）
        /// </summary>
        [JsonProperty("bit_rates")]
        public List<BitRateItem> BitRates { get; set; }

        [JsonProperty("audition_info")]
        public AuditionInfo AuditionInfo { get; set; }

        [JsonProperty("song_maker_team")]
        public SongMakerTeam SongMakerTeam { get; set; }

        [JsonProperty("media_type")]
        public string MediaType { get; set; }

        public bool? Explicit { get; set; }

        public Chorus Chorus { get; set; }

        public Colors Colors { get; set; }

        [JsonProperty("limited_free_info")]
        public object LimitedFreeInfo { get; set; }

        /// <summary>
        /// 1: 有人声; 2: 伴奏/翻唱等
        /// </summary>
        public int? Vocal { get; set; }

        [JsonProperty("lang_codes")]
        public List<string> LangCodes { get; set; }

        [JsonProperty("first_vocal")]
        public Range FirstVocal { get; set; }

        [JsonProperty("sharable_platforms")]
        public List<string> SharablePlatforms { get; set; }

        /// <summary>
        /// 可播放区间（ms）
        /// </summary>
        [JsonProperty("playable_range")]
        public Range PlayableRange { get; set; }

        [JsonProperty("plug_status")]
        public PlugStatus PlugStatus { get; set; }

        public List<TagWrapper> Tags { get; set; }

        public List<Fragment> Fragments { get; set; }
    }

    public class Album
    {
        public string Id { get; set; }

        public string Name { get; set; }

        /// <summary>
        /// unix 秒
        /// </summary>
        [JsonProperty("release_date")]
        public long ReleaseDate { get; set; }

        [JsonProperty("url_cover")]
        public UrlWithTemplate UrlCover { get; set; }

        [JsonProperty("url_player_bg")]
        public UrlWithTemplate UrlPlayerBg { get; set; }

        [JsonProperty("cover_gradient_effect_color")]
        public List<RgbColor> CoverGradientEffectColor { get; set; }

        [JsonProperty("playing_wave_color")]
        public RgbaColor PlayingWaveColor { get; set; }

        [JsonProperty("paused_wave_color")]
        public RgbaColor PausedWaveColor { get; set; }
    }

    public class UrlWithTemplate
    {
        public string Uri { get; set; }

        public List<string> Urls { get; set; }

        [JsonProperty("template")]
        public string Template { get; set; }

        [JsonProperty("template_prefix")]
        public string TemplatePrefix { get; set; }
    }

    public class Artist
    {
        public string Id { get; set; }

        public string Name { get; set; }

        [JsonProperty("url_avatar")]
        public UrlWithTemplate UrlAvatar { get; set; }

        public State State { get; set; }

        [JsonProperty("user_info")]
        public ArtistUserInfo UserInfo { get; set; }

        [JsonProperty("simple_display_name")]
        public string SimpleDisplayName { get; set; }

        [JsonProperty("user_artist_type")]
        public int? UserArtistType { get; set; }
    }

    public class ArtistUserInfo
    {
        public string Id { get; set; }

        public string Nickname { get; set; }

        [JsonProperty("medium_avatar_url")]
        public UrlsOnly MediumAvatarUrl { get; set; }

        [JsonProperty("thumb_avatar_url")]
        public UrlsOnly ThumbAvatarUrl { get; set; }

        [JsonProperty("artist_id")]
        public string ArtistId { get; set; }

        public bool? Secret { get; set; }

        [JsonProperty("test_tag")]
        public int? TestTag { get; set; }

        [JsonProperty("vip_stage")]
        public string VipStage { get; set; }

        [JsonProperty("is_vip")]
        public bool? IsVip { get; set; }
    }

    public class UrlsOnly
    {
        public List<string> Urls { get; set; }

        [JsonProperty("need_complete_url")]
        public bool? NeedCompleteUrl { get; set; }
    }

    public class State
    {
        [JsonProperty("blocked_by_me")]
        public bool? BlockedByMe { get; set; }
    }

    public class Preview
    {
        /// <summary>
        /// 试听时长(ms) 或有时是固定 30047/29002
        /// </summary>
        public int? Duration { get; set; }

        /// <summary>
        /// 试听起点(ms)
        /// </summary>
        public int? Start { get; set; }

        public string Vid { get; set; }

        [JsonProperty("bit_rates")]
        public List<BitRateItem> BitRates { get; set; }
    }

    public class BitRateItem
    {
        /// <summary>
        /// 比特率（br），单位 bps
        /// </summary>
        public long Br { get; set; }

        /// <summary>
        /// 文件大小（字节）
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// 质量标识：medium/higher/highest/lossless/spatial
        /// </summary>
        public string Quality { get; set; }
    }

    public class Stats
    {
        [JsonProperty("count_collected")]
        public long? CountCollected { get; set; }

        [JsonProperty("count_comment")]
        public long? CountComment { get; set; }

        [JsonProperty("count_shared")]
        public long? CountShared { get; set; }
    }

    public class LabelInfo
    {
        [JsonProperty("only_vip_download")]
        public bool? OnlyVipDownload { get; set; }

        [JsonProperty("only_vip_playable")]
        public bool? OnlyVipPlayable { get; set; }

        [JsonProperty("quality_only_vip_can_download")]
        public List<string> QualityOnlyVipCanDownload { get; set; }

        [JsonProperty("quality_only_vip_can_play")]
        public List<string> QualityOnlyVipCanPlay { get; set; }

        [JsonProperty("is_original")]
        public bool? IsOriginal { get; set; }

        [JsonProperty("quality_map")]
        public Dictionary<string, QualityDetailWrap> QualityMap { get; set; }
    }

    public class QualityDetailWrap
    {
        [JsonProperty("play_detail")]
        public QualityDetail PlayDetail { get; set; }

        [JsonProperty("download_detail")]
        public QualityDetail DownloadDetail { get; set; }
    }

    public class QualityDetail
    {
        public string Condition { get; set; }

        [JsonProperty("need_vip")]
        public bool? NeedVip { get; set; }

        [JsonProperty("need_purchase")]
        public bool? NeedPurchase { get; set; }
    }

    public class AuditionInfo
    {
        public string Vid { get; set; }

        [JsonProperty("start_time_ms")]
        public int? StartTimeMs { get; set; }

        [JsonProperty("duration_ms")]
        public int? DurationMs { get; set; }
    }

    public class SongMakerTeam
    {
        public List<NameOnly> Composers { get; set; }
        public List<NameOnly> Lyricists { get; set; }
    }

    public class NameOnly
    {
        public string Name { get; set; }
    }

    public class Chorus
    {
        /// <summary>
        /// 开始(ms)
        /// </summary>
        public int? Start { get; set; }

        /// <summary>
        /// 时长(ms)
        /// </summary>
        public int? Duration { get; set; }
    }

    public class Colors
    {
        [JsonProperty("cover_gradient_effect_color")]
        public List<RgbColor> CoverGradientEffectColor { get; set; }

        [JsonProperty("normal_lyric_color")]
        public RgbaColor NormalLyricColor { get; set; }

        [JsonProperty("playing_lyric_color")]
        public RgbaColor PlayingLyricColor { get; set; }

        [JsonProperty("recommend_reason_background_color")]
        public RgbaColor RecommendReasonBackgroundColor { get; set; }

        [JsonProperty("featured_comment_tag_color")]
        public RgbaColor FeaturedCommentTagColor { get; set; }

        [JsonProperty("background_color")]
        public RgbaColor BackgroundColor { get; set; }

        [JsonProperty("playing_wave_color")]
        public RgbaColor PlayingWaveColor { get; set; }

        [JsonProperty("paused_wave_color")]
        public RgbaColor PausedWaveColor { get; set; }

        [JsonProperty("comment_share_additional_color")]
        public RgbaColor CommentShareAdditionalColor { get; set; }

        [JsonProperty("base_colors")]
        public List<RgbColor> BaseColors { get; set; }

        [JsonProperty("non_interactive_anchor_background")]
        public RgbaColor NonInteractiveAnchorBackground { get; set; }
    }

    public class RgbColor
    {
        public string Rgb { get; set; }
    }

    public class RgbaColor : RgbColor
    {
        public string Alpha { get; set; }
    }

    public class Range
    {
        /// <summary>
        /// 开始(ms)
        /// </summary>
        public int? Start { get; set; }

        /// <summary>
        /// 时长(ms)
        /// </summary>
        public int? Duration { get; set; }
    }

    public class PlugStatus
    {
        [JsonProperty("can_plug")]
        public bool? CanPlug { get; set; }

        [JsonProperty("is_plugged")]
        public bool? IsPlugged { get; set; }
    }

    public class TagWrapper
    {
        public TagCategory Category { get; set; }

        [JsonProperty("first_level_tag")]
        public Tag FirstLevelTag { get; set; }

        [JsonProperty("second_level_tag")]
        public Tag SecondLevelTag { get; set; }
    }

    public class TagCategory
    {
        [JsonProperty("tag_id")]
        public long TagId { get; set; }

        [JsonProperty("tag_name")]
        public string TagName { get; set; }
    }

    public class Tag
    {
        [JsonProperty("tag_id")]
        public long TagId { get; set; }

        [JsonProperty("tag_name")]
        public string TagName { get; set; }
    }

    public class Fragment
    {
        public string Type { get; set; }

        [JsonProperty("start_time")]
        public int? StartTime { get; set; }

        [JsonProperty("end_time")]
        public int? EndTime { get; set; }
    }

    /// <summary>
    /// Soda Music track_v2 接口返回（获取乐曲具体信息）
    /// </summary>
    public class TrackDetailResult
    {
        [JsonProperty("status_info")]
        public StatusInfo StatusInfo { get; set; }

        public LyricInfo Lyric { get; set; }

        public TrackInfo Track { get; set; }

        [JsonProperty("track_player")]
        public TrackPlayer TrackPlayer { get; set; }

        [JsonProperty("risk_result")]
        public int? RiskResult { get; set; }

        [JsonProperty("expire_at")]
        public long? ExpireAt { get; set; }
    }

    public class LyricInfo
    {
        public string Content { get; set; }

        public string Lang { get; set; }

        [JsonProperty("hide_request_lyrics")]
        public bool? HideRequestLyrics { get; set; }

        public string Type { get; set; }

        [JsonProperty("lyric_contributor")]
        public LyricContributor LyricContributor { get; set; }

        public string Id { get; set; }

        [JsonProperty("lang_translations")]
        public Dictionary<string, LyricTranslation> LangTranslations { get; set; }
    }

    public class LyricContributor
    {
        [JsonProperty("filter_reason")]
        public string FilterReason { get; set; }
    }

    public class LyricTranslation
    {
        public string Content { get; set; }

        public string Lang { get; set; }

        [JsonProperty("hide_request_lyrics")]
        public bool? HideRequestLyrics { get; set; }

        public string Type { get; set; }

        [JsonProperty("lyric_contributor")]
        public LyricContributor LyricContributor { get; set; }

        public string Id { get; set; }
    }

    public class TrackInfo
    {
        public string Id { get; set; }

        public Album Album { get; set; }

        public List<Artist> Artists { get; set; }

        /// <summary>
        /// 时长(ms)
        /// </summary>
        public long Duration { get; set; }

        public string Name { get; set; }

        public Preview Preview { get; set; }

        public State State { get; set; }

        public Stats Stats { get; set; }

        public string Vid { get; set; }

        [JsonProperty("label_info")]
        public LabelInfo LabelInfo { get; set; }

        [JsonProperty("sim_id")]
        public long? SimId { get; set; }

        [JsonProperty("bit_rates")]
        public List<BitRateItem> BitRates { get; set; }

        [JsonProperty("audition_info")]
        public AuditionInfo AuditionInfo { get; set; }

        [JsonProperty("song_maker_team")]
        public SongMakerTeam SongMakerTeam { get; set; }

        [JsonProperty("media_type")]
        public string MediaType { get; set; }

        public Chorus Chorus { get; set; }

        public Colors Colors { get; set; }

        [JsonProperty("limited_free_info")]
        public LimitedFreeInfo LimitedFreeInfo { get; set; }

        public int? Vocal { get; set; }

        [JsonProperty("lang_codes")]
        public List<string> LangCodes { get; set; }

        [JsonProperty("first_vocal")]
        public Range FirstVocal { get; set; }

        [JsonProperty("sharable_platforms")]
        public List<string> SharablePlatforms { get; set; }

        [JsonProperty("plug_status")]
        public PlugStatus PlugStatus { get; set; }

        public List<TagWrapper> Tags { get; set; }
    }

    public class LimitedFreeInfo
    {
        [JsonProperty("queue_types")]
        public List<string> QueueTypes { get; set; }

        [JsonProperty("expire_time")]
        public long? ExpireTime { get; set; }

        public string Sign { get; set; }

        [JsonProperty("sign_version")]
        public string SignVersion { get; set; }

        [JsonProperty("limited_free_type")]
        public string LimitedFreeType { get; set; }

        [JsonProperty("rewind_prev_intercept_type")]
        public string RewindPrevInterceptType { get; set; }

        [JsonProperty("intercept_type")]
        public string InterceptType { get; set; }
    }

    public class TrackPlayer
    {
        [JsonProperty("expire_at")]
        public long? ExpireAt { get; set; }

        [JsonProperty("media_id")]
        public string MediaId { get; set; }

        [JsonProperty("url_player_info")]
        public string UrlPlayerInfo { get; set; }

        [JsonProperty("video_model")]
        public string VideoModel { get; set; }

        [JsonProperty("video_model_type")]
        public int? VideoModelType { get; set; }
    }
}
