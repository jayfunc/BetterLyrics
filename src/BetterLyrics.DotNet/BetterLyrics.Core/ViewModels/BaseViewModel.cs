// 2025/6/23 by Zhe Fang

using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterLyrics.Core.ViewModels;

public class BaseViewModel : ObservableRecipient
{
    public BaseViewModel()
    {
        IsActive = true;
    }
}