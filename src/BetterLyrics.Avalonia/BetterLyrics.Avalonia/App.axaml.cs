using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using BetterLyrics.Avalonia.Providers;
using BetterLyrics.Avalonia.Services;
using BetterLyrics.Avalonia.ViewModels;
using BetterLyrics.Avalonia.Views;
using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;

namespace BetterLyrics.Avalonia
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            PathHelper.Initialize(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BetterLyrics"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BetterLyrics", "Cache"),
                "Assets");
            PathHelper.EnsureDirectories();

            Ioc.Default.ConfigureServices(
                new ServiceCollection()
                    // Services
                    .AddSingleton<ILocalizationService, LocalizationService>()
                    .AddSingleton<ILyricsCacheService, LyricsCacheService>()
                    .AddSingleton<ISettingsService, SettingsService>()
                    .AddSingleton<IAppUpdateService, AppUpdateService>()

                    // Providers
                    .AddSingleton<IPlatformProvider, PlatformProvider>()
                    .AddSingleton<IStringConverterProvider, StringConverterProvider>()
                    .AddSingleton<ISystemUIProvider, SystemUIProvider>()

                    // ViewModels
                    .AddSingleton<AboutControlViewModel>()
                    .AddSingleton<SettingsPageViewModel>()

                    .BuildServiceProvider()
            );

            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow();
            }
            else if (ApplicationLifetime is IActivityApplicationLifetime singleViewFactoryApplicationLifetime)
            {
                singleViewFactoryApplicationLifetime.MainViewFactory = () => new SettingsPage();
            }
            else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
            {
                singleViewPlatform.MainView = new SettingsPage();
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}