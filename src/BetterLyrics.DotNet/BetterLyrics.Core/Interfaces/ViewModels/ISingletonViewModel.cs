namespace BetterLyrics.Core.Interfaces.ViewModels;

/// <summary>
/// ViewModel that implements this interface should manage its own lifetime.
/// If at the same time it inherits from ObservableRecipient, it should set IsActive to true in the constructor.
/// </summary>
public interface ISingletonViewModel
{
}
