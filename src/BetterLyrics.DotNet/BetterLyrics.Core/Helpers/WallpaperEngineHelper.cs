using BetterLyrics.Core.Models.WallpaperEngine;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace BetterLyrics.Core.Helpers;

/// <summary>
/// Reads Wallpaper Engine's configuration and project metadata.
/// Wallpaper Engine keeps the currently selected wallpaper in
/// <c>&lt;install&gt;\config.json</c> under
/// <c>&lt;user&gt;.general.wallpaperconfig.selectedwallpapers</c>.
/// </summary>
public static class WallpaperEngineHelper
{
    private const string WallpaperEngineAppId = "431960";

    private static readonly JsonDocumentOptions JsonOptions = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip
    };

    /// <summary>
    /// Converts a wallpaper file (project.json, scene.pkg, video, index.html, ...)
    /// to the project.json path that identifies the wallpaper project.
    /// </summary>
    public static string? NormalizeProjectPath(string? filePath, string? installDirectory = null)
    {
        if (string.IsNullOrWhiteSpace(filePath)) return null;

        try
        {
            var path = filePath.Trim().Replace('/', '\\');
            if (path.StartsWith(@"\\?\", StringComparison.Ordinal)) path = path[4..];

            if (!Path.IsPathRooted(path))
            {
                if (string.IsNullOrWhiteSpace(installDirectory)) return null;
                path = Path.Combine(installDirectory, path);
            }

            path = Path.GetFullPath(path);

            var directory = Directory.Exists(path) ? path : Path.GetDirectoryName(path);
            for (var i = 0; i < 5 && !string.IsNullOrEmpty(directory); i++)
            {
                var projectJson = Path.Combine(directory, "project.json");
                if (File.Exists(projectJson)) return Path.GetFullPath(projectJson);
                directory = Path.GetDirectoryName(directory);
            }

            return path;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Reads the wallpapers currently assigned to monitors.
    /// Returns <see langword="null"/> when the config cannot be read or when it
    /// does not contain any selected wallpaper (for example while a playlist
    /// controls the desktop), so callers can keep the previous state.
    /// </summary>
    public static IReadOnlySet<string>? ReadActiveProjectPaths(string configJson, string? installDirectory = null,
        string? userName = null)
    {
        try
        {
            using var document = JsonDocument.Parse(configJson, JsonOptions);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return null;

            JsonElement? profile = null;
            JsonElement? fallbackProfile = null;

            // Older config layouts stored "general" directly at the root.
            if (root.TryGetProperty("general", out var rootGeneral) &&
                rootGeneral.ValueKind == JsonValueKind.Object)
            {
                fallbackProfile = root;
            }

            foreach (var property in root.EnumerateObject())
            {
                if (property.Name.StartsWith('?')) continue;
                if (property.Value.ValueKind != JsonValueKind.Object) continue;
                if (!property.Value.TryGetProperty("general", out var general) ||
                    general.ValueKind != JsonValueKind.Object) continue;

                fallbackProfile ??= property.Value;

                if (!string.IsNullOrEmpty(userName) &&
                    string.Equals(property.Name, userName, StringComparison.OrdinalIgnoreCase))
                {
                    profile = property.Value;
                    break;
                }
            }

            profile ??= fallbackProfile;
            if (profile == null) return null;

            if (!profile.Value.TryGetProperty("general", out var profileGeneral)) return null;
            if (!profileGeneral.TryGetProperty("wallpaperconfig", out var wallpaperConfig)) return null;
            if (!wallpaperConfig.TryGetProperty("selectedwallpapers", out var selectedWallpapers)) return null;
            if (selectedWallpapers.ValueKind != JsonValueKind.Object) return null;

            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var monitor in selectedWallpapers.EnumerateObject())
            {
                string? filePath;

                if (monitor.Value.ValueKind == JsonValueKind.String)
                {
                    filePath = GetStringValue(monitor.Value);
                }
                else if (monitor.Value.ValueKind == JsonValueKind.Object &&
                         monitor.Value.TryGetProperty("file", out var file))
                {
                    filePath = GetStringValue(file);
                }
                else
                {
                    continue;
                }

                var normalized = NormalizeProjectPath(filePath, installDirectory);
                if (!string.IsNullOrWhiteSpace(normalized)) result.Add(normalized);
            }

            return result.Count == 0 ? null : result;
        }
        catch
        {
            return null;
        }
    }

    public static IReadOnlySet<string>? ReadActiveProjectPathsFromFile(string configPath, string? installDirectory = null,
        string? userName = null)
    {
        var json = ReadAllTextWithRetry(configPath);
        return json == null ? null : ReadActiveProjectPaths(json, installDirectory, userName);
    }

    /// <summary>
    /// Enumerates all installed wallpaper projects (local projects, default
    /// projects and Steam Workshop items).
    /// </summary>
    public static IReadOnlyList<WallpaperEngineWallpaper> ReadInstalledWallpapers(
        string installDirectory,
        IEnumerable<string>? steamLibraryDirectories = null,
        IEnumerable<string>? extraWallpaperFolders = null)
    {
        var projectJsonPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var projectsRoot = Path.Combine(installDirectory, "projects");
        AddProjectIfExists(projectsRoot, projectJsonPaths);
        AddProjectsFromRoot(Path.Combine(projectsRoot, "myprojects"), projectJsonPaths);
        AddProjectsFromRoot(Path.Combine(projectsRoot, "defaultprojects"), projectJsonPaths);

        foreach (var libraryDirectory in steamLibraryDirectories ?? [])
        {
            if (string.IsNullOrWhiteSpace(libraryDirectory)) continue;

            var contentDirectory = Path.Combine(libraryDirectory, "steamapps", "workshop", "content",
                WallpaperEngineAppId);
            if (!Directory.Exists(contentDirectory)) continue;

            foreach (var wallpaperDirectory in SafeGetDirectories(contentDirectory))
            {
                var projectJson = Path.Combine(wallpaperDirectory, "project.json");
                if (File.Exists(projectJson)) projectJsonPaths.Add(Path.GetFullPath(projectJson));
            }
        }

        foreach (var extraFolder in extraWallpaperFolders ?? [])
        {
            if (string.IsNullOrWhiteSpace(extraFolder)) continue;
            AddProjectsFromRoot(extraFolder, projectJsonPaths);
        }

        var wallpapers = new List<WallpaperEngineWallpaper>(projectJsonPaths.Count);

        foreach (var projectJsonPath in projectJsonPaths)
        {
            var (title, type, workshopId) = ReadProjectInfo(projectJsonPath);

            wallpapers.Add(new WallpaperEngineWallpaper
            {
                ProjectPath = projectJsonPath,
                Title = title,
                DisplayName = title,
                Type = type,
                WorkshopId = workshopId
            });
        }

        var duplicatedTitles = wallpapers
            .GroupBy(x => x.Title, StringComparer.OrdinalIgnoreCase)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return wallpapers
            .Select(x => new WallpaperEngineWallpaper
            {
                ProjectPath = x.ProjectPath,
                Title = x.Title,
                DisplayName = duplicatedTitles.Contains(x.Title) ? $"{x.Title} ({x.Identifier})" : x.Title,
                Type = x.Type,
                WorkshopId = x.WorkshopId
            })
            .OrderBy(x => x.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(x => x.ProjectPath, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Reads Steam library roots from libraryfolders.vdf.
    /// </summary>
    public static IReadOnlyList<string> GetSteamLibraryPaths(string? steamPath)
    {
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(steamPath)) return [];

        var normalizedSteamPath = steamPath.Replace('/', '\\').TrimEnd('\\');
        if (Directory.Exists(normalizedSteamPath)) paths.Add(normalizedSteamPath);

        var libraryFoldersPath = Path.Combine(normalizedSteamPath, "steamapps", "libraryfolders.vdf");
        if (!File.Exists(libraryFoldersPath)) return paths.ToList();

        try
        {
            foreach (var line in File.ReadLines(libraryFoldersPath))
            {
                var match = Regex.Match(line, "\"path\"\\s*\"(?<path>[^\"]+)\"");
                if (!match.Success) continue;

                var path = match.Groups["path"].Value.Replace("\\\\", "\\").Replace('/', '\\');
                if (Directory.Exists(path)) paths.Add(Path.GetFullPath(path));
            }
        }
        catch
        {
            // Ignore malformed or inaccessible library files.
        }

        return paths.ToList();
    }

    public static string? ReadAllTextWithRetry(string path, int attempts = 4)
    {
        for (var i = 0; i < attempts; i++)
        {
            try
            {
                return File.ReadAllText(path);
            }
            catch (IOException)
            {
                Thread.Sleep(40);
            }
            catch (UnauthorizedAccessException)
            {
                return null;
            }
            catch
            {
                return null;
            }
        }

        return null;
    }

    private static void AddProjectsFromRoot(string root, HashSet<string> projectJsonPaths)
    {
        if (!Directory.Exists(root)) return;

        AddProjectIfExists(root, projectJsonPaths);

        foreach (var directory in SafeGetDirectories(root))
        {
            AddProjectIfExists(directory, projectJsonPaths);

            foreach (var subDirectory in SafeGetDirectories(directory))
            {
                AddProjectIfExists(subDirectory, projectJsonPaths);
            }
        }
    }

    private static void AddProjectIfExists(string directory, HashSet<string> projectJsonPaths)
    {
        var projectJson = Path.Combine(directory, "project.json");
        if (File.Exists(projectJson)) projectJsonPaths.Add(Path.GetFullPath(projectJson));
    }

    private static (string Title, string Type, string? WorkshopId) ReadProjectInfo(string projectJsonPath)
    {
        var fallbackTitle = Path.GetFileName(Path.GetDirectoryName(projectJsonPath)) ?? projectJsonPath;
        var workshopId = GetWorkshopIdFromPath(projectJsonPath);

        try
        {
            var json = ReadAllTextWithRetry(projectJsonPath);
            if (json == null) return (fallbackTitle, string.Empty, workshopId);

            using var document = JsonDocument.Parse(json, JsonOptions);
            var root = document.RootElement;

            var title = root.TryGetProperty("title", out var titleElement) ? GetStringValue(titleElement) : null;
            var type = root.TryGetProperty("type", out var typeElement) ? GetStringValue(typeElement) : null;
            var projectWorkshopId = root.TryGetProperty("workshopid", out var workshopIdElement)
                ? GetStringValue(workshopIdElement)
                : null;

            if (!string.IsNullOrWhiteSpace(projectWorkshopId)) workshopId = projectWorkshopId;

            return (string.IsNullOrWhiteSpace(title) ? fallbackTitle : title.Trim(), type ?? string.Empty, workshopId);
        }
        catch
        {
            return (fallbackTitle, string.Empty, workshopId);
        }
    }

    private static string? GetStringValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.ToString(),
            _ => null
        };
    }

    private static string? GetWorkshopIdFromPath(string projectJsonPath)
    {
        var segments = projectJsonPath.Split(['\\', '/'], StringSplitOptions.RemoveEmptyEntries);

        for (var i = 0; i < segments.Length - 2; i++)
        {
            if (string.Equals(segments[i], "content", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(segments[i + 1], WallpaperEngineAppId, StringComparison.OrdinalIgnoreCase))
            {
                return segments[i + 2];
            }
        }

        return null;
    }

    private static IEnumerable<string> SafeGetDirectories(string path)
    {
        try
        {
            return Directory.GetDirectories(path);
        }
        catch
        {
            return [];
        }
    }
}
