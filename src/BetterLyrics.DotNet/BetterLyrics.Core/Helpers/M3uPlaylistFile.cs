using BetterLyrics.Core.Extensions;
using BetterLyrics.Core.Models;

namespace BetterLyrics.Core.Helpers;

public sealed class M3uPlaylistEntry
{
    /// <summary>
    /// Raw lines of this entry, including any #EXTINF or comment lines that belong to the track.
    /// </summary>
    public List<string> RawLines { get; } = [];

    /// <summary>
    /// Absolute local path parsed from the track line, or null when this entry is not a resolvable local track.
    /// </summary>
    public string? TrackPath { get; set; }
}

public static class M3uPlaylistFile
{
    public static List<M3uPlaylistEntry> Read(string playlistPath)
    {
        var entries = new List<M3uPlaylistEntry>();
        var playlistDirectory = Path.GetDirectoryName(Path.GetFullPath(playlistPath)) ?? string.Empty;
        var pendingLines = new List<string>();

        foreach (var rawLine in File.ReadLines(playlistPath))
        {
            var line = rawLine.TrimEnd('\r');
            var trimmed = line.Trim();

            if (trimmed.StartsWith("#EXTM3U", StringComparison.OrdinalIgnoreCase))
            {
                // Keep the file header in place instead of letting it travel with the first track.
                AddEntry(entries, [line], null);
                continue;
            }

            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
            {
                pendingLines.Add(line);
                continue;
            }

            pendingLines.Add(line);
            AddEntry(entries, pendingLines, ResolveTrackPath(trimmed, playlistDirectory));
            pendingLines = [];
        }

        if (pendingLines.Count > 0) AddEntry(entries, pendingLines, null);

        return entries;
    }

    public static void Write(string playlistPath, IEnumerable<M3uPlaylistEntry> entries)
    {
        File.WriteAllLines(playlistPath, entries.SelectMany(x => x.RawLines));
    }

    /// <summary>
    /// Returns the library tracks that appear in the playlist, in playlist order.
    /// </summary>
    public static List<ExtendedTrack> GetTracks(IReadOnlyList<M3uPlaylistEntry> entries,
        IReadOnlyList<ExtendedTrack> allTracks)
    {
        var tracksByPath = new Dictionary<string, ExtendedTrack>(StringComparer.OrdinalIgnoreCase);
        foreach (var track in allTracks)
        {
            var path = GetTrackPath(track);
            if (path != null) tracksByPath.TryAdd(path, track);
        }

        var result = new List<ExtendedTrack>();
        var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in entries)
        {
            if (entry.TrackPath is not { } path) continue;
            if (!tracksByPath.TryGetValue(path, out var track)) continue;
            if (seenPaths.Add(path)) result.Add(track);
        }

        return result;
    }

    /// <summary>
    /// Rewrites the playlist entries so the tracks follow <paramref name="displayOrder"/>.
    /// Entries that are not part of the current view keep their original position.
    /// Returns false when the view does not match the playlist (for example while a search filter is active).
    /// </summary>
    public static bool Reorder(IReadOnlyList<ExtendedTrack> displayOrder, List<M3uPlaylistEntry> entries)
    {
        var displayedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var track in displayOrder)
        {
            var path = GetTrackPath(track);
            if (path == null || !displayedPaths.Add(path)) return false;
        }

        var entriesByPath = new Dictionary<string, M3uPlaylistEntry>(StringComparer.OrdinalIgnoreCase);
        var slots = new List<int>();
        for (var i = 0; i < entries.Count; i++)
        {
            var path = entries[i].TrackPath;
            if (path == null || !displayedPaths.Contains(path)) continue;
            if (entriesByPath.TryAdd(path, entries[i])) slots.Add(i);
        }

        if (slots.Count != displayOrder.Count) return false;

        var orderedEntries = new M3uPlaylistEntry[displayOrder.Count];
        for (var i = 0; i < displayOrder.Count; i++)
        {
            var path = GetTrackPath(displayOrder[i]);
            if (path == null || !entriesByPath.TryGetValue(path, out var entry)) return false;
            orderedEntries[i] = entry;
        }

        for (var i = 0; i < slots.Count; i++) entries[slots[i]] = orderedEntries[i];
        return true;
    }

    public static M3uPlaylistEntry CreateTrackEntry(string trackPath)
    {
        var entry = new M3uPlaylistEntry { TrackPath = SafeGetFullPath(trackPath) };
        entry.RawLines.Add(trackPath);
        return entry;
    }

    public static string? GetTrackPath(ExtendedTrack track)
    {
        if (string.IsNullOrEmpty(track.Uri)) return null;
        try
        {
            var uri = new Uri(track.Uri);
            return uri.IsFile ? SafeGetFullPath(uri.LocalPath) : null;
        }
        catch
        {
            var decoded = track.Uri.ToDecodedAbsoluteUri();
            return string.IsNullOrWhiteSpace(decoded) ? null : SafeGetFullPath(decoded);
        }
    }

    private static void AddEntry(List<M3uPlaylistEntry> entries, IEnumerable<string> lines, string? trackPath)
    {
        var entry = new M3uPlaylistEntry { TrackPath = trackPath };
        entry.RawLines.AddRange(lines);
        entries.Add(entry);
    }

    private static string? ResolveTrackPath(string value, string playlistDirectory)
    {
        if (Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            return uri.IsFile ? SafeGetFullPath(uri.LocalPath) : null;
        }

        var localPath = Path.IsPathRooted(value)
            ? value
            : Path.Combine(playlistDirectory, value.Replace('/', Path.DirectorySeparatorChar));
        return SafeGetFullPath(localPath);
    }

    private static string? SafeGetFullPath(string path)
    {
        try
        {
            return Path.GetFullPath(path);
        }
        catch
        {
            return null;
        }
    }
}