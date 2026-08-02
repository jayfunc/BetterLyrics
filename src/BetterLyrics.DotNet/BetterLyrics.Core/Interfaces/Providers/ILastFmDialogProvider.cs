namespace BetterLyrics.Core.Interfaces.Providers;

public interface ILastFmDialogProvider
{
    Task ShowAuthDialogAsync();
    Task ShowUnAuthDialogAsync(Func<Task> onConfirm);
}
