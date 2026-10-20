using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models.WallpaperEngine;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Services;

/// <summary>
/// Watches Wallpaper Engine's config.json for wallpaper changes and exposes the
/// installed wallpaper projects.
/// </summary>
public sealed class WallpaperEngineService : IWallpaperEngineService
{
    private const string WallpaperEngineSteamAppId = "431960";

    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan InstallProbeInterval = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan WallpaperCacheDuration = TimeSpan.FromSeconds(30);

    private readonly ILogger<WallpaperEngineService> _logger;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    private string? _installDirectory;
    private string? _configPath;
    private string? _configStamp;
    private IReadOnlySet<string>? _activeProjectPaths;
    private IReadOnlyList<WallpaperEngineWallpaper>? _cachedWallpapers;
    private DateTime _wallpaperCacheTimeUtc;
    private DateTime _lastInstallProbeUtc = DateTime.MinValue;
    private Timer? _pollTimer;
    private bool _initialized;
    private int _isPolling;

    public WallpaperEngineService(ILogger<WallpaperEngineService> logger)
    {
        _logger = logger;
    }

    public bool IsAvailable => _configPath != null && File.Exists(_configPath);

    public event EventHandler<WallpaperEngineActiveWallpaperChangedEventArgs>? ActiveWallpaperChanged;

    public Task InitializeAsync()
    {
        if (_initialized) return Task.CompletedTask;
        _initialized = true;

        try
        {
            ProbeInstall(force: true);

            if (_configPath != null)
            {
                _activeProjectPaths = WallpaperEngineHelper.ReadActiveProjectPathsFromFile(
                    _configPath, _installDirectory, Environment.UserName);
            }

            _pollTimer = new Timer(Poll, null, PollInterval, PollInterval);

            if (_installDirectory != null)
            {
                _logger.LogInformation("Wallpaper Engine integration initialized. Install directory: {Directory}",
                    _installDirectory);
            }
            else
            {
                _logger.LogInformation("Wallpaper Engine installation was not found. Integration is idle.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to initialize Wallpaper Engine integration.");
        }

        return Task.CompletedTask;
    }

    public IReadOnlySet<string>? GetActiveWallpaperProjectPaths() => _activeProjectPaths;

    public async Task<IReadOnlyList<WallpaperEngineWallpaper>> GetInstalledWallpapersAsync(bool forceRefresh = false,
        CancellationToken cancellationToken = default)
    {
        if (!forceRefresh && _cachedWallpapers != null &&
            DateTime.UtcNow - _wallpaperCacheTimeUtc < WallpaperCacheDuration)
        {
            return _cachedWallpapers;
        }

        await _refreshLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!forceRefresh && _cachedWallpapers != null &&
                DateTime.UtcNow - _wallpaperCacheTimeUtc < WallpaperCacheDuration)
            {
                return _cachedWallpapers;
            }

            ProbeInstall(force: forceRefresh);

            IReadOnlyList<WallpaperEngineWallpaper> wallpapers;
            if (_installDirectory == null)
            {
                wallpapers = [];
            }
            else
            {
                var installDirectory = _installDirectory;
                var steamLibraryPaths = new HashSet<string>(
                    WallpaperEngineHelper.GetSteamLibraryPaths(GetSteamPath()), StringComparer.OrdinalIgnoreCase);

                // The install directory itself points at a Steam library even when
                // the Steam registry key is unavailable.
                var localLibraryRoot = TryGetSteamLibraryRoot(installDirectory);
                if (localLibraryRoot != null) steamLibraryPaths.Add(localLibraryRoot);

                wallpapers = await Task.Run(
                    () => WallpaperEngineHelper.ReadInstalledWallpapers(installDirectory, steamLibraryPaths),
                    cancellationToken).ConfigureAwait(false);
            }

            _cachedWallpapers = wallpapers;
            _wallpaperCacheTimeUtc = DateTime.UtcNow;

            return wallpapers;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to enumerate Wallpaper Engine wallpapers.");
            return _cachedWallpapers ?? [];
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private void Poll(object? state)
    {
        if (!_initialized) return;
        if (Interlocked.Exchange(ref _isPolling, 1) == 1) return;

        try
        {
            ProbeInstall();

            if (_configPath == null || !File.Exists(_configPath)) return;

            var configInfo = new FileInfo(_configPath);
            var stamp = $"{configInfo.LastWriteTimeUtc.Ticks}:{configInfo.Length}";
            if (string.Equals(stamp, _configStamp, StringComparison.Ordinal)) return;

            var activeProjectPaths = WallpaperEngineHelper.ReadActiveProjectPathsFromFile(
                _configPath, _installDirectory, Environment.UserName);

            // The file may be read while Wallpaper Engine is rewriting it.
            // Keep the previous state and retry on the next tick.
            if (activeProjectPaths == null) return;

            _configStamp = stamp;

            var previousProjectPaths = _activeProjectPaths;
            if (previousProjectPaths != null && previousProjectPaths.SetEquals(activeProjectPaths)) return;

            _activeProjectPaths = activeProjectPaths;

            _logger.LogInformation("Wallpaper Engine active wallpaper changed: {Wallpapers}",
                string.Join(", ", activeProjectPaths));

            ActiveWallpaperChanged?.Invoke(this,
                new WallpaperEngineActiveWallpaperChangedEventArgs(activeProjectPaths));
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to poll Wallpaper Engine configuration.");
        }
        finally
        {
            Interlocked.Exchange(ref _isPolling, 0);
        }
    }

    private void ProbeInstall(bool force = false)
    {
        if (!force && DateTime.UtcNow - _lastInstallProbeUtc < InstallProbeInterval) return;
        _lastInstallProbeUtc = DateTime.UtcNow;

        if (_installDirectory != null)
        {
            var existingConfigPath = Path.Combine(_installDirectory, "config.json");
            if (File.Exists(existingConfigPath))
            {
                _configPath = existingConfigPath;
                return;
            }
        }

        var installDirectory = FindInstallDirectory();
        if (installDirectory == null) return;

        if (!string.Equals(_installDirectory, installDirectory, StringComparison.OrdinalIgnoreCase))
        {
            _installDirectory = installDirectory;
            _configPath = Path.Combine(installDirectory, "config.json");
            _configStamp = null;
        }
        else
        {
            _configPath ??= Path.Combine(installDirectory, "config.json");
        }
    }

    private static string? FindInstallDirectory()
    {
        foreach (var processName in new[] { "wallpaper64", "wallpaper32" })
        {
            try
            {
                foreach (var process in Process.GetProcessesByName(processName))
                {
                    using (process)
                    {
                        try
                        {
                            var executablePath = process.MainModule?.FileName;
                            var directory = string.IsNullOrEmpty(executablePath)
                                ? null
                                : Path.GetDirectoryName(executablePath);

                            if (directory != null && IsWallpaperEngineDirectory(directory)) return directory;
                        }
                        catch
                        {
                            // Access to another process can be denied.
                        }
                    }
                }
            }
            catch
            {
                // Ignore process enumeration failures.
            }
        }

        foreach (var directory in GetRegistryInstallDirectories())
        {
            if (IsWallpaperEngineDirectory(directory)) return directory;
        }

        var steamPath = GetSteamPath();
        foreach (var libraryDirectory in WallpaperEngineHelper.GetSteamLibraryPaths(steamPath))
        {
            var directory = Path.Combine(libraryDirectory, "steamapps", "common", "wallpaper_engine");
            if (IsWallpaperEngineDirectory(directory)) return directory;
        }

        foreach (var programFiles in new[]
                 {
                     Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                     Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
                 })
        {
            if (string.IsNullOrWhiteSpace(programFiles)) continue;

            var defaultSteamPath = Path.Combine(programFiles, "Steam");
            foreach (var libraryDirectory in WallpaperEngineHelper.GetSteamLibraryPaths(defaultSteamPath))
            {
                var directory = Path.Combine(libraryDirectory, "steamapps", "common", "wallpaper_engine");
                if (IsWallpaperEngineDirectory(directory)) return directory;
            }
        }

        return null;
    }

    private static string? TryGetSteamLibraryRoot(string installDirectory)
    {
        try
        {
            var libraryRoot = Path.GetFullPath(Path.Combine(installDirectory, "..", "..", ".."));
            var workshopContent = Path.Combine(libraryRoot, "steamapps", "workshop", "content",
                WallpaperEngineSteamAppId);

            return Directory.Exists(workshopContent) ? libraryRoot : null;
        }
        catch
        {
            return null;
        }
    }

    private static bool IsWallpaperEngineDirectory(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory)) return false;

        return File.Exists(Path.Combine(directory, "config.json")) ||
               File.Exists(Path.Combine(directory, "wallpaper64.exe")) ||
               File.Exists(Path.Combine(directory, "wallpaper32.exe"));
    }

    private static IEnumerable<string> GetRegistryInstallDirectories()
    {
        var directories = new List<string>();

        TryAddRegistryDirectory(Registry.LocalMachine,
            @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Steam App " + WallpaperEngineSteamAppId,
            directories);
        TryAddRegistryDirectory(Registry.LocalMachine,
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App " + WallpaperEngineSteamAppId,
            directories);
        TryAddRegistryDirectory(Registry.CurrentUser,
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App " + WallpaperEngineSteamAppId,
            directories);

        var steamPath = GetSteamPath();
        if (!string.IsNullOrWhiteSpace(steamPath))
        {
            directories.Add(Path.Combine(steamPath, "steamapps", "common", "wallpaper_engine"));
        }

        return directories;
    }

    private static void TryAddRegistryDirectory(RegistryKey root, string subKey, ICollection<string> directories)
    {
        try
        {
            using var key = root.OpenSubKey(subKey);
            if (key?.GetValue("InstallLocation") is string installLocation &&
                !string.IsNullOrWhiteSpace(installLocation))
            {
                directories.Add(installLocation);
            }
        }
        catch
        {
            // Ignore registry access failures.
        }
    }

    private static string? GetSteamPath()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
            if (key?.GetValue("SteamPath") is string steamPath && !string.IsNullOrWhiteSpace(steamPath))
            {
                return steamPath.Replace('/', '\\');
            }
        }
        catch
        {
            // Ignore registry access failures.
        }

        return null;
    }
}
