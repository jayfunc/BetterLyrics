using BetterLyrics.Core.Interfaces.Providers;
using System;

namespace BetterLyrics.Avalonia.Providers;

public class SpoutTextureProvider : ISpoutTextureProvider
{
    public string SenderName => throw new NotImplementedException();

    public void Close()
    {
        throw new NotImplementedException();
    }

    public void Initialize(object device, string senderName)
    {
        throw new NotImplementedException();
    }

    public void SendTexture(object renderTarget)
    {
        throw new NotImplementedException();
    }
}
