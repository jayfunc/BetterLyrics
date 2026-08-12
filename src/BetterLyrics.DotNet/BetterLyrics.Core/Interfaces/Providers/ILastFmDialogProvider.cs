namespace BetterLyrics.Core.Interfaces.Providers;

public interface ILastFmDialogProvider
{
    Task ShowAuthDialogAsync(Func<Task> onConfirm);
    Task ShowUnAuthDialogAsync(Func<Task> onConfirm);
}
