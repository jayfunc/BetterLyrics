using System.Threading.Tasks;
using Avalonia;
using Avalonia.Browser;

namespace BetterLyrics.Avalonia.Browser;

internal sealed class Program
{
    private static Task Main(string[] args) => BuildAvaloniaApp()
        .WithInterFont()
#if DEBUG
        .WithDeveloperTools()
#endif
        .StartBrowserAppAsync("out");

    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>();
    }
}