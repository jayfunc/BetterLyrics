using System.Threading.Tasks;
using BetterLyrics.Core.Interfaces.Providers;

namespace BetterLyrics.Avalonia.Providers;

public class ProgramProvider : IProgramProvider
{
    public Task<string?> GetDisplayNameByAumidAsync(string? aumid)
    {
        throw new System.NotImplementedException();
    }

    public Task<byte[]?> GetIconByAumidAsync(string aumid)
    {
        throw new System.NotImplementedException();
    }

    public Task<string?> GetAppPathByAumidAsync(string? aumid)
    {
        throw new System.NotImplementedException();
    }
}