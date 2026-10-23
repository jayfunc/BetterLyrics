namespace BetterLyrics.Core.Interfaces.ViewModels;

/// <summary>
/// ViewModel that implements this interface should not manage its own lifetime.
/// If at the same time it inherits from ObservableRecipient, IsActive should be set by the view that uses it, when the view is loaded and unloaded.
/// In this project, this is done by the BaseUserControl, BasePage and BaseWindow classes, which are the base classes for all views.
/// </summary>
public interface ITransientViewModel
{
}
