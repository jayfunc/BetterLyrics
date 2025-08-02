�?<div style="text-align: center;">

[�? 点击此�?�查看常见问�? (FAQ)](#faq)

</div>

<div align="center">
  <img src="BetterLyrics.WinUI3/BetterLyrics.WinUI3/Assets/Logo.png" alt="" width="64">
</div>

<h2 align="center">
BetterLyrics
</h2>

<div style="text-align: center;">

[![](https://img.shields.io/badge/zh--CN-%E7%AE%80%E4%BD%93%E4%B8%AD%E6%96%87-blue)](https://github.com/jayfunc/BetterLyrics/blob/dev/README.zh-CN.md) [![Static Badge](https://img.shields.io/badge/zh--TW-%E7%B9%81%E9%AB%94%E4%B8%AD%E6%96%87-blue)](https://github.com/jayfunc/BetterLyrics/blob/dev/README.zh-TW.md) [![Static Badge](https://img.shields.io/badge/ja-%E6%97%A5%E6%9C%AC%E8%AA%9E-blue)](https://github.com/jayfunc/BetterLyrics/blob/dev/README.ja.md) [![Static Badge](https://img.shields.io/badge/ko-%ED%95%9C%EA%B5%AD%EC%9D%B8-blue)](https://github.com/jayfunc/BetterLyrics/blob/dev/README.ko.md) [![GitHub Repo stars](https://img.shields.io/github/stars/jayfunc/BetterLyrics)](https://github.com/jayfunc/BetterLyrics/stargazers)

</div>

<div style="text-align: center;">

![Static Badge](https://img.shields.io/badge/Language-C%23-purple) ![Static Badge](https://img.shields.io/badge/License-MIT-red) ![Static Badge](https://img.shields.io/badge/IDE-Visual%20Studio-purple) ![Static Badge](https://img.shields.io/badge/Framework-WinUI%203-blue)

</div>

<h4 align="center">
你的动态歌词显示工具，基于 WinUI 3 �? Win2D 构建 —�? �?持本地播放及多�?�播放器
</h3>

## 🎉 �?项目�?少数派推荐！

查看文章：[BetterLyrics �? 一款为 Windows 设�?�的沉浸式流畅歌词显示工具](https://sspai.com/post/101028)

## 🔈 反�?�与交流�?

- [<img src="BetterLyrics.WinUI3\BetterLyrics.WinUI3\Assets\QQ.png" height="20"> QQ](https://qun.qq.com/universal-share/share?ac=1&authKey=4Q%2BYTq3wZldYpF5SbS5c19ECFsiYoLZFAIcBNNzYpBUtiEjaZ8sZ%2F%2BnFN0qw3lad&busi_data=eyJncm91cENvZGUiOiIxMDU0NzAwMzg4IiwidG9rZW4iOiJiVnhqemVYN0N5QVc3b1ZkR24wWmZOTUtvUkJoWm1JRWlaWW5iZnlBcXJtZUtGc2FFTHNlUlFZMi9iRm03cWF5IiwidWluIjoiMTM5NTczOTY2MCJ9&data=39UmAihyH_o6CZaOs7nk2mO_lz2ruODoDou6pxxh7utcxP4WF5sbDBDOPvZ_Wqfzeey4441anegsLYQJxkrBAA&svctype=4&tempid=h5_group_info) (1054700388)
- [<img src="BetterLyrics.WinUI3\BetterLyrics.WinUI3\Assets\Discord.png" height="12"> Discord](https://discord.gg/5yAQPnyCKv)
- [<img src="BetterLyrics.WinUI3\BetterLyrics.WinUI3\Assets\Telegram.png" height="16"> Telegram](https://t.me/+svhSLZ7awPsxNGY1)

## 🌟 主�?�特�?

- 🌠 **美�?�的用户界面**
  - 流畅的动画与特效
- ↔️ **强大的歌词翻�?**
  - 离线机器翻译（支�? 30 种�??言�?
  - �?动�?�取�?地歌词文件中的嵌入翻�?
- 🧩 **多样的歌词来�?**
  - �?地存�?
    - 音乐文件（含嵌入歌词�?
    - [.lrc](<https://en.wikipedia.org/wiki/LRC_(file_format)>) 文件（支持标准与增强格式�?
    - [.eslrc](https://github.com/ESLyric/release) 文件
    - [.ttml](https://en.wikipedia.org/wiki/Timed_Text_Markup_Language) 文件
  - 在线歌词�?
    - QQ 音乐
    - 网易云音�? NetEase Cloud Music
    - 酷狗音乐 Kugou Music
    - [amll-ttml-db](https://github.com/Steve-xmh/amll-ttml-db)
    - [LRCLIB](https://lrclib.net/)
- 🎶 **�?持�?��?�音乐播放器**

  - <details><summary>⚠️ 网易云音�?</summary>

    - 请先安�?? [BetterNCM 插件](https://microblock.cc/betterncm)。�?�安装后弹出降级指引，�?�按指引将网易云音乐降级�? 2.10.13�?
    - 然后在插件市场安�? InfLink 插件，安装完成后重启网易云音乐即�?�?
    - ⚠️ 由于插件�?题，时间轴可能存在异�?

    </details>

  - <details><summary>⚠️ 酷狗音乐</summary>

    - 请确保酷狗音乐�?�置�?“支持系统播放控制（如锁屏界�?）”已开�?
    - 酷狗音乐不会广播时间轴信�?，�?�致切换进度�? BetterLyrics 无法检�?
    - ⚠️ 时间轴问题为酷狗�?�?限制

    </details>

  - <details><summary>⚠️ Apple Music</summary>

    - 请在设置�?将时间轴阈值�?�为�? 600ms（“�?�置�?-“高级选项”），否则歌词会不断前后跳动
    - ⚠️ 需额�?��?�置以避免歌词抖�?（�?��?�文�? FAQ�?

    </details>

  - <details><summary>⚠️ foobar2000</summary>

    - 请安�? https://github.com/dumbie/foo_mediacontrol
    - ⚠️ 由于插件�?题，时间轴可能存在异�?

    </details>

  - Spotify
  - QQ 音乐
  - PotPlayer
  - 媒体�?放器（系统）

  - <details><summary>LX Music</summary>

    - 请确保已�? LX Music 设置页开�?“Open API�?
    - 然后�? BetterLyrics 设置-高级选项�?�?�? LX Music 服务器地址（通常�? http://127.0.0.1:23330）即�?

    </details>

  - <details><summary>MusicBee</summary>

    - 请安�? https://github.com/HenryPDT/mb_MediaControl

    </details>

  - <details><summary>iTunes</summary>

    - 请安�? https://github.com/thewizrd/iTunes-SMTC

    </details>

  - <details><summary>AIMP</summary>

    - 请安�? https://www.aimp.ru/?do=catalog&rec_id=1097

    </details>

- 🪟 **多�?�显示模�?**
  - **标准模式**
    - 沉浸式歌词动画与动态背�?
  - **停靠模式**
    - 智能歌词条停靠屏幕边�?
  - **桌面模式**
    - 歌词�?�?于�?�面应用之上
- �?? **智能行为**
  - 音乐暂停时自动隐�?

> �?项目仍在开发中，最新分�?�?能存�? bug 或异常�?�为�?

## �?�?

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

�? B 站�?�看我们的介绍�?��?�（2025 �? 7 �? 7 日上传）：[点�?��?�看](https://www.bilibili.com/video/BV1zjGjzfEXh)

## 立即体验

### Microsoft Store

<a href="https://apps.microsoft.com/detail/9P1WCD1P597R?referrer=appbadge&mode=direct">
	<img src="https://get.microsoft.com/images/en-us%20dark.svg" width="200"/>
</a>

**最简�?**的获取方式�?**无限�?**免费试用或购买（免费与付费无�?�?�?

�? 如果觉得好用，�?�迎�? **Microsoft Store** �?买支�? 🧧，感�?！�?

> 稳定版发布后，Microsoft Store 会�??一时间更新�?

### Google Drive

也可通过 Google Drive 获取（�?? [release](https://github.com/jayfunc/BetterLyrics/releases) 页面�?

> 下载的是 ".zip" 文件，安装方法�?�参�? [此文�?](How2Install/How2Install.md)�?

## 💖 特别感谢

- [Lyricify-Lyrics-Helper](https://github.com/WXRIW/Lyricify-Lyrics-Helper)
  - 提供 QQ、网易云、酷狗歌词获取、解密与解析
- [lrclib](https://github.com/tranxuanthang/lrclib)
  - LRCLIB 歌词 API 提供�?
- [Audio Tools Library (ATL) for .NET](https://github.com/Zeugma440/atldotnet)
  - 用于提取音乐文件�?的图�?
- [WinUIEx](https://github.com/dotMorten/WinUIEx)
  - 提供便捷�? Win32 窗口 API
- [TagLib#](https://github.com/mono/taglib-sharp)
  - 用于读取原�?�歌词内�?
- [Vanara](https://github.com/dahall/Vanara)
  - Win32 API 封�?�库
- [LibreTranslate](https://github.com/LibreTranslate/LibreTranslate)
  - 提供离线歌词翻译能力
- [Stackoverflow - 如何�? WPF �?动画�? Margin 属�?](https://stackoverflow.com/a/21542882/11048731)
- [DevWinUI](https://github.com/ghost1372/DevWinUI)
- [Bilibili -【WinUI3】SystemBackdropController：定义云母、亚克力效果](https://www.bilibili.com/video/BV1PY4FevEkS)
- [cnblogs - .NET App �? Windows 系统媒体控制(SMTC)交互](https://www.cnblogs.com/TwilightLemon/p/18279496)
- [Win2D �?的游戏循�?：CanvasAnimatedControl](https://www.cnblogs.com/walterlv/p/10236395.html)
- [r2d2rigo/Win2D-Samples](https://github.com/r2d2rigo/Win2D-Samples/blob/master/IrisBlurWin2D/IrisBlurWin2D/MainPage.xaml.cs)
- [CommunityToolkit - 从入门到精通](https://mvvm.coldwind.top/)

## 灵感来源

- [refined-now-playing-netease](https://github.com/solstice23/refined-now-playing-netease)
- [Lyricify-App](https://github.com/WXRIW/Lyricify-App)
- [椒盐音乐 Salt Player](https://moriafly.com/program/salt-player)
- [MyToolBar](https://github.com/TwilightLemon/MyToolBar)

## ✍️ 欢迎协助翻译

没有找到你的�?言�?
�?担心！快来参与翻译，成为贡献者之一吧！😆
点击[此链�?](https://crowdin.com/project/betterlyrics/invite?h=d767e4f2dbd832d8fcdb6f7e5a198b402502866)通过 Crowdin 翻译�?应用�?

## Star 记录

[![](https://api.star-history.com/svg?repos=jayfunc/BetterLyrics&type=Date)](https://www.star-history.com/#jayfunc/BetterLyrics&Date)

## 欢迎�? issue �? PR

如发�? bug 请在 issues 提出，或有想法�?�迎在�?�分�?�?

---

## FAQ

### 停靠模式下看不到按钮

进入“停靠模式”后，操作按�?会隐藏。将鼠标�?停在窗口顶部即可显示“沉浸”、“更多”、“关�?”按�?�?

![alt text](FAQ/image-10.png)

将鼠标悬停在窗口底部边缘稍上方，会显示底部白色悬�?控制窗口

![alt text](FAQ/image-11.png)

点击“小白条”可显示底部�?�?控制栏（�?当前�?放进度、时间轴偏移调整、上一�?/暂停/下一曲、翻译、布局、�?�置�?

![alt text](FAQ/image-12.png)

### 桌面模式如何锁定窗口

![alt text](FAQ/image-6.png)

将鼠标悬停在顶部，点击锁定图标即�?，或�? `Ctrl + Alt + U`�?

### 桌面模式如何解锁窗口

![alt text](FAQ/image-7.png)

在系统托盘右�?图标，选择“解锁窗口”，或按 `Ctrl + Alt + U`�?

### 歌词时间轴有延迟

将鼠标悬停在应用底部�?

![alt text](FAQ/image.png)

点击�?一�?图标按钮（歌词时间轴偏移），即可�?由调整偏移量�?

### 歌词频繁跳动（�?? Apple Music�?

![alt text](FAQ/image-2.png)

进入“高级选项”，增大阈值（红�?�标记�?�），直至歌词�?�常�?
