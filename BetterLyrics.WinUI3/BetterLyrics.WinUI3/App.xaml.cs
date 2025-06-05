using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services.Database;
using BetterLyrics.WinUI3.Services.Settings;
using BetterLyrics.WinUI3.ViewModels;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.ApplicationModel.Resources;
using Microsoft.Windows.AppLifecycle;
using Newtonsoft.Json;
using Serilog;
using Serilog.Core;
using Windows.ApplicationModel.Core;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private readonly ILogger<App> _logger;

        public static new App Current => (App)Application.Current;
        public MainWindow? MainWindow { get; private set; }
        public MainWindow? SettingsWindow { get; set; }

        public static ResourceLoader? ResourceLoader { get; private set; }

        public static DispatcherQueue DispatcherQueue => DispatcherQueue.GetForCurrentThread();

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();

            App.ResourceLoader = new ResourceLoader();

            Helper.AppInfo.EnsureDirectories();
            ConfigureServices();

            _logger = Ioc.Default.GetService<ILogger<App>>()!;

            UnhandledException += App_UnhandledException;
        }

        private static void ConfigureServices()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(Helper.AppInfo.LogFilePattern, rollingInterval: RollingInterval.Day)
                .CreateLogger();

            // Register services
            Ioc.Default.ConfigureServices(
                new ServiceCollection()
                    .AddSingleton(DispatcherQueue.GetForCurrentThread())
                    .AddLogging(loggingBuilder =>
                    {
                        loggingBuilder.ClearProviders();
                        loggingBuilder.AddSerilog();
                    })
                    // Services
                    .AddSingleton<SettingsService>()
                    .AddSingleton<DatabaseService>()
                    // ViewModels
                    .AddSingleton<MainViewModel>()
                    .AddSingleton<SettingsViewModel>()
                    .BuildServiceProvider()
            );
        }

        private void App_UnhandledException(
            object sender,
            Microsoft.UI.Xaml.UnhandledExceptionEventArgs e
        )
        {
            _logger.LogError(e.Exception, "App_UnhandledException");
            e.Handled = true;
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            // Activate the window
            MainWindow = new MainWindow();
            MainWindow!.Navigate(typeof(MainPage));
            MainWindow.Activate();
        }
    }
}
