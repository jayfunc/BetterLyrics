// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;
using System;
using System.Runtime.CompilerServices;

namespace BetterLyrics.WinUI3.ViewModels
{
    /// <summary>
    /// Defines the <see cref="BaseViewModel" />
    /// </summary>
    public partial class BaseViewModel : ObservableRecipient, IDisposable
    {
        #region Fields

        /// <summary>
        /// Defines the _dispatcherQueue
        /// </summary>
        private protected readonly DispatcherQueue _dispatcherQueue =
            DispatcherQueue.GetForCurrentThread();

        /// <summary>
        /// Defines the _settingsService
        /// </summary>
        private protected readonly ISettingsService _settingsService;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseViewModel"/> class.
        /// </summary>
        /// <param name="settingsService">The settingsService<see cref="ISettingsService"/></param>
        public BaseViewModel(ISettingsService settingsService)
        {
            IsActive = true;
            _settingsService = settingsService;
        }

        #endregion

        #region Methods

        /// <summary>
        /// The Dispose
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
