using System;
using System.IO;

namespace BetterLyrics.WinUI3.UITest;

/// <summary>
/// Command line contract shared with BetterLyrics.WinUI3.UITests.
/// </summary>
internal static class UiTestSession
{
    private const string EnableSwitch = "--ui-test";
    private const string OutputSwitch = "--ui-test-output";

    public static bool IsEnabled { get; private set; }

    public static string? OutputDirectory { get; private set; }

    public static void Initialize(string[] args)
    {
        var enableIndex = Array.FindIndex(
            args,
            x => string.Equals(x, EnableSwitch, StringComparison.OrdinalIgnoreCase));

        if (enableIndex < 0) return;

        OutputDirectory = ReadOption(args, OutputSwitch)
            ?? Path.Combine(Path.GetTempPath(), $"BetterLyricsUiTest_{DateTime.Now:yyyyMMdd_HHmmss}");

        Directory.CreateDirectory(OutputDirectory);

        IsEnabled = true;
    }

    private static string? ReadOption(string[] args, string option)
    {
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];

            if (arg.StartsWith($"{option}=", StringComparison.OrdinalIgnoreCase))
            {
                return arg[(option.Length + 1)..].Trim('"');
            }

            if (string.Equals(arg, option, StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                return args[i + 1].Trim('"');
            }
        }

        return null;
    }
}
