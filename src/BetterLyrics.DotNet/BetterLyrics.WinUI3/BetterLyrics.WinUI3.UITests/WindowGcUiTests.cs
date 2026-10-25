using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Windows.ApplicationModel;
using Windows.Management.Deployment;

namespace BetterLyrics.WinUI3.UITests;

[TestClass]
public sealed class WindowGcUiTests
{
    private const string PackageName = "37412.BetterLyrics";
    private const string ApplicationId = "App";
    private const string CurrentWindowFileName = "current-window.json";
    private const string ResultFileName = "result.json";

    // Keep in sync with WindowGcTestRunner in the app project. Windows without a
    // top-right close button cannot be closed through this user path.
    private static readonly string[] _expectedWindows =
    [
        "SettingsWindow",
        "LyricsSearchWindow",
        "LyricsShareWindow",
        "StatsDashboardWindow",
        "MusicGalleryWindow",
        "NowPlayingWindow"
    ];

    private static readonly TimeSpan _launchTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan _windowCloseTimeout = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan _testTimeout = TimeSpan.FromMinutes(8);
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    [Timeout(9 * 60 * 1000)]
    public void ClosingEveryClosableWindowByTitleBarButton_ReleasesItForGc()
    {
        // The test clicks real screen coordinates; keep Win32 window rectangles in
        // the same physical coordinate space as SetCursorPos on high DPI displays.
        SetProcessDpiAwarenessContext(new IntPtr(-4));

        if (Process.GetProcessesByName("BetterLyrics.WinUI3").Length > 0)
        {
            Assert.Inconclusive("Close every running BetterLyrics instance before running this UI test.");
        }

        var package = FindPackage();
        if (package is null)
        {
            Assert.Inconclusive(
                $"The '{PackageName}' package is not installed for the current user. " +
                "Deploy the BetterLyrics.WinUI3 package before running the UI test.");
        }

        var outputDirectory = Path.Combine(
            Path.GetTempPath(),
            "BetterLyricsUiTests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outputDirectory);

        var currentWindowPath = Path.Combine(outputDirectory, CurrentWindowFileName);
        var resultPath = Path.Combine(outputDirectory, ResultFileName);
        var appUserModelId = $"{package.Id.FamilyName}!{ApplicationId}";
        var arguments = $"--ui-test --ui-test-output \"{outputDirectory}\"";

        TestContext.WriteLine($"Package: {package.Id.FullName}");
        TestContext.WriteLine($"Output:  {outputDirectory}");

        var processId = ActivateApplication(appUserModelId, arguments);
        var process = GetProcess(processId);

        try
        {
            var launchDeadline = DateTime.UtcNow + _launchTimeout;

            while (DateTime.UtcNow < launchDeadline
                && !File.Exists(currentWindowPath)
                && !File.Exists(resultPath))
            {
                Thread.Sleep(100);
            }

            if (!File.Exists(currentWindowPath) && !File.Exists(resultPath))
            {
                Assert.Fail(
                    "BetterLyrics did not start in UI test mode within " +
                    $"{_launchTimeout.TotalSeconds} seconds.");
            }

            var lastProcessedIndex = 0;
            var deadline = DateTime.UtcNow + _testTimeout;

            while (DateTime.UtcNow < deadline && !File.Exists(resultPath))
            {
                var currentWindow = TryReadCurrentWindow(currentWindowPath);

                if (currentWindow is not null
                    && currentWindow.Index > lastProcessedIndex
                    && currentWindow.Phase == "opened")
                {
                    lastProcessedIndex = currentWindow.Index;

                    var handle = new IntPtr(currentWindow.Handle);
                    Assert.IsTrue(
                        IsWindow(handle),
                        $"{currentWindow.WindowType} reported window handle 0x{currentWindow.Handle:X} but it does not exist.");

                    TestContext.WriteLine($"Closing {currentWindow.WindowType} through its close button...");

                    ClickWindowCloseButton(handle, currentWindow.WindowType);
                    WaitUntilWindowClosed(handle, _windowCloseTimeout);
                }

                Thread.Sleep(100);
            }

            if (!File.Exists(resultPath))
            {
                var progress = TryReadProgress(outputDirectory);
                Assert.Fail(
                    $"The app did not produce '{ResultFileName}' within the test timeout." +
                    Environment.NewLine +
                    (progress ?? "No app-side progress was written."));
            }

            var resultJson = File.ReadAllText(resultPath);
            TestContext.WriteLine(resultJson);

            var result = JsonSerializer.Deserialize<UiTestResultDto>(resultJson, _jsonOptions);
            Assert.IsNotNull(result, "The app produced an unreadable test result.");

            if (result.Error is not null)
            {
                Assert.Fail(result.Error);
            }

            Assert.AreEqual(
                _expectedWindows.Length,
                result.Windows.Count,
                "The app did not run the GC check for every window.");

            foreach (var expectedWindow in _expectedWindows)
            {
                var windowResult = result.Windows.SingleOrDefault(x => x.WindowType == expectedWindow);
                Assert.IsNotNull(windowResult, $"{expectedWindow} was not tested.");

                Assert.IsTrue(
                    windowResult.Closed,
                    $"{expectedWindow} was not closed through its close button.");

                Assert.IsNull(
                    windowResult.Error,
                    $"{expectedWindow} failed while the test was running: {windowResult.Error}");

                Assert.IsTrue(
                    windowResult.Collected,
                    $"{expectedWindow} was not garbage collected. " +
                    $"Still alive: {string.Join(", ", windowResult.AliveObjects)}");
            }

            Assert.IsTrue(result.Success, "The app reported that the GC test failed.");
        }
        finally
        {
            if (process is not null)
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill(entireProcessTree: true);
                    }
                }
                catch
                {
                    // The app normally exits by itself after writing result.json.
                }
            }
        }
    }

    private static void ClickWindowCloseButton(IntPtr handle, string windowType)
    {
        Assert.IsTrue(
            GetWindowRect(handle, out var bounds),
            $"{windowType} has no screen bounds, so its close button cannot be clicked.");

        var dpi = GetDpiForWindow(handle);
        var scale = dpi == 0 ? 1.0 : dpi / 96.0;

        // The close button is always the right-most caption button. Clicking its
        // real screen location (instead of using UIA) avoids creating automation
        // peers that would keep the window's XAML tree alive during the GC check.
        var hoverX = bounds.Right - (int)Math.Round(100 * scale);
        var hoverY = bounds.Top + (int)Math.Round(18 * scale);
        var closeX = bounds.Right - (int)Math.Round(40 * scale);
        var closeY = bounds.Top + (int)Math.Round(23 * scale);

        // NowPlayingWindow uses XAML caption buttons and can still be in the
        // background. Bring it forward so the first click is delivered to the
        // button instead of only activating the window. Hovering first also makes
        // its auto-hidden title bar commands visible.
        SetForegroundWindow(handle);
        Thread.Sleep(300);
        MoveMouse(hoverX, hoverY);
        Thread.Sleep(600);

        MoveMouse(closeX, closeY);
        Thread.Sleep(150);
        ClickLeftMouseButton();

        // The pointer is intentionally left on the close button. This is the exact
        // scenario that used to keep a WinUI window alive.
    }

    private static void MoveMouse(int x, int y)
    {
        SetCursorPos(x, y);
    }

    private static void ClickLeftMouseButton()
    {
        mouse_event(MouseEventFlags.LeftDown, 0, 0, 0, UIntPtr.Zero);
        Thread.Sleep(60);
        mouse_event(MouseEventFlags.LeftUp, 0, 0, 0, UIntPtr.Zero);
    }

    private static void WaitUntilWindowClosed(IntPtr handle, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            if (!IsWindow(handle))
            {
                return;
            }

            Thread.Sleep(100);
        }

        Assert.Fail($"Window 0x{handle.ToInt64():X} did not close within {timeout.TotalSeconds} seconds.");
    }

    private static CurrentWindowDto? TryReadCurrentWindow(string path)
    {
        try
        {
            if (!File.Exists(path)) return null;

            return JsonSerializer.Deserialize<CurrentWindowDto>(
                File.ReadAllText(path),
                _jsonOptions);
        }
        catch (IOException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? TryReadProgress(string outputDirectory)
    {
        var progressPath = Path.Combine(outputDirectory, "progress.json");
        return File.Exists(progressPath) ? File.ReadAllText(progressPath) : null;
    }

    private static Package? FindPackage()
    {
        try
        {
            var packages = new PackageManager()
                .FindPackagesForUser(string.Empty)
                .Where(x => string.Equals(x.Id.Name, PackageName, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(x => x.InstalledDate)
                .ToList();

            // Prefer the loose development package when both a Store install and a
            // side-by-side development deployment are present.
            return packages.FirstOrDefault(x => x.IsDevelopmentMode)
                ?? packages.FirstOrDefault();
        }
        catch (Exception ex)
        {
            Assert.Inconclusive($"Unable to enumerate installed packages: {ex.Message}");
            return null;
        }
    }

    private static uint ActivateApplication(string appUserModelId, string arguments)
    {
        uint processId = 0;
        Exception? failure = null;

        var thread = new Thread(() =>
        {
            try
            {
                var activationManager = (IApplicationActivationManager)new ApplicationActivationManager();
                var result = activationManager.ActivateApplication(
                    appUserModelId,
                    arguments,
                    ActivateOptions.None,
                    out processId);

                Marshal.ThrowExceptionForHR(result);
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        })
        {
            IsBackground = true
        };

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (failure is not null)
        {
            throw new InvalidOperationException(
                $"Failed to launch '{appUserModelId}'.",
                failure);
        }

        if (processId == 0)
        {
            throw new InvalidOperationException(
                $"Windows did not return a process id when launching '{appUserModelId}'.");
        }

        return processId;
    }

    private static Process? GetProcess(uint processId)
    {
        try
        {
            return Process.GetProcessById((int)processId);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetProcessDpiAwarenessContext(IntPtr value);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    private static extern void mouse_event(
        MouseEventFlags flags,
        uint dx,
        uint dy,
        uint data,
        UIntPtr extraInfo);

    [Flags]
    private enum MouseEventFlags : uint
    {
        LeftDown = 0x0002,
        LeftUp = 0x0004
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    private sealed class CurrentWindowDto
    {
        public int Index { get; set; }

        public string WindowType { get; set; } = string.Empty;

        public long Handle { get; set; }

        public string Phase { get; set; } = string.Empty;
    }

    private sealed class UiTestResultDto
    {
        public bool Success { get; set; }

        public string? Error { get; set; }

        public List<WindowGcResultDto> Windows { get; set; } = [];
    }

    private sealed class WindowGcResultDto
    {
        public int Index { get; set; }

        public string WindowType { get; set; } = string.Empty;

        public bool Closed { get; set; }

        public bool Collected { get; set; }

        public bool WindowCollected { get; set; }

        public List<string> AliveObjects { get; set; } = [];

        public string? Error { get; set; }
    }

    private enum ActivateOptions
    {
        None = 0,
        DesignMode = 1,
        NoErrorUI = 2,
        NoSplashScreen = 4
    }

    [ComImport]
    [Guid("2e941141-7f97-4756-ba1d-9decde894a3d")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IApplicationActivationManager
    {
        [PreserveSig]
        int ActivateApplication(
            [MarshalAs(UnmanagedType.LPWStr)] string appUserModelId,
            [MarshalAs(UnmanagedType.LPWStr)] string? arguments,
            ActivateOptions options,
            out uint processId);

        [PreserveSig]
        int ActivateForFile(
            [MarshalAs(UnmanagedType.LPWStr)] string appUserModelId,
            IntPtr itemArray,
            [MarshalAs(UnmanagedType.LPWStr)] string verb,
            out uint processId);

        [PreserveSig]
        int ActivateForProtocol(
            [MarshalAs(UnmanagedType.LPWStr)] string appUserModelId,
            IntPtr itemArray,
            out uint processId);
    }

    [ComImport]
    [Guid("45BA127D-10A8-46EA-8AB7-56EA9078943C")]
    private class ApplicationActivationManager
    {
    }
}
