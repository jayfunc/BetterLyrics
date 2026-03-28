# Windows Media Controller
[![NuGet](https://img.shields.io/nuget/vpre/Dubya.WindowsMediaController.svg)](https://nuget.org/packages/Dubya.WindowsMediaController)

This repository provides a wrapper for developers to more easily get information from and interact with the Windows 10/11 OS media interface (Also referred to Windows System Media Transport Controls (SMTC)). 

| Windows 10    | Windows 11    |
| ------------- | ------------- |
| ![Windows 10 Media Interface](https://raw.githubusercontent.com/DubyaDude/WindowsMediaController/master/docs/images/Win10.png)  | ![Windows 11 Media Interface](https://raw.githubusercontent.com/DubyaDude/WindowsMediaController/master/docs/images/Win11.png)  |

This allows for a better understanding and control of the Media Sessions and can have many different applications. Some features include:
- Control playback on individual Media Sessions (Spotify, Chrome, etc)
- Get media information of currently playing (Song, Author, Thumbnail, etc)

## Requirements
- Windows 10 (Build 17763+) or Windows 11
- The ability to talk to Windows Runtime. (In a majority of cases, this will not be an issue)
- .NET Framework 4.6.1+ or .NET 5+
- May need to be able to interact with the desktop
  - In situations such as being run through Windows Task Scheduler, the application will need an active window to start with, you can hide it afterward.
### NET Framework:
For .NET Framework, I've seen people encountering issues with how the package gets imported. This is caused by using an older format of .NET projects (you can see the newer format being used in [Sample.CMD](https://github.com/DubyaDude/WindowsMediaController/blob/master/Sample.CMD/Sample.CMD.csproj)). If you have this issue, add the package by adding this to the .csproj file.
<br> (replacing '2.5.6' with the preferred NuGet version)
```csproj
<ItemGroup>
  <PackageReference Include="Dubya.WindowsMediaController">
    <Version>2.5.6</Version>
  </PackageReference>
</ItemGroup>
```
### NET 5+:
NET 5 brought along a lot of changes in how WinRT is meant to be accessed. More of that info can be found [here](https://docs.microsoft.com/en-us/windows/apps/desktop/modernize/desktop-to-uwp-enhance).

If you're doing a GUI app you **should** be good to go and be able to just import the lib.

However, for other cases, your `TargetFramework` in the .csproj file needs to be modified before importing the package.
<br> (replacing net6.0 with desired .NET version)
```csproj
<TargetFramework>net6.0-windows10.0.22000.0</TargetFramework>
```

## How To Use
### Initialization:
```csharp
mediaManager = new MediaManager();

mediaManager.OnAnySessionOpened += MediaManager_OnAnySessionOpened;
mediaManager.OnAnySessionClosed += MediaManager_OnAnySessionClosed;
mediaManager.OnFocusedSessionChanged += MediaManager_OnFocusedSessionChanged;
mediaManager.OnAnyMediaPropertyChanged += MediaManager_OnAnyMediaPropertyChanged;
mediaManager.OnAnyPlaybackStateChanged += MediaManager_OnAnyPlaybackStateChanged;
mediaManager.OnAnyTimelinePropertyChanged += MediaManager_OnAnyTimelinePropertyChanged;

mediaManager.Start();
OR
await mediaManager.StartAsync();
```

### Class Structure:
MediaManager:
```csharp
ReadOnlyDictionary<string, MediaSession> CurrentMediaSessions;
bool IsStarted { get; }
GlobalSystemMediaTransportControlsSessionManager WindowsSessionManager { get; }

void Start();
async Task StartAsync();
MediaSession GetFocusedSession();
void ForceUpdate();

delegate void OnAnySessionOpened(MediaManager.MediaSession session);
delegate void OnAnySessionClosed(MediaManager.MediaSession session);
delegate void OnFocusedSessionChanged(MediaManager.MediaSession session);
delegate void OnAnyMediaPropertyChanged(MediaManager.MediaSession sender, GlobalSystemMediaTransportControlsSessionMediaProperties args);
delegate void OnAnyPlaybackStateChanged(MediaManager.MediaSession sender, GlobalSystemMediaTransportControlsSessionPlaybackInfo args);
delegate void OnAnyTimelinePropertyChanged(MediaSession mediaSession, GlobalSystemMediaTransportControlsSessionTimelineProperties timelineProperties);
```
MediaManager.MediaSession:
```csharp
readonly string Id;
GlobalSystemMediaTransportControlsSession ControlSession { get; }

delegate void OnSessionClosed(MediaManager.MediaSession session);
delegate void OnMediaPropertyChanged(MediaManager.MediaSession sender, GlobalSystemMediaTransportControlsSessionMediaProperties args);
delegate void OnPlaybackStateChanged(MediaManager.MediaSession sender, GlobalSystemMediaTransportControlsSessionPlaybackInfo args);
delegate void OnTimelinePropertyChanged(MediaSession mediaSession, GlobalSystemMediaTransportControlsSessionTimelineProperties timelineProperties);
```

### Getting Some Info:

- Getting PlaybackInfo (Seeing what actions are available/Is paused or playing, etc)
  - Returns: GlobalSystemMediaTransportControlsSessionPlaybackInfo
  - ``mediaSession.ControlSession.GetPlaybackInfo()``

- Getting current MediaProperties (Currently playing title, author, thumbnail, etc)
  - Returns: GlobalSystemMediaTransportControlsSessionMediaProperties
  - ``await mediaSession.ControlSession.TryGetMediaPropertiesAsync()``

### Useful Microsoft Documentations:
- [GlobalSystemMediaTransportControlsSessionManager](https://docs.microsoft.com/en-us/uwp/api/windows.media.control.globalsystemmediatransportcontrolssessionmanager) - Located in `MediaManager.WindowsSessionManager`. This class allows for events whenever a source's state changes.
- [GlobalSystemMediaTransportControlsSession](https://docs.microsoft.com/en-us/uwp/api/windows.media.control.globalsystemmediatransportcontrolssession) - Located in `MediaManager.MediaSession.ControlSession`. The Media Session that allows for events whenever the playback state or the media property changes as well as to grab such info whenever desired.

  
## Samples
- Sample.CMD - A very barebone console application for developers to get a feel of how their use-case might act.

![Sample.CMD](https://raw.githubusercontent.com/DubyaDude/WindowsMediaController/master/docs/images/Sample.CMD.png)

- Sample.UI - A WPF media controller

![Sample.UI](https://raw.githubusercontent.com/DubyaDude/WindowsMediaController/master/docs/images/Sample.UI.png)


## Credit
- Luca Marini ([Stack Overflow](https://stackoverflow.com/users/13997827/luca-marini)) - Helped me understand the Windows API
- Google ([materialui](https://github.com/google/material-design-icons)) - Utilizing their play icon to create our icon
- Kinnara ([ModernWpf](https://github.com/Kinnara/ModernWpf)) - Utilized the ModernWpf library to create the UI sample
