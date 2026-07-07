using System.Collections.Generic;
using System.Threading.Tasks;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Interfaces.Providers;

namespace BetterLyrics.Avalonia.Providers;

public class FilePickerProvider : IFilePickerProvider
{
    public Task<(string? name, string? path)> PickSingleFolderAsync(WindowType targetWindowType,
        object? targetWindowParameter = null)
    {
        throw new System.NotImplementedException();
    }

    public Task<(string? name, string? path)> PickSingleFileAsync(string[] fileTypeFilter, WindowType targetWindowType,
        object? targetWindowParameter = null)
    {
        throw new System.NotImplementedException();
    }

    public Task<(string? name, string? path)> PickSaveFileAsync(IDictionary<string, IList<string>> fileTypeChoices,
        string? suggestedFileName, WindowType targetWindowType,
        object? targetWindowParameter = null)
    {
        throw new System.NotImplementedException();
    }
}