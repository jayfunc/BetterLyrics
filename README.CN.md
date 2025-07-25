> 注：以下内容含有大语言模型翻译内容

<a href="https://github.com/jayfunc/BetterLyrics/blob/dev/README.md">_**🌐 Click here to see the English version**_</a>

<a href="https://github.com/jayfunc/BetterLyrics/blob/dev/FAQ/FAQ.md">_**❓ 点击查看常见问题（FAQ）**_</a>

<div align="center">
  <img src="BetterLyrics.WinUI3/BetterLyrics.WinUI3/Assets/Logo.png" alt="" width="64"/>
</div>

<h2 align="center">
BetterLyrics
</h2>
<h4 align="center">
基于 WinUI 3 构建的流畅动态歌词显示工具
</h3>

## 🎉 本项目已获得少数派推荐！

查看文章：[BetterLyrics – 专为 Windows 设计的沉浸式流畅歌词显示工具](https://sspai.com/post/101028)

## 🔈 反馈交流群

- [QQ](https://qun.qq.com/universal-share/share?ac=1&authKey=4Q%2BYTq3wZldYpF5SbS5c19ECFsiYoLZFAIcBNNzYpBUtiEjaZ8sZ%2F%2BnFN0qw3lad&busi_data=eyJncm91cENvZGUiOiIxMDU0NzAwMzg4IiwidG9rZW4iOiJiVnhqemVYN0N5QVc3b1ZkR24wWmZOTUtvUkJoWm1JRWlaWW5iZnlBcXJtZUtGc2FFTHNlUlFZMi9iRm03cWF5IiwidWluIjoiMTM5NTczOTY2MCJ9&data=39UmAihyH_o6CZaOs7nk2mO_lz2ruODoDou6pxxh7utcxP4WF5sbDBDOPvZ_Wqfzeey4441anegsLYQJxkrBAA&svctype=4&tempid=h5_group_info) (1054700388) | [Discord](https://discord.gg/5yAQPnyCKv) | [Telegram](https://t.me/+svhSLZ7awPsxNGY1)

## 🌟 核心亮点功能

- 🌠 美观的用户界面
  - 流畅的动画与视觉特效，打造赏心悦目的歌词体验
- ↔️ 强大的歌词翻译功能
  - 支持离线机器翻译，涵盖 30 种语言
  - 自动读取本地歌词文件中的嵌入翻译
- 🧩 多种歌词来源
  - 本地存储
    - 音乐文件（支持嵌入歌词）
    - [.lrc](<https://en.wikipedia.org/wiki/LRC_(file_format)>) 歌词文件（兼容标准格式和增强格式）
    - [.eslrc](https://github.com/ESLyric/release) 格式
    - [.ttml](https://en.wikipedia.org/wiki/Timed_Text_Markup_Language) 格式
  - 在线歌词服务
    - QQ 音乐
    - 网易云音乐
    - 酷狗音乐
    - [amll-ttml-db](https://github.com/Steve-xmh/amll-ttml-db)
    - [LRCLIB](https://lrclib.net/)
- 🪟 多种歌词显示模式
  - 标准模式
    - 沉浸式歌词体验，搭配丰富动画与动态背景，听歌更享受
  - 停靠模式
    - 智能歌词栏固定在屏幕边缘，简洁美观、互不打扰
  - 桌面模式
    - 歌词悬浮于其他窗口之上，边听歌边工作两不误
- 🧠 智能行为支持
  - 音乐播放器关闭后自动隐藏歌词窗口，干净整洁不打扰

> 本项目仍在开发中，最新版本可能存在 bug 和意外行为。

## 应用截图

### 标准模式

![alt text](Screenshots/image.png)

![alt text](Screenshots/glow-float.gif)

![alt text](Screenshots/fan.png)

![alt text](Screenshots/lyrics-only.png)

![alt text](Screenshots/album-art-only.png)

### 悬浮模式

![alt text](Screenshots/dock-1.png)

![alt text](Screenshots/dock-2.png)

### 桌面模式

![alt text](Screenshots/desktop-1.png)

![alt text](Screenshots/desktop-2.png)

## 演示视频

观看我们的介绍视频（2025 年 7 月 7 日上传）：[B 站链接](https://www.bilibili.com/video/BV1zjGjzfEXh)

## 已测试的音乐播放器

- 网易云音乐
  - 请先安装 [BetterNCM 插件](https://microblock.cc/betterncm) 安装完成后如若弹出降级指引，请根据指引完成网易云音乐的降级操作（降级至 2.10.13）；
  - 之后请在 PluginMarket 内安装 InfLink 插件，安装完成后请重启网易云音乐。至此，所有预备操作均已完成，尽情享用吧！
- 酷狗音乐
  - 请确保酷狗音乐设置项 “支持系统播放控件，如锁屏界面” 已开启
  - 不会广播时间线信息，这意味着当您在酷狗音乐中更改播放进度时，BetterLyrics 无法检测到此更改。
- Apple Music
  - 确保您在设置中将时间线阈值设置为约 600 毫秒（进入"设置"-"高级选项"进行更改），否则歌词会不断前后跳动。
- foobar2000
  - 确保您安装了 https://github.com/dumbie/foo_mediacontrol 插件
- Spotify
- QQ 音乐
- PotPlayer
- 媒体播放器（系统自带）
- LX 音乐
  - 请确保您已在 LX 音乐设置页面启用“开放 API”
  - 然后打开 BetterLyrics，进入设置，点击“高级选项”，输入您的 LX 音乐服务器地址（例如 http://127.0.0.1:23330）即可
- MusicBee
  - 请先安装 https://github.com/HenryPDT/mb_MediaControl
- iTunes
  - 请先安装 https://github.com/thewizrd/iTunes-SMTC
- AIMP
  - 请先安装 https://www.aimp.ru/?do=catalog&rec_id=1097

## 立即下载体验

### Microsoft Store

<a href="https://apps.microsoft.com/detail/9P1WCD1P597R?referrer=appbadge&mode=direct">
	<img src="https://get.microsoft.com/images/en-us%20dark.svg" width="200"/>
</a>

**最简单**的获取方式，**无限制**免费试用或购买（免费版与付费版**功能完全相同**）

☕ 如果您觉得本软件好用，请考虑在 **Microsoft Store** 中购买支持开发者 🧧，非常感谢您的支持！🥰

> 稳定版本发布时，Microsoft Store 将永远是第一个收到更新的渠道。

### Google Drive

或从 Google Drive 获取（请查看[发布页面](https://github.com/jayfunc/BetterLyrics/releases)获取链接）

> 请注意您下载的是".zip"文件，安装指南请参考[此文档](How2Install/How2Install.md)。

## 特别感谢

- [Lyricify-Lyrics-Helper](https://github.com/WXRIW/Lyricify-Lyrics-Helper)
  - 提供 QQ、网易、酷狗音源的歌词获取、解密和解析
- [LRCLIB](https://lrclib.net/)
  - LRCLIB 歌词 API 提供商
- [Audio Tools Library (ATL) for .NET](https://github.com/Zeugma440/atldotnet)
  - 用于提取音乐文件中的图片
- [WinUIEx](https://github.com/dotMorten/WinUIEx)
  - 提供便捷的 Win32 API 窗口操作方式
- [TagLib#](https://github.com/mono/taglib-sharp)
  - 用于读取原始歌词内容
- [Vanara](https://github.com/dahall/Vanara)
  - Win32 API 包装器
- [Stackoverflow - How to animate Margin property in WPF](https://stackoverflow.com/a/21542882/11048731)
- [DevWinUI](https://github.com/ghost1372/DevWinUI)
- [Bilibili -【WinUI3】SystemBackdropController：定义云母、亚克力效果](https://www.bilibili.com/video/BV1PY4FevEkS)
- [cnblogs - .NET App 与 Windows 系统媒体控制(SMTC)交互](https://www.cnblogs.com/TwilightLemon/p/18279496)
- [Win2D 中的游戏循环：CanvasAnimatedControl](https://www.cnblogs.com/walterlv/p/10236395.html)
- [r2d2rigo/Win2D-Samples](https://github.com/r2d2rigo/Win2D-Samples/blob/master/IrisBlurWin2D/IrisBlurWin2D/MainPage.xaml.cs)
- [CommunityToolkit - 从入门到精通](https://mvvm.coldwind.top/)

## 设计灵感来源

- [refined-now-playing-netease](https://github.com/solstice23/refined-now-playing-netease)
- [Lyricify-App](https://github.com/WXRIW/Lyricify-App)
- [椒盐音乐 Salt Player](https://moriafly.com/program/salt-player)
- [MyToolBar](https://github.com/TwilightLemon/MyToolBar)

## ✍️ 帮助我们翻译成您的语言

找不到您的语言？
别担心！立即开始翻译，成为贡献者！😆
点击[链接](https://crowdin.com/project/betterlyrics/invite?h=d767e4f2dbd832d8fcdb6f7e5a198b402502866)，立即通过 Crowdin 将这款应用翻译成您的语言！

## Star 历史

[![Star History Chart](https://api.star-history.com/svg?repos=jayfunc/BetterLyrics&type=Date)](https://www.star-history.com/#jayfunc/BetterLyrics&Date)

## 欢迎提交问题和拉取请求

如果您发现 bug，请在 issues 中提交；如果您有任何想法，也欢迎在这里分享。
