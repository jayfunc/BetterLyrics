[_点击此处查看常见问题_](#faq)

![](Promotion/banner.png)

<div align=center>

</div>

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
你的动态歌词显示工具，基于 WinUI 3 和 Win2D 构建 —— 支持本地播放及多种播放器
</h3>

## 🎉 本项目被少数派推荐！

查看文章：[BetterLyrics – 一款为 Windows 设计的沉浸式流畅歌词显示工具](https://sspai.com/post/101028)

## 🔈 反馈与交流群

- [<img src="BetterLyrics.WinUI3\BetterLyrics.WinUI3\Assets\QQ.png" height="20"> QQ](https://qun.qq.com/universal-share/share?ac=1&authKey=4Q%2BYTq3wZldYpF5SbS5c19ECFsiYoLZFAIcBNNzYpBUtiEjaZ8sZ%2F%2BnFN0qw3lad&busi_data=eyJncm91cENvZGUiOiIxMDU0NzAwMzg4IiwidG9rZW4iOiJiVnhqemVYN0N5QVc3b1ZkR24wWmZOTUtvUkJoWm1JRWlaWW5iZnlBcXJtZUtGc2FFTHNlUlFZMi9iRm03cWF5IiwidWluIjoiMTM5NTczOTY2MCJ9&data=39UmAihyH_o6CZaOs7nk2mO_lz2ruODoDou6pxxh7utcxP4WF5sbDBDOPvZ_Wqfzeey4441anegsLYQJxkrBAA&svctype=4&tempid=h5_group_info) (1054700388)
- [<img src="BetterLyrics.WinUI3\BetterLyrics.WinUI3\Assets\Discord.png" height="12"> Discord](https://discord.gg/5yAQPnyCKv)
- [<img src="BetterLyrics.WinUI3\BetterLyrics.WinUI3\Assets\Telegram.png" height="16"> Telegram](https://t.me/+svhSLZ7awPsxNGY1)

## 🌟 主要特性

- 🌠 **美观的用户界面**
  - 流畅的动画与特效
- ↔️ **强大的歌词翻译**
  - 离线机器翻译（支持 30 种语言）
  - 自动读取本地歌词文件中的嵌入翻译
- 🧩 **多样的歌词来源**
  - 本地存储
    - 音乐文件（含嵌入歌词）
    - [.lrc](<https://en.wikipedia.org/wiki/LRC_(file_format)>) 文件（支持标准与增强格式）
    - [.eslrc](https://github.com/ESLyric/release) 文件
    - [.ttml](https://en.wikipedia.org/wiki/Timed_Text_Markup_Language) 文件
  - 在线歌词源
    - QQ 音乐
    - 网易云音乐 NetEase Cloud Music
    - 酷狗音乐 Kugou Music
    - [amll-ttml-db](https://github.com/Steve-xmh/amll-ttml-db)
    - [LRCLIB](https://lrclib.net/)
- 🎶 **支持多种音乐播放器**

  - <details><summary>⚠️ 网易云音乐</summary>

    - 请先安装 [BetterNCM 插件](https://microblock.cc/betterncm)。如安装后弹出降级指引，请按指引将网易云音乐降级至 2.10.13；
    - 然后在插件市场安装 InfLink 插件，安装完成后重启网易云音乐即可。
    - ⚠️ 由于插件问题，时间轴可能存在异常

    </details>

  - <details><summary>⚠️ 酷狗音乐</summary>

    - 请确保酷狗音乐设置中“支持系统播放控制（如锁屏界面）”已开启
    - 酷狗音乐不会广播时间轴信息，导致切换进度时 BetterLyrics 无法检测
    - ⚠️ 时间轴问题为酷狗本身限制

    </details>

  - <details><summary>⚠️ Apple Music</summary>

    - 请在设置中将时间轴阈值设为约 600ms（“设置”-“高级选项”），否则歌词会不断前后跳动
    - ⚠️ 需额外设置以避免歌词抖动（详见文末 FAQ）

    </details>

  - <details><summary>⚠️ foobar2000</summary>

    - 请安装 https://github.com/dumbie/foo_mediacontrol
    - ⚠️ 由于插件问题，时间轴可能存在异常

    </details>

  - Spotify
  - QQ 音乐
  - PotPlayer
  - 媒体播放器（系统）

  - <details><summary>LX Music</summary>

    - 请确保已在 LX Music 设置页开启“Open API”
    - 然后在 BetterLyrics 设置-高级选项中填写 LX Music 服务器地址（通常为 http://127.0.0.1:23330）即可

    </details>

  - <details><summary>MusicBee</summary>

    - 请安装 https://github.com/HenryPDT/mb_MediaControl

    </details>

  - <details><summary>iTunes</summary>

    - 请安装 https://github.com/thewizrd/iTunes-SMTC

    </details>

  - <details><summary>AIMP</summary>

    - 请安装 https://www.aimp.ru/?do=catalog&rec_id=1097

    </details>

- 🪟 **多种显示模式**
  - **标准模式**
    - 沉浸式歌词动画与动态背景
  - **停靠模式**
    - 智能歌词条停靠屏幕边缘
  - **桌面模式**
    - 歌词悬浮于桌面应用之上
- 🧠 **智能行为**
  - 音乐暂停时自动隐藏

> 本项目仍在开发中，最新分支可能存在 bug 或异常行为。

## 截图

### 标准模式

![alt text](Screenshots/image.png)

![alt text](Screenshots/glow-float.gif)

![alt text](Screenshots/fan.png)

![alt text](Screenshots/lyrics-only.png)

![alt text](Screenshots/album-art-only.png)

### 停靠模式

![alt text](Screenshots/dock-1.png)

![alt text](Screenshots/dock-2.png)

### 桌面模式

![alt text](Screenshots/desktop-1.png)

![alt text](Screenshots/desktop-2.png)

## 演示

在 B 站观看我们的介绍视频（2025 年 8 月 18 日上传）：[点此观看](https://www.bilibili.com/video/BV1yLYtzQEME/)

## 立即体验

### Microsoft Store

<a href="https://apps.microsoft.com/detail/9P1WCD1P597R?referrer=appbadge&mode=direct">
	<img src="https://get.microsoft.com/images/en-us%20dark.svg" width="200"/>
</a>

**最简单**的获取方式。**无限制**免费试用或购买（免费与付费无差别）

☕ 如果觉得好用，欢迎在 **Microsoft Store** 购买支持 🧧，感谢！🥰

> 稳定版发布后，Microsoft Store 会第一时间更新。

### Google Drive

也可通过 Google Drive 获取（见 [release](https://github.com/jayfunc/BetterLyrics/releases) 页面）

> 下载的是 ".zip" 文件，安装方法请参考 [此文档](How2Install/How2Install.md)。

## 💖 特别感谢

- [Lyricify-Lyrics-Helper](https://github.com/WXRIW/Lyricify-Lyrics-Helper)
  - 提供 QQ、网易云、酷狗歌词获取、解密与解析
- [lrclib](https://github.com/tranxuanthang/lrclib)
  - LRCLIB 歌词 API 提供方
- [Audio Tools Library (ATL) for .NET](https://github.com/Zeugma440/atldotnet)
  - 用于提取音乐文件中的图片
- [WinUIEx](https://github.com/dotMorten/WinUIEx)
  - 提供便捷的 Win32 窗口 API
- [TagLib#](https://github.com/mono/taglib-sharp)
  - 用于读取原始歌词内容
- [Vanara](https://github.com/dahall/Vanara)
  - Win32 API 封装库
- [LibreTranslate](https://github.com/LibreTranslate/LibreTranslate)
  - 提供离线歌词翻译能力
- [Stackoverflow - 如何在 WPF 中动画化 Margin 属性](https://stackoverflow.com/a/21542882/11048731)
- [DevWinUI](https://github.com/ghost1372/DevWinUI)
- [Bilibili -【WinUI3】SystemBackdropController：定义云母、亚克力效果](https://www.bilibili.com/video/BV1PY4FevEkS)
- [cnblogs - .NET App 与 Windows 系统媒体控制(SMTC)交互](https://www.cnblogs.com/TwilightLemon/p/18279496)
- [Win2D 中的游戏循环：CanvasAnimatedControl](https://www.cnblogs.com/walterlv/p/10236395.html)
- [r2d2rigo/Win2D-Samples](https://github.com/r2d2rigo/Win2D-Samples/blob/master/IrisBlurWin2D/IrisBlurWin2D/MainPage.xaml.cs)
- [CommunityToolkit - 从入门到精通](https://mvvm.coldwind.top/)

## 灵感来源

- [refined-now-playing-netease](https://github.com/solstice23/refined-now-playing-netease)
- [Lyricify-App](https://github.com/WXRIW/Lyricify-App)
- [椒盐音乐 Salt Player](https://moriafly.com/program/salt-player)
- [MyToolBar](https://github.com/TwilightLemon/MyToolBar)

## ✍️ 欢迎协助翻译

没有找到你的语言？
别担心！快来参与翻译，成为贡献者之一吧！😆
点击[此链接](https://crowdin.com/project/betterlyrics/invite?h=d767e4f2dbd832d8fcdb6f7e5a198b402502866)通过 Crowdin 翻译本应用！

## Star 记录

[![](https://api.star-history.com/svg?repos=jayfunc/BetterLyrics&type=Date)](https://www.star-history.com/#jayfunc/BetterLyrics&Date)

## 欢迎提 issue 和 PR

如发现 bug 请在 issues 提出，或有想法欢迎在此分享。

---

## FAQ

### 停靠模式下看不到按钮

进入“停靠模式”后，操作按钮会隐藏。将鼠标悬停在窗口顶部即可显示“沉浸”、“更多”、“关闭”按钮。

![alt text](FAQ/image-10.png)

将鼠标悬停在窗口底部边缘稍上方，会显示底部白色悬浮控制窗口

![alt text](FAQ/image-11.png)

点击“小白条”可显示底部悬浮控制栏（含当前播放进度、时间轴偏移调整、上一曲/暂停/下一曲、翻译、布局、设置）

![alt text](FAQ/image-12.png)

### 桌面模式如何锁定窗口

![alt text](FAQ/image-6.png)

将鼠标悬停在顶部，点击锁定图标即可，或按 `Ctrl + Alt + U`。

### 桌面模式如何解锁窗口

![alt text](FAQ/image-7.png)

在系统托盘右键图标，选择“解锁窗口”，或按 `Ctrl + Alt + U`。

### 歌词时间轴有延迟

将鼠标悬停在应用底部，

![alt text](FAQ/image.png)

点击第一个图标按钮（歌词时间轴偏移），即可自由调整偏移量。

### 歌词频繁跳动（如 Apple Music）

![alt text](FAQ/image-2.png)

进入“高级选项”，增大阈值（红框标记处），直至歌词正常。
