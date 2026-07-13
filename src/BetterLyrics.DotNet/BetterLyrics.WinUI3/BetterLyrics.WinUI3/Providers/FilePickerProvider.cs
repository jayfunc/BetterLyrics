using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Interfaces.Providers;
using CommunityToolkit.Mvvm.DependencyInjection;
using WinRT.Interop;

namespace BetterLyrics.WinUI3.Providers;

public class FilePickerProvider : IFilePickerProvider
{
    private readonly IWindowManagerProvider _windowManagerProvider =
        Ioc.Default.GetRequiredService<IWindowManagerProvider>();

    public async Task<(string? name, string? path)> PickSingleFolderAsync(WindowType targetWindowType,
        object? targetWindowParameter = null)
    {
        var window = _windowManagerProvider.GetWindow(targetWindowType, targetWindowParameter);
        if (window == null) return (null, null);

        var picker = new FolderPicker();
        picker.FileTypeFilter.Add("*");

        var hwnd = WindowNative.GetWindowHandle(window);
        InitializeWithWindow.Initialize(picker, hwnd);

        var folder = await picker.PickSingleFolderAsync();

        return (folder?.Name, folder?.Path);
    }

    public async Task<(string? name, string? path)> PickSingleFileAsync(string[] fileTypeFilter,
        WindowType targetWindowType,
        object? targetWindowParameter = null
    )
    {
        var window = _windowManagerProvider.GetWindow(targetWindowType, targetWindowParameter);
        if (window == null) return (null, null);

        var picker = new FileOpenPicker();
        foreach (var item in fileTypeFilter) picker.FileTypeFilter.Add(item);

        var hwnd = WindowNative.GetWindowHandle(window);
        InitializeWithWindow.Initialize(picker, hwnd);

        var file = await picker.PickSingleFileAsync();

        return (file?.Name, file?.Path);
    }

    public async Task<(string? name, string? path)> PickSaveFileAsync(
        IDictionary<string, IList<string>> fileTypeChoices, string? suggestedFileName,
        WindowType targetWindowType,
        object? targetWindowParameter = null
    )
    {
        var window = _windowManagerProvider.GetWindow(targetWindowType, targetWindowParameter);
        if (window == null) return (null, null);

        var picker = new FileSavePicker();
        foreach (var item in fileTypeChoices) picker.FileTypeChoices.Add(item);

        if (suggestedFileName != null) picker.SuggestedFileName = suggestedFileName;

        var hwnd = WindowNative.GetWindowHandle(window);
        InitializeWithWindow.Initialize(picker, hwnd);

        var file = await picker.PickSaveFileAsync();

        return (file?.Name, file?.Path);
    }
}