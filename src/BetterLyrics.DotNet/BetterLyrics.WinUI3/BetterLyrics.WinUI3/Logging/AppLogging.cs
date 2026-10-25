using BetterLyrics.Core.Helpers;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;

namespace BetterLyrics.WinUI3.Logging;

public static class AppLogging
{
    private const string OutputTemplate =
        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}";

    private static readonly LoggingLevelSwitch LevelSwitch = new(LogEventLevel.Information);

    private static bool _isShutdown;

    public static LogEventLevel MinimumLevel => LevelSwitch.MinimumLevel;

    public static void Initialize()
    {
        _isShutdown = false;

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.ControlledBy(LevelSwitch)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Error)
            .MinimumLevel.Override("Microsoft.Extensions.Http", LogEventLevel.Error)
            .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.File(
                PathHelper.LogFilePattern,
                rollingInterval: RollingInterval.Day,
                flushToDiskInterval: TimeSpan.FromSeconds(1),
                outputTemplate: OutputTemplate)
            .CreateLogger();
    }

    public static void ApplyLogLevel(bool writeDebugLogs)
    {
        LevelSwitch.MinimumLevel = writeDebugLogs ? LogEventLevel.Debug : LogEventLevel.Information;
    }

    public static void Shutdown()
    {
        if (_isShutdown) return;
        _isShutdown = true;

        Log.CloseAndFlush();
    }
}
