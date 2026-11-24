namespace BetterLyrics.Core.Constants;

public static class LastFmApi
{
    // According to https://support.last.fm/t/api-keys-will-require-pro-from-1st-december/121030
    // Last.fm API keys will require a paid subscription from 1st December 2026. The free tier will be removed, and all existing free API keys will stop working.
    // BetterLyrics will continue to work with Last.fm, but users will need to provide their own API key and shared secret. The app will not provide a default API key or shared secret.
    public const string ApiKeyCredentialKey = "LastFMApiKey";
    public const string SharedSecretCredentialKey = "LastFMSharedSecret";
    public const string SessionKeyCredentialKey = "LastFMSessionKey";

    public const string UnAuthUrl = "https://www.last.fm/settings/applications";
}
