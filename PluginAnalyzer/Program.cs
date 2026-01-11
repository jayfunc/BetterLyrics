using Mono.Cecil;
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;

class Program
{
    // Global sets
    static HashSet<string> _mergedAssemblies = new HashSet<string>();
    static HashSet<string> _mergedSystemTypes = new HashSet<string>();
    static string _currentScanDirectory = "";

    static void Main(string[] args)
    {
        // =========================================================
        // 🚀 MODE 1: CLI Automation Mode (被 VS 编译调用时)
        // =========================================================
        if (args.Length > 0)
        {
            try
            {
                string scanPath = args[0].Replace("\"", "").Trim();
                string targetNs = args.Length > 1 ? args[1] : "BetterLyrics.WinUI3";
                string prefix = args.Length > 2 ? args[2] : "Plugin";

                // 读取第4个参数作为输出目录，如果没传，就默认用扫描目录
                string outputDir = args.Length > 3 ? args[3].Replace("\"", "").Trim() : scanPath;

                Console.WriteLine($"[Analyzer] Scanning: {scanPath}");
                Console.WriteLine($"[Analyzer] Output to: {outputDir}");

                RunBatch(scanPath, targetNs, prefix, outputDir, silent: true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Analyzer] Error: {ex.Message}");
                Environment.Exit(1);
            }
            return;
        }

        // =========================================================
        // 🚀 MODE 2: Interactive Mode (手动双击运行)
        // =========================================================
        Console.WriteLine("==================================================");
        Console.WriteLine("   Plugin Dependency Analyzer (Auto-CLI Ready)    ");
        Console.WriteLine("==================================================");

        while (true)
        {
            _mergedAssemblies.Clear();
            _mergedSystemTypes.Clear();

            Console.WriteLine("\n1. Enter Plugin FOLDER path (or 'exit'):");
            Console.Write("> ");
            string input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input)) continue;
            if (input.Trim().ToLower() == "exit") break;

            string path = input.Replace("\"", "").Trim();
            if (!Directory.Exists(path)) { Console.WriteLine("[ERROR] Folder not found."); continue; }

            Console.WriteLine("2. Enter Namespace (Default: BetterLyrics.WinUI3):");
            Console.Write("> ");
            string ns = Console.ReadLine().Trim();
            if (string.IsNullOrWhiteSpace(ns)) ns = "BetterLyrics.WinUI3";

            string prefix = new DirectoryInfo(path).Name;
            Console.WriteLine($"3. Enter prefix (Default: {prefix}):");
            Console.Write("> ");
            string p = Console.ReadLine().Trim();
            if (!string.IsNullOrWhiteSpace(p)) prefix = p;

            // 👇👇👇【修复点在这里】👇👇👇
            // 交互模式下，我们默认把文件生成在 exe 旁边的 AnalyzerOutput 文件夹里
            string interactiveOutputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AnalyzerOutput");

            // 传入 interactiveOutputDir 以匹配新的方法签名
            RunBatch(path, ns, prefix, interactiveOutputDir, silent: false);
        }
    }

    // ---------------------------------------------------------
    // 下面的 RunBatch 和 GenerateFiles 签名已更新支持 outputDir
    // ---------------------------------------------------------

    static void RunBatch(string scanPath, string ns, string prefix, string outputDir, bool silent)
    {
        _currentScanDirectory = scanPath;
        _mergedAssemblies.Clear();
        _mergedSystemTypes.Clear();

        ProcessDirectory(scanPath);

        // 传递 outputDir
        GenerateFiles(prefix, ns, outputDir, silent);
    }

    static void GenerateFiles(string prefix, string rootNamespace, string outputDir, bool silent)
    {
        Directory.CreateDirectory(outputDir);

        string xmlFileName = $"{prefix}_TrimmerRoots.xml";
        string csFileName = $"{prefix}_TrimmingConfig.cs";

        // 👇 关键修改：区分 Assembly 名和 Namespace 名
        string assemblyName = rootNamespace; // DLL 名字 (例如 BetterLyrics.WinUI3)
        string codeNamespace = $"{rootNamespace}.PluginConfigs"; // C# 命名空间 (例如 BetterLyrics.WinUI3.PluginConfigs)

        // 类名和全名
        string configClassName = $"{prefix.Replace(".", "_").Replace(" ", "_")}_Config";
        string fullConfigClassName = $"{codeNamespace}.{configClassName}";

        // 路径变量
        string xmlPath = Path.Combine(outputDir, xmlFileName);
        string csPath = Path.Combine(outputDir, csFileName);

        // ==========================================
        // 1. Generate XML
        // ==========================================
        var xmlBuilder = new StringBuilder();
        xmlBuilder.AppendLine("");
        xmlBuilder.AppendLine("<linker>");

        // A. 保护插件依赖
        if (_mergedAssemblies.Count > 0)
        {
            foreach (var asm in _mergedAssemblies.OrderBy(n => n))
                xmlBuilder.AppendLine($"  <assembly fullname=\"{asm}\" preserve=\"all\" />");
        }

        // B. 保护生成的 Config 类
        // ⚠️ 注意：assembly fullname 必须是 rootNamespace (DLL名)，而不是 codeNamespace
        xmlBuilder.AppendLine($"  <assembly fullname=\"{assemblyName}\">");
        xmlBuilder.AppendLine($"    <type fullname=\"{fullConfigClassName}\" preserve=\"all\" />");
        xmlBuilder.AppendLine($"  </assembly>");

        xmlBuilder.AppendLine("</linker>");

        // ==========================================
        // 2. Generate C#
        // ==========================================
        var csBuilder = new StringBuilder();
        csBuilder.AppendLine("// Auto-Generated by PluginAnalyzer");
        csBuilder.AppendLine("using System.Diagnostics.CodeAnalysis;");
        csBuilder.AppendLine("using System.Runtime.CompilerServices;");
        csBuilder.AppendLine("");

        // 👇 这里使用带后缀的命名空间
        csBuilder.AppendLine($"namespace {codeNamespace};");

        csBuilder.AppendLine($"internal static class {configClassName}");
        csBuilder.AppendLine("{");

        foreach (var type in _mergedSystemTypes.OrderBy(n => n))
        {
            csBuilder.AppendLine($"    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof({type}))]");
        }

        csBuilder.AppendLine("    [ModuleInitializer]");
        csBuilder.AppendLine("    internal static void Initialize()");
        csBuilder.AppendLine("    {");
        csBuilder.AppendLine("        // This method runs automatically on startup.");
        csBuilder.AppendLine("    }");
        csBuilder.AppendLine("}");

        // ==========================================
        // 3. Write & Notify
        // ==========================================
        File.WriteAllText(xmlPath, xmlBuilder.ToString());
        File.WriteAllText(csPath, csBuilder.ToString());

        if (!silent)
        {
            Console.WriteLine($"[Analyzer] Generated: {xmlFileName}");
            Process.Start("explorer.exe", outputDir);
        }
        else
        {
            Console.WriteLine($"[Analyzer] Created: {xmlPath}");
            Console.WriteLine($"[Analyzer] Created: {csPath}");
        }
    }

    // ---------------------------------------------------------
    // Helpers (逻辑保持不变)
    // ---------------------------------------------------------

    static void ProcessDirectory(string folderPath)
    {
        var files = Directory.GetFiles(folderPath, "*.dll");
        foreach (var file in files)
        {
            if (!IsHostOrSystemBinary(Path.GetFileName(file))) AnalyzeDll(file);
        }
    }

    static void AnalyzeDll(string path)
    {
        try
        {
            var resolver = new DefaultAssemblyResolver();
            resolver.AddSearchDirectory(Path.GetDirectoryName(path));
            using var assembly = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { AssemblyResolver = resolver });

            foreach (var typeRef in assembly.MainModule.GetTypeReferences())
            {
                if (typeRef.Scope is AssemblyNameReference asmRef && !IsIgnoredDependency(asmRef.Name) && !IsLocalDll(asmRef.Name))
                    _mergedAssemblies.Add(asmRef.Name);

                if (typeRef.FullName.StartsWith("System.") || typeRef.FullName.StartsWith("Microsoft.Win32"))
                    _mergedSystemTypes.Add(FormatTypeName(typeRef));
            }
        }
        catch { }
    }

    static bool IsLocalDll(string asmName) => File.Exists(Path.Combine(_currentScanDirectory, asmName + ".dll"));

    static bool IsHostOrSystemBinary(string fileName)
    {
        fileName = fileName.ToLower();
        return fileName.StartsWith("betterlyrics.") && !fileName.Contains("plugin") ||
               fileName.StartsWith("microsoft.windows") || fileName.StartsWith("winrt.") || fileName.StartsWith("system.");
    }

    static bool IsIgnoredDependency(string asmName) => asmName.Contains("BetterLyrics") || asmName.Contains("WindowsAppSDK") || asmName.Contains("Microsoft.Windows") || asmName == "netstandard" || asmName == "mscorlib";

    static string FormatTypeName(TypeReference type)
    {
        string name = type.FullName;

        // 1. 处理嵌套类型 (Cecil 用 '/'，C# 用 '.')
        name = name.Replace("/", ".");

        // 2. 处理泛型 (核心修复：根据参数数量动态生成逗号)
        // List`1 -> List<>
        // Dictionary`2 -> Dictionary<,>
        // Tuple`3 -> Tuple<,,>
        if (name.Contains("`"))
        {
            name = Regex.Replace(name, @"`(\d+)", match =>
            {
                int argsCount = int.Parse(match.Groups[1].Value);
                // 逗号数量 = 参数数量 - 1
                // 比如 1个参数=0个逗号 (<>)
                // 比如 2个参数=1个逗号 (<,>)
                return "<" + new string(',', Math.Max(0, argsCount - 1)) + ">";
            });
        }

        return name;
    }
}