<div align="center">
  <img src="BetterLyrics.WinUI3/BetterLyrics.WinUI3/Assets/Logo.png" alt="" width="64"/>
</div>

<h2 align="center">
BetterLyrics
</div>

<h3 align="center">
Your smooth dynamic local lyrics display built with WinUI 3
</div>

---

## Highlighted features

- Dynamic blur album art as background
- Smooth lyrics fade in/out, zoom in/out effects
- Smooth user interface change from song to song
- **Gradient** Karaoke effect on every single character

Coding in progress...

## Customize in your way

We provide more than one setting item to better align with your preference

- Theme (light, dark, follow system)

- Backdrop (none, mica, acrylic, transparent)

- Album art as background (dynamic, blur amount, opacity)

- Lyrics (alignment, font size, font color **(picked from album art accent color)** line spacing, opacity, blur amount, dynamic **glow** effect)

- Language (English, Simplified Chinese, Traditional Chinese)

## Live demonstration

![alt text](Screenshots/Beelink-SER-8-Moonlight2025-06-0318-35-35-ezgif.com-video-to-gif-converter.gif)

![alt text](Screenshots/BetterLyrics.WinUI3_igDdnc4rzW.gif)

Or watch our introduction video「BetterLyrics 阶段性开发成果展示」(uploaded on 31 May 2025) on Bilibili below (click the cover image to watch):

[![Bilibili](https://i1.hdslb.com/bfs/archive/75a7a1a3803b617574090a91c59785b6c40d0fe5.jpg@672w_378h_1c.avif)](https://b23.tv/QjKkYmL)

## Screenshots

### Split view

Non-immersive mode

![alt text](Screenshots/Snipaste_2025-06-07_17-36-26.png)

Immersive mode
![alt text](Screenshots/Snipaste_2025-06-03_16-47-43.png)

### Lyrics only

![alt text](Screenshots/Snipaste_2025-06-03_17-51-22.png)

### Fullscreen

![alt text](Screenshots/Snipaste_2025-06-03_18-36-05.png)

### Settings

![alt text](Screenshots/Snipaste_2025-06-07_17-32-02.png)
![alt text](Screenshots/Snipaste_2025-06-07_17-32-17.png)
![alt text](Screenshots/Snipaste_2025-06-07_17-32-23.png)

## Download it now

<a href="https://apps.microsoft.com/detail/9P1WCD1P597R?referrer=appbadge&mode=direct">
	<img src="https://get.microsoft.com/images/en-us%20dark.svg" width="200"/>
</a>

> **Easiest** way to get it. **Unlimited** free trail or purchase (there is **no difference** between free and paid version, if you like you can purchase to support me)

Or alternatively get it from Google Drive (see [release](https://github.com/jayfunc/BetterLyrics/releases/latest) page for the link)

> Please note you are downloading ".zip" file, for guide on how to install it, please kindly follow [this doc](How2Install/How2Install.md).

## Setup your app

This project relies on listening messages from [SMTC](https://learn.microsoft.com/en-ca/windows/uwp/audio-video-camera/integrate-with-systemmediatransportcontrols).
So technically, as long as you are using the music apps (like

- Spotify
- Groove Music
- Apple Music
- Windows Media Player
- VLC Media Player
- QQ 音乐
- 网易云音乐
- 酷狗音乐
- 酷我音乐

) which support SMTC, then possibly (I didn't test all of themif you find one fail to listen to, you can open an issue) all you need to do is just load your local music/lyrics lib and you are good to go.

## Future work

- Watching file changes
  When you downloading lyrics (using some other tools or your own scripts) while listening to new musics (non-existed on your local disks), this app can automatically load those new files.

> Please note: we are not planning support directly load lyrics files via some music software APIs due to copyright issues.

## Many thanks to

- [Audio Tools Library (ATL) for .NET](https://github.com/Zeugma440/atldotnet)
- [DevWinUI](https://github.com/ghost1372/DevWinUI)
- [Stackoverflow - How to animate Margin property in WPF](https://stackoverflow.com/a/21542882/11048731)
- [TagLib#](https://github.com/mono/taglib-sharp)
- [Bilibili -【WinUI3】SystemBackdropController：定义云母、亚克力效果](https://www.bilibili.com/video/BV1PY4FevEkS)
- [cnblogs - .NET App 与 Windows 系统媒体控制(SMTC)交互](https://www.cnblogs.com/TwilightLemon/p/18279496)
- [Win2D 中的游戏循环：CanvasAnimatedControl](https://www.cnblogs.com/walterlv/p/10236395.html)
- [r2d2rigo/Win2D-Samples](https://github.com/r2d2rigo/Win2D-Samples/blob/master/IrisBlurWin2D/IrisBlurWin2D/MainPage.xaml.cs)

## Inspired by

- [BetterNCM](https://github.com/std-microblock/BetterNCM)
- [Lyricify-App](https://github.com/WXRIW/Lyricify-App)
- [椒盐音乐 Salt Player](https://moriafly.com/program/salt-player)

## Third-party libraries that this project uses

```
<PackageReference Include="CommunityToolkit.WinUI.Converters" Version="8.2.250402" />
<PackageReference Include="CommunityToolkit.WinUI.Extensions" Version="8.2.250402" />
<PackageReference Include="CommunityToolkit.WinUI.Helpers" Version="8.2.250402" />
<PackageReference Include="CommunityToolkit.WinUI.Media" Version="8.2.250402" />
<PackageReference Include="DevWinUI" Version="8.3.0" />
<PackageReference Include="DevWinUI.Controls" Version="8.3.0" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="9.0.5" />
<PackageReference Include="Microsoft.Extensions.Logging" Version="9.0.5" />
<PackageReference Include="Microsoft.Graphics.Win2D" Version="1.3.2" />
<PackageReference Include="Microsoft.Windows.SDK.BuildTools" Version="10.0.26100.4188" />
<PackageReference Include="Microsoft.WindowsAppSDK" Version="1.7.250513003" />
<PackageReference Include="Microsoft.Xaml.Behaviors.WinUI.Managed" Version="3.0.0" />
<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
<PackageReference Include="Serilog.Extensions.Logging" Version="9.0.2" />
<PackageReference Include="Serilog.Sinks.File" Version="7.0.0" />
<PackageReference Include="sqlite-net-pcl" Version="1.9.172" />
<PackageReference Include="System.Text.Encoding.CodePages" Version="9.0.5" />
<PackageReference Include="Ude.NetStandard" Version="1.2.0" />
<PackageReference Include="z440.atl.core" Version="6.24.0" />
```

## Any issues and PRs are welcomed
