using System;
using System.Threading.Tasks;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Models.Settings;

namespace BetterLyrics.Core.Interfaces.Providers;

public interface IAddMediaSourceDialogProvider
{
    Task ShowDialogAsync(FileSourceType fileSourceType, Func<MediaFolder, Task<(bool isValid, string? errorMessage)>> validationCallback);
}
