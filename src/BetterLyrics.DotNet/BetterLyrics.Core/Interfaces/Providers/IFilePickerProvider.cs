using BetterLyrics.Core.Enums;

namespace BetterLyrics.Core.Interfaces.Providers;

public interface IFilePickerProvider
{
    Task<(string? name, string? path)> PickSingleFolderAsync(WindowType targetWindowType,
        object? targetWindowParameter = null);

    Task<(string? name, string? path)> PickSingleFileAsync(string[] fileTypeFilter,
        WindowType targetWindowType,
        object? targetWindowParameter = null);

    Task<(string? name, string? path)> PickSaveFileAsync(IDictionary<string, IList<string>> fileTypeChoices,
        string? suggestedFileName,
        WindowType targetWindowType,
        object? targetWindowParameter = null);
}