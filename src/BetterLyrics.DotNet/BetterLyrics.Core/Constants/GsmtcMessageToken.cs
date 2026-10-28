namespace BetterLyrics.Core.Constants;

public static class GsmtcMessageToken
{
    /// <summary>
    /// 内置播放器专用 GSMTC 服务发送消息时使用的通道标识，避免影响订阅默认通道的其他接收者。
    /// </summary>
    public const string BuiltIn = "BuiltInGsmtc";
}
