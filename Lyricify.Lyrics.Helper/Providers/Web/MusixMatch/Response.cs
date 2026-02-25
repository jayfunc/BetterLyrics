using Newtonsoft.Json;

#nullable disable
namespace Lyricify.Lyrics.Providers.Web.Musixmatch
{
    public class GetTokenResponse
    {
        [JsonProperty("message")]
        public MessageContent Message { get; set; }

        public class MessageContent
        {
            [JsonProperty("header")]
            public Header Header { get; set; }

            [JsonProperty("body")]
            public Body Body { get; set; }
        }

        public class Header
        {
            [JsonProperty("status_code")]
            public int StatusCode { get; set; }

            [JsonProperty("execute_time")]
            public double ExecuteTime { get; set; }

            [JsonProperty("pid")]
            public int Pid { get; set; }

            [JsonProperty("surrogate_key_list")]
            public object[] SurrogateKeyList { get; set; }

            [JsonProperty("hint")]
            public string Hint { get; set; }
        }

        public class Body
        {
            [JsonProperty("user_token")]
            public string UserToken { get; set; }

            [JsonProperty("app_config")]
            public AppConfig AppConfig { get; set; }

            [JsonProperty("location")]
            public Location Location { get; set; }
        }

        public class AppConfig
        {
            [JsonProperty("trial")]
            public bool Trial { get; set; }

            [JsonProperty("mobilePopOverMaximum")]
            public int MobilePopOverMaximum { get; set; }

            [JsonProperty("mobilePopOverMinTimes")]
            public int MobilePopOverMinTimes { get; set; }

            [JsonProperty("mobilePopOverMaxTimes")]
            public int MobilePopOverMaxTimes { get; set; }

            [JsonProperty("isRoviCopyEnabled")]
            public bool IsRoviCopyEnabled { get; set; }

            [JsonProperty("isGettyCopyEnabled")]
            public bool IsGettyCopyEnabled { get; set; }

            [JsonProperty("searchMaxResults")]
            public int SearchMaxResults { get; set; }

            [JsonProperty("fbShareUrlSpotify")]
            public bool FbShareUrlSpotify { get; set; }

            [JsonProperty("twShareUrlSpotify")]
            public bool TwShareUrlSpotify { get; set; }

            [JsonProperty("fbPostTimeline")]
            public bool FbPostTimeline { get; set; }

            [JsonProperty("fbOpenGraph")]
            public bool FbOpenGraph { get; set; }

            [JsonProperty("subtitlesMaxDeviation")]
            public int SubtitlesMaxDeviation { get; set; }

            [JsonProperty("localeDefaultLang")]
            public string LocaleDefaultLang { get; set; }

            [JsonProperty("missionEnable")]
            public bool MissionEnable { get; set; }

            [JsonProperty("content_team")]
            public bool ContentTeam { get; set; }

            [JsonProperty("missions")]
            public Missions Missions { get; set; }

            [JsonProperty("mission_manager_categories")]
            public MissionManagerCategories MissionManagerCategories { get; set; }

            [JsonProperty("appstore_products")]
            public object[] AppstoreProducts { get; set; }

            [JsonProperty("tracking_list")]
            public TrackingList[] TrackingList { get; set; }

            [JsonProperty("spotifyCountries")]
            public string[] SpotifyCountries { get; set; }

            [JsonProperty("service_list")]
            public ServiceList[] ServiceList { get; set; }

            [JsonProperty("show_amazon_music")]
            public bool ShowAmazonMusic { get; set; }

            [JsonProperty("isSentryEnabled")]
            public bool IsSentryEnabled { get; set; }

            [JsonProperty("languages")]
            public string[] Languages { get; set; }

            [JsonProperty("last_updated")]
            public DateTime LastUpdated { get; set; }

            [JsonProperty("cluster")]
            public string Cluster { get; set; }

            [JsonProperty("event_map")]
            public EventMap[] EventMap { get; set; }

            [JsonProperty("mission_report_types")]
            public string[] MissionReportTypes { get; set; }

            [JsonProperty("can_show_performers_tag")]
            public bool CanShowPerformersTag { get; set; }

            [JsonProperty("smart_translations")]
            public SmartTranslations SmartTranslations { get; set; }

            [JsonProperty("audio_upload")]
            public bool AudioUpload { get; set; }
        }

        public class Missions
        {
            [JsonProperty("enabled")]
            public bool Enabled { get; set; }

            [JsonProperty("community")]
            public string[] Community { get; set; }
        }

        public class MissionManagerCategories
        {
            [JsonProperty("taskTypes")]
            public TaskType[] TaskTypes { get; set; }

            [JsonProperty("audioSources")]
            public AudioSource[] AudioSources { get; set; }

            [JsonProperty("userGroups")]
            public UserGroup[] UserGroups { get; set; }

            [JsonProperty("rewards")]
            public Reward[] Rewards { get; set; }
        }

        public class TaskType
        {
            [JsonProperty("value")]
            public string Value { get; set; }

            [JsonProperty("label")]
            public string Label { get; set; }
        }

        public class AudioSource
        {
            [JsonProperty("value")]
            public string Value { get; set; }

            [JsonProperty("label")]
            public string Label { get; set; }
        }

        public class UserGroup
        {
            [JsonProperty("value")]
            public string Value { get; set; }

            [JsonProperty("label")]
            public string Label { get; set; }
        }

        public class Reward
        {
            [JsonProperty("label")]
            public string Label { get; set; }

            [JsonProperty("value")]
            public string Value { get; set; }
        }

        public class TrackingList
        {
            [JsonProperty("tracking")]
            public Tracking Tracking { get; set; }
        }

        public class Tracking
        {
            [JsonProperty("context")]
            public string Context { get; set; }

            [JsonProperty("delay")]
            public int Delay { get; set; }

            [JsonProperty("track_cache_ttl")]
            public int TrackCacheTtl { get; set; }
        }

        public class ServiceList
        {
            [JsonProperty("service")]
            public Service Service { get; set; }
        }

        public class Service
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("display_name")]
            public string DisplayName { get; set; }

            [JsonProperty("type")]
            public string Type { get; set; }

            [JsonProperty("oauth_api")]
            public bool OauthApi { get; set; }

            [JsonProperty("oauth_web_signin")]
            public OauthWebSignin OauthWebSignin { get; set; }

            [JsonProperty("streaming")]
            public bool Streaming { get; set; }

            [JsonProperty("playlist")]
            public bool Playlist { get; set; }

            [JsonProperty("locker")]
            public bool Locker { get; set; }

            [JsonProperty("deeplink")]
            public bool Deeplink { get; set; }
        }

        public class OauthWebSignin
        {
            [JsonProperty("enabled")]
            public bool Enabled { get; set; }

            [JsonProperty("user_prefix")]
            public string UserPrefix { get; set; }
        }

        public class EventMap
        {
            [JsonProperty("regex")]
            public string Regex { get; set; }

            [JsonProperty("enabled")]
            public bool Enabled { get; set; }

            [JsonProperty("piggyback")]
            public Piggyback Piggyback { get; set; }
        }

        public class Piggyback
        {
            [JsonProperty("server_weight")]
            public int ServerWeight { get; set; }
        }

        public class SmartTranslations
        {
            [JsonProperty("enabled")]
            public bool Enabled { get; set; }

            [JsonProperty("threshold")]
            public int Threshold { get; set; }
        }

        public class Location
        {
            [JsonProperty("GEOIP_CITY_COUNTRY_CODE")]
            public string GeoIPCityCountryCode { get; set; }

            [JsonProperty("GEOIP_CITY_COUNTRY_CODE3")]
            public string GeoIPCityCountryCode3 { get; set; }

            [JsonProperty("GEOIP_CITY_COUNTRY_NAME")]
            public string GeoIPCityCountryName { get; set; }

            [JsonProperty("GEOIP_CITY")]
            public string GeoIPCity { get; set; }

            [JsonProperty("GEOIP_CITY_CONTINENT_CODE")]
            public string GeoIPCityContinentCode { get; set; }

            [JsonProperty("GEOIP_LATITUDE")]
            public double GeoIPLatitude { get; set; }

            [JsonProperty("GEOIP_LONGITUDE")]
            public double GeoIPLongitude { get; set; }

            [JsonProperty("GEOIP_AS_ORG")]
            public string GeoIPAsOrg { get; set; }

            [JsonProperty("GEOIP_ORG")]
            public string GeoIPOrg { get; set; }

            [JsonProperty("GEOIP_ISP")]
            public string GeoIPIsp { get; set; }

            [JsonProperty("GEOIP_NET_NAME")]
            public string GeoIPNetName { get; set; }

            [JsonProperty("BADIP_TAGS")]
            public object[] BadipTags { get; set; }
        }
    }

    public class GetTrackResponse
    {
        [JsonProperty("message")]
        public MessageContent Message { get; set; }

        public class MessageContent
        {
            [JsonProperty("header")]
            public Header Header { get; set; }

            [JsonProperty("body")]
            public Body Body { get; set; }
        }

        public class Header
        {
            [JsonProperty("status_code")]
            public int StatusCode { get; set; }

            [JsonProperty("execute_time")]
            public double ExecuteTime { get; set; }

            [JsonProperty("confidence")]
            public int Confidence { get; set; }

            [JsonProperty("mode")]
            public string Mode { get; set; }

            [JsonProperty("cached")]
            public int Cached { get; set; }

            [JsonProperty("hint")]
            public string Hint { get; set; }
        }

        public class Body
        {
            [JsonProperty("track")]
            public Track Track { get; set; }
        }

        public class Track
        {
            [JsonProperty("track_id")]
            public int TrackId { get; set; }

            [JsonProperty("track_mbid")]
            public string TrackMbid { get; set; }

            [JsonProperty("track_isrc")]
            public string TrackIsrc { get; set; }

            [JsonProperty("commontrack_isrcs")]
            public List<List<string>> CommontrackIsrcs { get; set; }

            [JsonProperty("track_spotify_id")]
            public string TrackSpotifyId { get; set; }

            [JsonProperty("commontrack_spotify_ids")]
            public List<string> CommontrackSpotifyIds { get; set; }

            [JsonProperty("track_soundcloud_id")]
            public int TrackSoundcloudId { get; set; }

            [JsonProperty("track_xboxmusic_id")]
            public string TrackXboxmusicId { get; set; }

            [JsonProperty("track_name")]
            public string TrackName { get; set; }

            [JsonProperty("track_name_translation_list")]
            public List<TrackNameTranslationList> TrackNameTranslationList { get; set; }

            [JsonProperty("track_rating")]
            public int TrackRating { get; set; }

            [JsonProperty("track_length")]
            public int TrackLength { get; set; }

            [JsonProperty("commontrack_id")]
            public int CommontrackId { get; set; }

            [JsonProperty("instrumental")]
            public int Instrumental { get; set; }

            [JsonProperty("explicit")]
            public int Explicit { get; set; }

            [JsonProperty("has_lyrics")]
            public int HasLyrics { get; set; }

            [JsonProperty("has_lyrics_crowd")]
            public int HasLyricsCrowd { get; set; }

            [JsonProperty("has_subtitles")]
            public int HasSubtitles { get; set; }

            [JsonProperty("has_richsync")]
            public int HasRichsync { get; set; }

            [JsonProperty("has_track_structure")]
            public int HasTrackStructure { get; set; }

            [JsonProperty("num_favourite")]
            public int NumFavourite { get; set; }

            [JsonProperty("lyrics_id")]
            public int LyricsId { get; set; }

            [JsonProperty("subtitle_id")]
            public int SubtitleId { get; set; }

            [JsonProperty("album_id")]
            public int AlbumId { get; set; }

            [JsonProperty("album_name")]
            public string AlbumName { get; set; }

            [JsonProperty("album_vanity_id")]
            public string AlbumVanityId { get; set; }

            [JsonProperty("artist_id")]
            public int ArtistId { get; set; }

            [JsonProperty("artist_mbid")]
            public string ArtistMbid { get; set; }

            [JsonProperty("artist_name")]
            public string ArtistName { get; set; }

            [JsonProperty("album_coverart_100x100")]
            public string AlbumCoverart100x100 { get; set; }

            [JsonProperty("album_coverart_350x350")]
            public string AlbumCoverart350x350 { get; set; }

            [JsonProperty("album_coverart_500x500")]
            public string AlbumCoverart500x500 { get; set; }

            [JsonProperty("album_coverart_800x800")]
            public string AlbumCoverart800x800 { get; set; }

            [JsonProperty("track_share_url")]
            public string TrackShareUrl { get; set; }

            [JsonProperty("track_edit_url")]
            public string TrackEditUrl { get; set; }

            [JsonProperty("commontrack_vanity_id")]
            public string CommontrackVanityId { get; set; }

            [JsonProperty("restricted")]
            public int Restricted { get; set; }

            [JsonProperty("first_release_date")]
            public DateTime FirstReleaseDate { get; set; }

            [JsonProperty("updated_time")]
            public DateTime UpdatedTime { get; set; }

            [JsonProperty("primary_genres")]
            public PrimaryGenres PrimaryGenres { get; set; }

            [JsonProperty("secondary_genres")]
            public SecondaryGenres SecondaryGenres { get; set; }
        }

        public class TrackNameTranslationList
        {
            [JsonProperty("track_name_translation")]
            public TrackNameTranslation TrackNameTranslation { get; set; }
        }

        public class TrackNameTranslation
        {
            [JsonProperty("language")]
            public string Language { get; set; }

            [JsonProperty("translation")]
            public string Translation { get; set; }
        }

        public class PrimaryGenres
        {
            [JsonProperty("music_genre_list")]
            public List<MusicGenreList> MusicGenreList { get; set; }
        }

        public class MusicGenreList
        {
            [JsonProperty("music_genre")]
            public MusicGenre MusicGenre { get; set; }
        }

        public class MusicGenre
        {
            [JsonProperty("music_genre_id")]
            public int MusicGenreId { get; set; }

            [JsonProperty("music_genre_parent_id")]
            public int MusicGenreParentId { get; set; }

            [JsonProperty("music_genre_name")]
            public string MusicGenreName { get; set; }

            [JsonProperty("music_genre_name_extended")]
            public string MusicGenreNameExtended { get; set; }

            [JsonProperty("music_genre_vanity")]
            public string MusicGenreVanity { get; set; }
        }

        public class SecondaryGenres
        {
            [JsonProperty("music_genre_list")]
            public List<MusicGenreList> MusicGenreList { get; set; }
        }
    }

    public class GetTranslationsResponse
    {
        [JsonProperty("message")]
        public MessageContent Message { get; set; }

        public class MessageContent
        {
            [JsonProperty("header")]
            public Header Header { get; set; }

            [JsonProperty("body")]
            public Body Body { get; set; }
        }

        public class Header
        {
            [JsonProperty("status_code")]
            public int StatusCode { get; set; }

            [JsonProperty("execute_time")]
            public double ExecuteTime { get; set; }

            [JsonProperty("hint")]
            public string Hint { get; set; }
        }

        public class Body
        {
            [JsonProperty("translations_list")]
            public TranslationsListItem[] TranslationsList { get; set; }
        }

        public class TranslationsListItem
        {
            [JsonProperty("translation")]
            public Translation Translation { get; set; }
        }

        public class Translation
        {
            [JsonProperty("type_id")]
            public string TypeId { get; set; }

            [JsonProperty("artist_id")]
            public int ArtistId { get; set; }

            [JsonProperty("language_from")]
            public string LanguageFrom { get; set; }

            [JsonProperty("user_id")]
            public string UserId { get; set; }

            [JsonProperty("app_id")]
            public string AppId { get; set; }

            [JsonProperty("description")]
            public string Description { get; set; }

            [JsonProperty("snippet")]
            public string Snippet { get; set; }

            [JsonProperty("position")]
            public int Position { get; set; }

            [JsonProperty("selected_language")]
            public string SelectedLanguage { get; set; }

            [JsonProperty("index")]
            public int Index { get; set; }

            [JsonProperty("wantkey")]
            public bool Wantkey { get; set; }

            [JsonProperty("create_timestamp")]
            public int CreateTimestamp { get; set; }

            [JsonProperty("language")]
            public string Language { get; set; }

            [JsonProperty("type_id_weight")]
            public int TypeIdWeight { get; set; }

            [JsonProperty("last_updated")]
            public DateTime LastUpdated { get; set; }

            [JsonProperty("key")]
            public string Key { get; set; }

            [JsonProperty("matched_line")]
            public string MatchedLine { get; set; }

            [JsonProperty("subtitle_matched_line")]
            public string SubtitleMatchedLine { get; set; }

            [JsonProperty("confidence")]
            public double Confidence { get; set; }

            [JsonProperty("user_score")]
            public int UserScore { get; set; }

            [JsonProperty("published_status_macro")]
            public object PublishedStatusMacro { get; set; }

            [JsonProperty("image_id")]
            public int ImageId { get; set; }

            [JsonProperty("video_id")]
            public int VideoId { get; set; }

            [JsonProperty("lyrics_id")]
            public int LyricsId { get; set; }

            [JsonProperty("subtitle_id")]
            public int SubtitleId { get; set; }

            [JsonProperty("created_date")]
            public DateTime CreatedDate { get; set; }

            [JsonProperty("commontrack_id")]
            public int CommontrackId { get; set; }

            [JsonProperty("is_expired")]
            public int IsExpired { get; set; }

            [JsonProperty("group_key")]
            public string GroupKey { get; set; }

            [JsonProperty("can_delete")]
            public int CanDelete { get; set; }

            [JsonProperty("is_mine")]
            public int IsMine { get; set; }

            [JsonProperty("can_approve")]
            public int CanApprove { get; set; }

            [JsonProperty("user")]
            public User User { get; set; }

            [JsonProperty("approver")]
            public object Approver { get; set; }

            [JsonProperty("can_translate")]
            public int CanTranslate { get; set; }
        }

        public class User
        {
            [JsonProperty("uaid")]
            public string Uaid { get; set; }

            [JsonProperty("is_mine")]
            public int IsMine { get; set; }

            [JsonProperty("user_name")]
            public string UserName { get; set; }

            [JsonProperty("user_profile_photo")]
            public string UserProfilePhoto { get; set; }

            [JsonProperty("has_private_profile")]
            public int HasPrivateProfile { get; set; }

            [JsonProperty("has_informative_profile_page")]
            public int HasInformativeProfilePage { get; set; }

            [JsonProperty("has_distributor_profile_page")]
            public int HasDistributorProfilePage { get; set; }

            [JsonProperty("score")]
            public int Score { get; set; }

            [JsonProperty("position")]
            public int Position { get; set; }

            [JsonProperty("level")]
            public string Level { get; set; }

            [JsonProperty("key")]
            public string Key { get; set; }

            [JsonProperty("rank_level")]
            public int RankLevel { get; set; }

            [JsonProperty("rank_name")]
            public string RankName { get; set; }

            [JsonProperty("rank_color")]
            public string RankColor { get; set; }

            [JsonProperty("rank_colors")]
            public RankColors RankColors { get; set; }

            [JsonProperty("rank_image_url")]
            public string RankImageUrl { get; set; }

            [JsonProperty("weekly_score")]
            public int WeeklyScore { get; set; }

            [JsonProperty("points_to_next_level")]
            public int PointsToNextLevel { get; set; }

            [JsonProperty("ratio_to_next_level")]
            public double RatioToNextLevel { get; set; }

            [JsonProperty("next_rank_name")]
            public string NextRankName { get; set; }

            [JsonProperty("ratio_to_next_rank")]
            public double RatioToNextRank { get; set; }

            [JsonProperty("next_rank_color")]
            public string NextRankColor { get; set; }

            [JsonProperty("next_rank_colors")]
            public RankColors NextRankColors { get; set; }

            [JsonProperty("next_rank_image_url")]
            public string NextRankImageUrl { get; set; }

            [JsonProperty("counters")]
            public Counters Counters { get; set; }

            [JsonProperty("moderator_eligible")]
            public bool ModeratorEligible { get; set; }

            [JsonProperty("artist_manager")]
            public int ArtistManager { get; set; }

            [JsonProperty("academy_completed")]
            public bool AcademyCompleted { get; set; }

            [JsonProperty("academy_completed_date")]
            public DateTime AcademyCompletedDate { get; set; }
        }

        public class RankColors
        {
            [JsonProperty("rank_color_10")]
            public string RankColor10 { get; set; }

            [JsonProperty("rank_color_50")]
            public string RankColor50 { get; set; }

            [JsonProperty("rank_color_100")]
            public string RankColor100 { get; set; }

            [JsonProperty("rank_color_200")]
            public string RankColor200 { get; set; }
        }

        public class Counters
        {
            [JsonProperty("lyrics_favourite_added")]
            public int LyricsFavouriteAdded { get; set; }

            [JsonProperty("lyrics_subtitle_added")]
            public int LyricsSubtitleAdded { get; set; }

            [JsonProperty("lyrics_generic_ko")]
            public int LyricsGenericKo { get; set; }

            [JsonProperty("lyrics_missing")]
            public int LyricsMissing { get; set; }

            [JsonProperty("lyrics_changed")]
            public int LyricsChanged { get; set; }

            [JsonProperty("lyrics_ok")]
            public int LyricsOk { get; set; }

            [JsonProperty("lyrics_to_add")]
            public int LyricsToAdd { get; set; }

            [JsonProperty("lyrics_ko")]
            public int LyricsKo { get; set; }

            [JsonProperty("lyrics_music_id")]
            public int LyricsMusicId { get; set; }

            [JsonProperty("vote_bonuses")]
            public int VoteBonuses { get; set; }

            [JsonProperty("track_translation")]
            public int TrackTranslation { get; set; }

            [JsonProperty("vote_maluses")]
            public int VoteMaluses { get; set; }

            [JsonProperty("lyrics_ai_incorrect_text_no")]
            public int LyricsAiIncorrectTextNo { get; set; }

            [JsonProperty("lyrics_ai_completely_wrong_skip")]
            public int LyricsAiCompletelyWrongSkip { get; set; }

            [JsonProperty("lyrics_ai_incorrect_text_yes")]
            public int LyricsAiIncorrectTextYes { get; set; }

            [JsonProperty("lyrics_ai_phrases_not_related_yes")]
            public int LyricsAiPhrasesNotRelatedYes { get; set; }

            [JsonProperty("lyrics_implicitly_ok")]
            public int LyricsImplicitlyOk { get; set; }

            [JsonProperty("lyrics_report_completely_wrong")]
            public int LyricsReportCompletelyWrong { get; set; }

            [JsonProperty("lyrics_richsync_added")]
            public int LyricsRichsyncAdded { get; set; }

            [JsonProperty("track_influencer_bonus_moderator_vote")]
            public int TrackInfluencerBonusModeratorVote { get; set; }

            [JsonProperty("lyrics_ai_mood_analysis_v3_value")]
            public int LyricsAiMoodAnalysisV3Value { get; set; }

            [JsonProperty("lyrics_ai_ugc_language")]
            public int LyricsAiUgcLanguage { get; set; }

            [JsonProperty("lyrics_ranking_change")]
            public int LyricsRankingChange { get; set; }

            [JsonProperty("track_structure")]
            public int TrackStructure { get; set; }

            [JsonProperty("track_complete_metadata")]
            public int TrackCompleteMetadata { get; set; }

            [JsonProperty("evaluation_academy_test")]
            public int EvaluationAcademyTest { get; set; }

            [JsonProperty("lyrics_report_contain_mistakes")]
            public int LyricsReportContainMistakes { get; set; }
        }
    }
}
