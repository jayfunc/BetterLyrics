using System.Runtime.CompilerServices;
using BetterLyrics.WinUI3.Services.Settings;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterLyrics.WinUI3.ViewModels
{
    public class BaseViewModel : ObservableObject
    {
        private readonly ISettingsService _settingsService;

        public BaseViewModel(ISettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        protected void Set(string key, object value, [CallerMemberName] string? propertyName = null)
        {
            _settingsService.Set(key, value);
            OnPropertyChanged(propertyName);
        }

        protected T? Get<T>(string key, T? defaultValue = default)
        {
            return _settingsService.Get(key, defaultValue);
        }
    }
}
