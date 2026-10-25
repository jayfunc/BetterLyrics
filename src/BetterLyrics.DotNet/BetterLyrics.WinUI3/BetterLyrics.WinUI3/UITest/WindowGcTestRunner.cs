using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Events;
using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using WinRT.Interop;
using WinUIEx;

namespace BetterLyrics.WinUI3.UITest;

/// <summary>
/// Opens every user-facing window, waits until the UI test clicks its title bar close
/// button, then verifies that the window and its view objects can be garbage collected.
/// Windows are never closed from this code: the only way a window leaves the test is
/// the close button click performed by BetterLyrics.WinUI3.UITests.
/// </summary>
internal static class WindowGcTestRunner
{
    // LyricsWindowSwitchWindow, SystemTrayWindow and the toast overlay windows do
    // not expose a top-right close button, so they cannot be exercised through the
    // user closing path covered by this test.
    private static readonly WindowType[] _windowTypes =
    [
        WindowType.SettingsWindow,
        WindowType.LyricsSearchWindow,
        WindowType.LyricsShareWindow,
        WindowType.StatsDashboardWindow,
        WindowType.MusicGalleryWindow,
        WindowType.NowPlayingWindow
    ];

    private static readonly TimeSpan _closeTimeout = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan _gcTimeout = TimeSpan.FromSeconds(45);
    private static readonly TimeSpan _trackingQuietTime = TimeSpan.FromSeconds(1);
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static async Task RunAsync()
    {
        var outputDirectory = UiTestSession.OutputDirectory
            ?? throw new InvalidOperationException("UI test output directory is not configured.");

        var result = new UiTestResult();
        var trackedObjects = new List<MemoryLeakObjectTrackedEventArgs>();

        void OnObjectTracked(object? sender, MemoryLeakObjectTrackedEventArgs args)
        {
            lock (trackedObjects)
            {
                trackedObjects.Add(args);
            }
        }

        MemoryLeakDetector.ObjectTracked += OnObjectTracked;

        try
        {
            WindowSession? currentSession = null;
            WindowType currentType = default;

            for (var i = 0; i < _windowTypes.Length; i++)
            {
                currentType = _windowTypes[i];

                if (currentSession is null)
                {
                    currentSession = await OpenWindowAndReportAsync(
                        i + 1,
                        currentType,
                        outputDirectory);
                }

                // The test clicks the close button; this code never closes the window.
                await currentSession.ClosedTask.WaitAsync(_closeTimeout);

                await WaitForTrackingToSettleAsync(trackedObjects);
                var trackedSnapshot = TakeTrackedSnapshot(trackedObjects);

                WindowSession? nextSession = null;
                if (i + 1 < _windowTypes.Length)
                {
                    // WinUI keeps the previous window's XAML tree alive until another
                    // window is created. Open the next window before checking GC so
                    // the test observes the same lifecycle as normal app usage.
                    nextSession = await OpenWindowAndReportAsync(
                        i + 2,
                        _windowTypes[i + 1],
                        outputDirectory);
                }

                var windowResult = await CheckClosedWindowAsync(
                    i + 1,
                    currentType,
                    currentSession,
                    trackedSnapshot);

                result.Windows.Add(windowResult);
                WriteJson(outputDirectory, "progress.json", result);

                currentSession = nextSession;
            }
        }
        catch (Exception ex)
        {
            result.Error = ex.ToString();
        }
        finally
        {
            MemoryLeakDetector.ObjectTracked -= OnObjectTracked;
        }

        result.Success = result.Error is null
            && result.Windows.Count == _windowTypes.Length
            && result.Windows.All(x => x.Collected);

        WriteJson(outputDirectory, "result.json", result);

        Program.DisposeServices();
        Environment.Exit(result.Success ? 0 : 1);
    }

    private static async Task<WindowSession> OpenWindowAndReportAsync(
        int index,
        WindowType windowType,
        string outputDirectory)
    {
        var session = ShowWindow(windowType);

        // Give XAML a moment to finish its first layout before the UI test clicks.
        await Task.Delay(600);

        WriteJson(outputDirectory, "current-window.json", new CurrentWindowState
        {
            Index = index,
            WindowType = windowType.ToString(),
            Handle = session.Handle.ToInt64(),
            Phase = "opened"
        });

        return session;
    }

    private static IReadOnlyList<MemoryLeakObjectTrackedEventArgs> TakeTrackedSnapshot(
        List<MemoryLeakObjectTrackedEventArgs> trackedObjects)
    {
        lock (trackedObjects)
        {
            var snapshot = trackedObjects.ToList();
            trackedObjects.Clear();
            return snapshot;
        }
    }

    private static async Task<WindowGcResult> CheckClosedWindowAsync(
        int index,
        WindowType windowType,
        WindowSession session,
        IReadOnlyList<MemoryLeakObjectTrackedEventArgs> trackedSnapshot)
    {
        var result = new WindowGcResult
        {
            Index = index,
            WindowType = windowType.ToString(),
            Closed = true
        };

        var references = trackedSnapshot
            .Select(x => new TrackedReference(x.Name, x.Reference))
            .ToList();

        // BaseWindow tracks itself, but keep an extra reference so the window is
        // always part of the assertion even if tracking changes in the future.
        references.Add(new TrackedReference($"{windowType}Window", session.WindowReference));

        result.Collected = await WaitForCollectionAsync(references);
        result.WindowCollected = !session.WindowReference.IsAlive;
        result.TrackedObjects = [.. references
            .Select(x => new TrackedObjectResult
            {
                Name = x.Name,
                Collected = !x.Reference.IsAlive
            })
            .OrderBy(x => x.Name)];

        result.AliveObjects = [.. result.TrackedObjects
            .Where(x => !x.Collected)
            .Select(x => x.Name)];

        result.ElapsedMilliseconds = session.Stopwatch.ElapsedMilliseconds;

        return result;
    }

    /// <summary>
    /// Creates and activates a window, then returns only a weak reference to it.
    /// Keeping the strong reference inside this synchronous method ensures the async
    /// state machine cannot accidentally keep the window alive during the GC check.
    /// </summary>
    private static WindowSession ShowWindow(WindowType windowType)
    {
        var windowManager = Ioc.Default.GetRequiredService<IWindowManagerProvider>();

        var opened = windowType == WindowType.NowPlayingWindow
            ? windowManager.OpenOrShowWindow<NowPlayingWindow>(new LyricsWindowStatus(LyricsWindowMode.Standard))
            : windowManager.OpenOrShowWindow(windowType) as Window
                ?? throw new InvalidOperationException($"Failed to open {windowType}.");

        var window = (Window)opened;
        var handle = WindowNative.GetWindowHandle(window);
        var windowReference = new WeakReference(window);
        var stopwatch = Stopwatch.StartNew();

        var closedTask = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        window.Closed += (_, _) => closedTask.TrySetResult();

        // NowPlayingWindow is intentionally not activated by the window manager so it
        // does not flash on startup. Activate it here so the UI test can click it.
        if (windowType == WindowType.NowPlayingWindow)
        {
            window.Activate();
            window.SetForegroundWindow();
        }

        window = null;

        return new WindowSession(handle, windowReference, closedTask.Task, stopwatch);
    }

    private static async Task WaitForTrackingToSettleAsync(List<MemoryLeakObjectTrackedEventArgs> trackedObjects)
    {
        var lastCount = -1;
        var lastChange = DateTime.UtcNow;
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);

        while (DateTime.UtcNow < deadline)
        {
            int count;
            lock (trackedObjects)
            {
                count = trackedObjects.Count;
            }

            if (count != lastCount)
            {
                lastCount = count;
                lastChange = DateTime.UtcNow;
            }

            if (count > 0 && DateTime.UtcNow - lastChange >= _trackingQuietTime)
            {
                return;
            }

            await Task.Delay(100);
        }
    }

    private static async Task<bool> WaitForCollectionAsync(IReadOnlyList<TrackedReference> references)
    {
        var deadline = DateTime.UtcNow + _gcTimeout;

        while (true)
        {
            await Task.Run(ForceGc);

            if (references.All(x => !x.Reference.IsAlive))
            {
                return true;
            }

            if (DateTime.UtcNow >= deadline)
            {
                return false;
            }

            await Task.Delay(250);
        }
    }

    private static void ForceGc()
    {
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
    }

    private static void WriteJson<T>(string outputDirectory, string fileName, T value)
    {
        var path = Path.Combine(outputDirectory, fileName);
        var temporaryPath = $"{path}.tmp";

        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(value, _jsonOptions));
        File.Move(temporaryPath, path, overwrite: true);
    }

    private sealed record WindowSession(
        IntPtr Handle,
        WeakReference WindowReference,
        Task ClosedTask,
        Stopwatch Stopwatch);

    private sealed record TrackedReference(string Name, WeakReference Reference);

    private sealed class UiTestResult
    {
        public bool Success { get; set; }

        public string? Error { get; set; }

        public List<WindowGcResult> Windows { get; } = [];
    }

    private sealed class WindowGcResult
    {
        public int Index { get; set; }

        public string WindowType { get; set; } = string.Empty;

        public bool Closed { get; set; }

        public bool Collected { get; set; }

        public bool WindowCollected { get; set; }

        public long ElapsedMilliseconds { get; set; }

        public List<TrackedObjectResult> TrackedObjects { get; set; } = [];

        public List<string> AliveObjects { get; set; } = [];

        public string? Error { get; set; }
    }

    private sealed class TrackedObjectResult
    {
        public string Name { get; set; } = string.Empty;

        public bool Collected { get; set; }
    }

    private sealed class CurrentWindowState
    {
        public int Index { get; set; }

        public string WindowType { get; set; } = string.Empty;

        public long Handle { get; set; }

        public string Phase { get; set; } = string.Empty;
    }
}
