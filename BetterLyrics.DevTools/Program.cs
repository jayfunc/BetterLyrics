using BetterLyrics.DevTools.Generators;
using System.Reflection;

namespace BetterLyrics.DevTools
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: BetterLyrics.DevTools.exe <DllPath> [Mode] [SourceDir]");
                return;
            }

            string dllPath = Path.GetFullPath(args[0]);
            string mode = args.Length > 1 ? args[1] : "All";
            string pluginDir = Path.GetDirectoryName(dllPath)!;
            string sourceRootDir = args.Length > 2
                ? args[2].Replace("\"", "").Trim()
                : pluginDir;

            if (!File.Exists(dllPath))
            {
                Console.WriteLine($"File not found: {dllPath}");
                return;
            }

            Console.WriteLine($"=== Plugin Analyzer: {Path.GetFileName(dllPath)} ===");
            Console.WriteLine($"Target Source Dir: {sourceRootDir}");

            try
            {
                if (mode == "Trim" || mode == "All")
                {
                    TrimmerGenerator.Run(dllPath, pluginDir);
                }

                if (mode == "Lang" || mode == "All")
                {
                    var asm = Assembly.LoadFrom(dllPath);

                    LangResourceGenerator.Run(asm, sourceRootDir);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                Environment.Exit(1);
            }
        }
    }
}