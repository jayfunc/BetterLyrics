# BetterLyrics.WinUI3.UITests

End-to-end UI test for window lifetime and garbage collection.

The test launches the packaged app in UI-test mode, opens every user-facing window
one by one, and closes each window with a real mouse click on its top-right close
button. The app-side test host then checks that the closed window and all XAML
objects tracked by `MemoryLeakDetector` were collected.

## Covered windows

- SettingsWindow
- LyricsSearchWindow
- LyricsShareWindow
- StatsDashboardWindow
- MusicGalleryWindow
- NowPlayingWindow

`LyricsWindowSwitchWindow`, `SystemTrayWindow`, and the toast overlay windows do not
expose a top-right close button, so they are not part of this close-button test.

## Prerequisites

1. Close every running BetterLyrics instance.
2. Build and deploy the `BetterLyrics.WinUI3 (Package)` project, including the
   current app-side test host (`UITest/` in the WinUI project).
3. Run the test on an interactive desktop session. The test moves the real mouse
   cursor and clicks the close buttons, so it cannot run in a headless session.

The test is marked `Inconclusive` when the package is not installed.

## Run

```powershell
dotnet test src/BetterLyrics.DotNet/BetterLyrics.WinUI3/BetterLyrics.WinUI3.UITests/BetterLyrics.WinUI3.UITests.csproj `
  -c Debug -p:Platform=x64
```

The app-side host writes progress to a temporary directory under
`%TEMP%\BetterLyricsUiTests`. The test prints that path to the test output; the final
`result.json` contains every tracked object, whether it was collected, and the
time spent.

## Troubleshooting

- Run `dotnet test` against the test project, not `BetterLyrics.slnx`. The solution
  contains the MSIX `.wapproj`, which the .NET SDK cannot evaluate outside Visual
  Studio and fails with `MSB4019` (`Microsoft.DesktopBridge.props` not found).
- The test project targets x64 only. In Visual Studio, select the x64 solution
  platform (the solution maps x86/ARM64 to the x64 test project as well) and rebuild
  the test project before opening Test Explorer.
- If Test Explorer still shows the old assembly location after moving the project,
  clear its cache or rebuild the solution.
