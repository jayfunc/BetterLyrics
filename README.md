# BetterLyrics

Local song lyrics presentation app built with WinUI3

## Highlighted features
- Dynamic blur album art as background
- Smooth lyrics fade in/out, zoom in/out effects
- Smooth user interface change from song to song
- Gradient Karaoke effect on every single character

> Note: Lyrics related effects and functions are built with [CanvasAnimatedControl](https://learn.microsoft.com/en-us/windows/apps/develop/win2d/quick-start#animate-your-app-with-canvasanimatedcontrol) instead of list of `TextBlock`, which ensures a smooth and accurate animation and more customized style.

Coding in progress...

## Customize in your way

We provide more than one setting item to better align with your preference

- Theme
	- Follow system
	- Light
	- Dark

- Backdrop
	- None
	- Mica
	- Mica alt
	- Acrylic desktop
	- Acrylic thin
	- Acrylic base
	- Transparent

- Album art as background
	- Dynamic
	- Opacity
	- Blur amount

- Lyrics
	- Alignment
	- Font size
	- Line spacing
	- Opacity on the edge
	- Blur amount
	- Glow effect

- Language
	- English
	- Simplified Chinese
	- Traditional Chinese

## Inspired by 
- [BetterNCM](https://github.com/std-microblock/BetterNCM)
- [Lyricify-App](https://github.com/WXRIW/Lyricify-App)
- [椒盐音乐 Salt Player](https://moriafly.com/program/salt-player)

## Demonstration video

See our latest introduction video「BetterLyrics 阶段性开发成果展示」on [Bilibili](https://b23.tv/QjKkYmL)

## Screenshots (outdated)

### Settings

![Settings](/Screenshots/settings.png)

### Light music mode

Will be activated automatically when lyrics are not detected/found

![Light music mode](/Screenshots/light-music.png)

### General music mode
![General music mode](/Screenshots/general-music.png)

### Real-time gif
![Real-time gif](/Screenshots/lyrics-animation.gif)

## Many thanks to 
- [Audio Tools Library (ATL) for .NET](https://github.com/Zeugma440/atldotnet)
- [DevWinUI](https://github.com/ghost1372/DevWinUI)
- [Stackoverflow - How to animate Margin property in WPF](https://stackoverflow.com/a/21542882/11048731)
- [TagLib#](https://github.com/mono/taglib-sharp)
- [Bilibili -【WinUI3】SystemBackdropController：定义云母、亚克力效果](https://www.bilibili.com/video/BV1PY4FevEkS)
- [cnblogs - .NET App 与Windows系统媒体控制(SMTC)交互](https://www.cnblogs.com/TwilightLemon/p/18279496)
- [Win2D 中的游戏循环：CanvasAnimatedControl](https://www.cnblogs.com/walterlv/p/10236395.html)
- [r2d2rigo/Win2D-Samples](https://github.com/r2d2rigo/Win2D-Samples/blob/master/IrisBlurWin2D/IrisBlurWin2D/MainPage.xaml.cs)

## Third-party libraries that this app uses
- CommunityToolkit.Labs.WinUI.MarqueeText
- CommunityToolkit.Labs.WinUI.OpacityMaskView
- CommunityToolkit.Mvvm
- CommunityToolkit.WinUI.Controls.Primitives
- CommunityToolkit.WinUI.Extensions
- DevWinUI
- DevWinUI.Controls
- Microsoft.Extensions.DependencyInjection
- Microsoft.Graphics.Win2D
- Microsoft.Windows.SDK.BuildTools
- Microsoft.WindowsAppSDK
- Microsoft.Xaml.Behaviors.WinUI.Managed
- z440.atl.core
