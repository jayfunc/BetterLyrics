[_Click here to view frequently asked questions (FAQ)_](#faq)

![](Promotion/banner.png)

<div align=center>
  <img src="BetterLyrics.WinUI3/BetterLyrics.WinUI3/Assets/Logo.png" alt="" width="64">
</div>

<h2 align=center>
BetterLyrics
</h2>

<div align=center>

[![](https://img.shields.io/badge/zh--CN-%E7%AE%80%E4%BD%93%E4%B8%AD%E6%96%87-blue)](https://github.com/jayfunc/BetterLyrics/blob/dev/README.zh-CN.md) [![Static Badge](https://img.shields.io/badge/zh--TW-%E7%B9%81%E9%AB%94%E4%B8%AD%E6%96%87-blue)](https://github.com/jayfunc/BetterLyrics/blob/dev/README.zh-TW.md) [![Static Badge](https://img.shields.io/badge/ja-%E6%97%A5%E6%9C%AC%E8%AA%9E-blue)](https://github.com/jayfunc/BetterLyrics/blob/dev/README.ja.md) [![Static Badge](https://img.shields.io/badge/ko-%ED%95%9C%EA%B5%AD%EC%9D%B8-blue)](https://github.com/jayfunc/BetterLyrics/blob/dev/README.ko.md)

</div>

<div align=center>

![Static Badge](https://img.shields.io/badge/Language-C%23-purple) ![Static Badge](https://img.shields.io/badge/License-MIT-red) ![Static Badge](https://img.shields.io/badge/IDE-Visual%20Studio-purple) ![Static Badge](https://img.shields.io/badge/Framework-WinUI%203-blue)

</div>

<div align=center>

[![GitHub Repo stars](https://img.shields.io/github/stars/jayfunc/BetterLyrics)](https://github.com/jayfunc/BetterLyrics/stargazers)

</div>

<h4 align="center">
Your dynamic lyrics display tool built with WinUI 3 and Win2D — works with local playback and other players
</h3>

## 🎉 This project was featured by SSPAI!

Check out the article: [BetterLyrics – An immersive and smooth lyrics display tool designed for Windows](https://sspai.com/post/101028)

## 🔈 Feedback and chat group

- [<img src="BetterLyrics.WinUI3\BetterLyrics.WinUI3\Assets\QQ.png" height="20"> QQ](https://qun.qq.com/universal-share/share?ac=1&authKey=4Q%2BYTq3wZldYpF5SbS5c19ECFsiYoLZFAIcBNNzYpBUtiEjaZ8sZ%2F%2BnFN0qw3lad&busi_data=eyJncm91cENvZGUiOiIxMDU0NzAwMzg4IiwidG9rZW4iOiJiVnhqemVYN0N5QVc3b1ZkR24wWmZOTUtvUkJoWm1JRWlaWW5iZnlBcXJtZUtGc2FFTHNlUlFZMi9iRm03cWF5IiwidWluIjoiMTM5NTczOTY2MCJ9&data=39UmAihyH_o6CZaOs7nk2mO_lz2ruODoDou6pxxh7utcxP4WF5sbDBDOPvZ_Wqfzeey4441anegsLYQJxkrBAA&svctype=4&tempid=h5_group_info) (1054700388)
- [<img src="BetterLyrics.WinUI3\BetterLyrics.WinUI3\Assets\Discord.png" height="12"> Discord](https://discord.gg/5yAQPnyCKv)
- [<img src="BetterLyrics.WinUI3\BetterLyrics.WinUI3\Assets\Telegram.png" height="16"> Telegram](https://t.me/+svhSLZ7awPsxNGY1)

## 🌟 Highlighted features

- 🌠 **Pleasing User Interface**
  - Fluent animations and effects
- ↔️ **Strong Lyrics Translation**
  - Offline machine translation (supporting 30 languages)
  - Auto reading local lyrics files for embedded translation
- 🧩 **Various Lyrics Source**
  - Local storage
    - Music files (with embedded lyrics)
    - [.lrc](<https://en.wikipedia.org/wiki/LRC_(file_format)>) files (with both core format and enhanced format)
    - [.eslrc](https://github.com/ESLyric/release) files
    - [.ttml](https://en.wikipedia.org/wiki/Timed_Text_Markup_Language) files
  - Online lyrics providers
    - QQ Music
    - 网易云音乐 NetEase Cloud Music
    - 酷狗音乐 Kugou Music
    - [amll-ttml-db](https://github.com/Steve-xmh/amll-ttml-db)
    - [LRCLIB](https://lrclib.net/)
- 🎶 **Multiple Music Players Supported**

  - <details><summary>⚠️ NetEase Cloud Music</summary>

    - Please install the [BetterNCM plugin](https://microblock.cc/betterncm) first. If a downgrade guide pops up after the installation, please follow the guide to complete the downgrade of NetEase Cloud Music (downgrade to 2.10.13);
    - After that, please install the InfLink plugin in PluginMarket. After the installation is complete, please restart NetEase Cloud Music. At this point, all preparatory operations have been completed, enjoy it!
    - ⚠️ Please note that there is issues with timeline due to plugin issue

    </details>

  - <details><summary>⚠️ Kugou Music</summary>

    - Please make sure that the Kugou Music setting "Support system playback controls, such as lock screen interface" is turned on
    - No timeline information broadcasted, which means when you change timeline position in Kugou Music, BetterLyrics has no way to detect this change.
    - ⚠️ Please note that there is issues with timeline due to Kugou itself

    </details>

  - <details><summary>⚠️ Apple Music</summary>

    - Make sure you have set timeline threshold to around 600 ms in settings (Go to "Settings" - "Advanced option" to change), otherwise, the lyrics will be moving forward and afterward constantly.
    - ⚠️ Please note that you need additional settings to stop from seeing shaking lyrics (see FAQ at the end of this document for more information)

    </details>

  - <details><summary>⚠️ foobar2000</summary>

    - Make sure you have https://github.com/dumbie/foo_mediacontrol installed with it
    - ⚠️ Please note that there is issues with timeline due to plugin issue

    </details>

  - Spotify
  - QQ Music
  - PotPlayer
  - Media Player (System)

  - <details><summary>LX Music</summary>

    - Please make sure you have enabled "Open API" in LX Music settings page
    - Then open BetterLyrics, go to settings, go to "Advanced options", input your LX Music server address (mostly like http://127.0.0.1:23330) and there you go!

    </details>

  - <details><summary>MusicBee</summary>

    - Please install https://github.com/HenryPDT/mb_MediaControl before using

    </details>

  - <details><summary>iTunes</summary>

    - Please install https://github.com/thewizrd/iTunes-SMTC before using

    </details>

  - <details><summary>AIMP</summary>

    - Please install https://www.aimp.ru/?do=catalog&rec_id=1097 before using

    </details>

- 🪟 **Multiple Display Modes**
  - **Standard Mode**
    - Enjoy an immersive listening journey with rich lyrics animations and beautifully dynamic backgrounds
  - **Dock Mode**
    - A smart animated lyrics bar docked to your screen edge
  - **Desktop Mode**
    - Enjoy immersive lyrics floating above your apps
- 🧠 **Smart Behaviors**
  - Auto hide when music paused

> This project is still under development, bugs and unexpected behaviors may be existed in the latest branch.

## Screenshots

### Standard mode

![alt text](Screenshots/image.png)

![alt text](Screenshots/glow-float.gif)

![alt text](Screenshots/fan.png)

![alt text](Screenshots/lyrics-only.png)

![alt text](Screenshots/album-art-only.png)

### Dock mode

![alt text](Screenshots/dock-1.png)

![alt text](Screenshots/dock-2.png)

### Desktop mode

![alt text](Screenshots/desktop-1.png)

![alt text](Screenshots/desktop-2.png)

## Demonstration

Watch our introduction video (uploaded on 18 Aug 2025) on Bilibili [here](https://www.bilibili.com/video/BV1yLYtzQEME/).

## Try it now

### Microsoft Store

<a href="https://apps.microsoft.com/detail/9P1WCD1P597R?referrer=appbadge&mode=direct">
	<img src="https://get.microsoft.com/images/en-us%20dark.svg" width="200"/>
</a>

**Easiest** way to get it. **Unlimited** free trail or purchase (there is **no difference** between free and paid version)

☕ If you find it useful, please consider purchasing 🧧 it in **Microsoft Store**, I'll appreciate it! 🥰

> When there's a stable version built, Microsoft Store will be the first channel to get updated.

### Google Drive

Or get it from Google Drive (see [release](https://github.com/jayfunc/BetterLyrics/releases) page for the link)

> Please note you are downloading ".zip" file, for guide on how to install it, please kindly follow [this doc](How2Install/How2Install.md).

## 💖 Many thanks to

- [Lyricify-Lyrics-Helper](https://github.com/WXRIW/Lyricify-Lyrics-Helper)
  - Provide lyrics fetch, decryption, and parse for QQ, Netease, Kugou sources
- [lrclib](https://github.com/tranxuanthang/lrclib)
  - LRCLIB lyrics API provider
- [Audio Tools Library (ATL) for .NET](https://github.com/Zeugma440/atldotnet)
  - Used for extracting pictures in music files
- [WinUIEx](https://github.com/dotMorten/WinUIEx)
  - Provide easy ways to access Win32 API regarding windowing
- [TagLib#](https://github.com/mono/taglib-sharp)
  - Used for reading original lyrics content
- [Vanara](https://github.com/dahall/Vanara)
  - Win32 API wrapper
- [LibreTranslate](https://github.com/LibreTranslate/LibreTranslate)
  - Provide the ability for offline lyrics translation
- [Stackoverflow - How to animate Margin property in WPF](https://stackoverflow.com/a/21542882/11048731)
- [DevWinUI](https://github.com/ghost1372/DevWinUI)
- [Bilibili -【WinUI3】SystemBackdropController：定义云母、亚克力效果](https://www.bilibili.com/video/BV1PY4FevEkS)
- [cnblogs - .NET App 与 Windows 系统媒体控制(SMTC)交互](https://www.cnblogs.com/TwilightLemon/p/18279496)
- [Win2D 中的游戏循环：CanvasAnimatedControl](https://www.cnblogs.com/walterlv/p/10236395.html)
- [r2d2rigo/Win2D-Samples](https://github.com/r2d2rigo/Win2D-Samples/blob/master/IrisBlurWin2D/IrisBlurWin2D/MainPage.xaml.cs)
- [CommunityToolkit - 从入门到精通](https://mvvm.coldwind.top/)

## Inspired by

- [refined-now-playing-netease](https://github.com/solstice23/refined-now-playing-netease)
- [Lyricify-App](https://github.com/WXRIW/Lyricify-App)
- [椒盐音乐 Salt Player](https://moriafly.com/program/salt-player)
- [MyToolBar](https://github.com/TwilightLemon/MyToolBar)

## ✍️ Help us translate into your language

Cannot find your language?
Don't worry! Start translating and become one of the contributors! 😆
Click the [link](https://crowdin.com/project/betterlyrics/invite?h=d767e4f2dbd832d8fcdb6f7e5a198b402502866) to translate this app into your language via Crowdin now!

## Star history

[![](https://api.star-history.com/svg?repos=jayfunc/BetterLyrics&type=Date)](https://www.star-history.com/#jayfunc/BetterLyrics&Date)

## Any issues and PRs are welcomed

If you find a bug please file it in issues or if you have any ideas feel free to share it here.

---

## FAQ

### I couldn't see any button in dock mode

It is important to note that when you enter "Docked Mode", the action buttons are hidden. Hover your mouse over the top to access the "Immerse", "More", and "Close" buttons.

![alt text](FAQ/image-10.png)

Hover the mouse slightly above the bottom edge of the window to display the white control floating window at the bottom

![alt text](FAQ/image-11.png)

Tap the "little white bar" to display the bottom control bar in floating window form (including current playback progress view, timeline offset adjustment; previous song, pause/play, next song; translation, layout, settings)

![alt text](FAQ/image-12.png)

### How can I lock the window in desktop mode

![alt text](FAQ/image-6.png)

Hover you mouse on the top, click on the lock icon and you're good to go! Or, alternatively, press `Ctrl + Alt + U`.

### How can I unlock the window in desktop mode

![alt text](FAQ/image-7.png)

It's in the system tray, right-click on the icon and you'll see "Unlock the window". Or, alternatively, press `Ctrl + Alt + U`.

### There's a delay in lyrics timeline

Hover you mouse on the very bottom of the app,

![alt text](FAQ/image.png)

And then click on the first icon button (Lyrics timeline offset), here you can adjust the offset freely.

### The lyrics jump back and forth frequently (e.g. Apple Music)

![alt text](FAQ/image-2.png)

Go to "Advanced options" section, increase the threshold value (marked with the bigger red rectangle) until the lyrics is working properly.
