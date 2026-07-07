using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace BetterLyrics.Core.Helpers;

/// <summary>
///     辅助类：使用 MetadataReader 读取 DLL 信息而不锁定文件
/// </summary>
public static class PluginMetadataHelper
{
    public static string? IdentifyPluginId(string folderPath)
    {
        var dllFiles = Directory.GetFiles(folderPath, "*.dll", SearchOption.AllDirectories);

        foreach (var dllPath in dllFiles)
            try
            {
                using var stream = File.OpenRead(dllPath);
                using var peReader = new PEReader(stream);

                if (!peReader.HasMetadata) continue;

                var reader = peReader.GetMetadataReader();
                if (!reader.IsAssembly) continue;

                var assemblyDefinition = reader.GetAssemblyDefinition();
                var assemblyName = reader.GetString(assemblyDefinition.Name);

                if (assemblyName.Contains("BetterLyrics.Plugins") || IsReferencingCore(reader)) return assemblyName;
            }
            catch
            {
            }

        return null;
    }

    private static bool IsReferencingCore(MetadataReader reader)
    {
        foreach (var handle in reader.AssemblyReferences)
        {
            var reference = reader.GetAssemblyReference(handle);
            var refName = reader.GetString(reference.Name);
            if (refName == "BetterLyrics.Core") return true;
        }

        return false;
    }
}