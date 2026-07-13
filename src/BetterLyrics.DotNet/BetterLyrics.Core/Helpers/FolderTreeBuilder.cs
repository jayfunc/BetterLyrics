using System.Collections.ObjectModel;
using System.Net;
using BetterLyrics.Core.Models;
using BetterLyrics.Core.Models.Settings;

namespace BetterLyrics.Core.Helpers;

public static class FolderTreeBuilder
{
    public static ObservableCollection<FolderNode> Build(List<ExtendedTrack> tracks,
        List<MediaFolder> folderConfigs)
    {
        var rootNodes = new ObservableCollection<FolderNode>();

        // 按 MediaFolderId 分组
        var folderGroups = tracks.GroupBy(t => t.MediaFolderId);

        foreach (var group in folderGroups)
        {
            var config = folderConfigs.FirstOrDefault(f => f.Id == group.Key);
            if (config == null) continue;

            var baseUri = config.GetStandardUri().AbsoluteUri.TrimEnd('/');

            var rootNode = new FolderNode
            {
                SourceType = config.SourceType,
                FolderName = config.Name ?? config.ConnectionSummary, // 显示用户自定义的名字
                MediaFolderId = group.Key,
                FolderPath = baseUri,
                IsExpanded = true
            };

            foreach (var track in group)
                try
                {
                    if (!track.Uri.StartsWith(baseUri)) continue; // 防御性编程

                    var relativePart = track.Uri.Substring(baseUri.Length);

                    var segments = relativePart
                        .Split('/', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => WebUtility.UrlDecode(s))
                        .ToArray();

                    if (segments.Length > 1) // 长度大于1说明在子文件夹里
                    {
                        var folderSegments = segments.Take(segments.Length - 1).ToArray();
                        CreateFolderStructure(rootNode, folderSegments, baseUri);
                    }
                }
                catch
                {
                }

            rootNodes.Add(rootNode);
        }

        return rootNodes;
    }

    private static void CreateFolderStructure(FolderNode parent, string[] segments, string rootBaseUri)
    {
        var current = parent;
        var currentFullPath = parent.FolderPath;

        foreach (var segmentName in segments)
        {
            var existingChild = current.SubFolders.FirstOrDefault(f => f.FolderName == segmentName);

            currentFullPath += "/" + WebUtility.UrlEncode(segmentName);

            if (existingChild == null)
            {
                var newFolder = new FolderNode
                {
                    FolderName = segmentName,
                    FolderPath = currentFullPath, // 存完整的 URI
                    MediaFolderId = parent.MediaFolderId
                };
                current.SubFolders.Add(newFolder);
                current = newFolder;
            }
            else
            {
                current = existingChild;
                currentFullPath = existingChild.FolderPath;
            }
        }
    }
}