namespace BetterLyrics.Core.Interfaces.Providers;

public interface ISpoutTextureProvider
{
    string SenderName { get; }
    public void Initialize(object device, string senderName);
    public void SendTexture(object renderTarget);
    public void Close();
}
