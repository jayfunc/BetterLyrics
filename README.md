# BetterLyrics

Local song lyrics presentation app built with WinUI3

## Features
- (Dynamic) themed colors from cover image
	- Due to the unpleasant experience I have commented dynamic codes but you can try to uncomment them to see the effect. At the moment, I haven't come up with any better ideas.
- Smooth lyrics fade in/out, zoom in/out effects

> Note: Lyrics related effects and functions are built with [CanvasAnimatedControl](https://learn.microsoft.com/en-us/windows/apps/develop/win2d/quick-start#animate-your-app-with-canvasanimatedcontrol) instead of list of `TextBlock`, which ensures a smooth and accurate animation and more customized style.

Coding in progress...

## Inspired by 
- [BetterNCM](https://github.com/std-microblock/BetterNCM)
- [Lyricify-App](https://github.com/WXRIW/Lyricify-App)
- [椒盐音乐 Salt Player](https://moriafly.com/program/salt-player)

## Screenshots
### Light music mode

Will be activated automatically when lyrics are not detected/found

![Light music mode](/Screenshots/image.png)

### General music mode
![General music mode](/Screenshots/image-1.png)

### Real-time gif
![Real-time gif](/Screenshots/1000003299.gif)

## Many thanks to 
- [Audio Tools Library (ATL) for .NET](https://github.com/Zeugma440/atldotnet)
- [DevWinUI](https://github.com/ghost1372/DevWinUI)
- [Stackoverflow - How to animate Margin property in WPF](https://stackoverflow.com/a/21542882/11048731)
- [TagLib#](https://github.com/mono/taglib-sharp)
- [Bilibili -【WinUI3】SystemBackdropController：定义云母、亚克力效果](https://www.bilibili.com/video/BV1PY4FevEkS)
- [cnblogs - .NET App 与Windows系统媒体控制(SMTC)交互](https://www.cnblogs.com/TwilightLemon/p/18279496)
- [Win2D 中的游戏循环：CanvasAnimatedControl](https://www.cnblogs.com/walterlv/p/10236395.html)

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
