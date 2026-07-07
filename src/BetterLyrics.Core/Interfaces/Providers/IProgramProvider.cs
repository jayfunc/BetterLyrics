namespace BetterLyrics.Core.Interfaces.Providers;

public interface IProgramProvider
{
    Task<string?> GetDisplayNameByAumidAsync(string? aumid);
    Task<byte[]?> GetIconByAumidAsync(string aumid);
    Task<string?> GetAppPathByAumidAsync(string? aumid);
}