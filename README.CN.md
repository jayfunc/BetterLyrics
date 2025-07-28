> 注：以下内容含有大语言模型翻译内容

<a href="https://github.com/jayfunc/BetterLyrics/blob/dev/README.md">_**🌐 Click here to see the English version**_</a>

<a href="https://github.com/jayfunc/BetterLyrics/blob/dev/FAQ/FAQ.md">_**❓ 点此查看常见问题（FAQ）**_</a>

<div align="center">
  <img src="BetterLyrics.WinUI3/BetterLyrics.WinUI3/Assets/Logo.png" alt="" width="96">
</div>

<h2 align="center">
BetterLyrics
</h2>
<h4 align="center">
一个基于 WinUI 3 和 Win2D 构建的动态歌词显示工具 —— 支持本地播放，并可监听多种播放器
</h4>

## 🎉 本项目已被少数派推荐！

查看文章：[BetterLyrics - 一款专为 Windows 打造的沉浸式流畅歌词显示软件](https://sspai.com/post/101028)

## 🔈 反馈与交流群

- [<img src="BetterLyrics.WinUI3\BetterLyrics.WinUI3\Assets\QQ.png" height="20"> QQ 群](https://qun.qq.com/universal-share/share?ac=1&authKey=4Q%2BYTq3wZldYpF5SbS5c19ECFsiYoLZFAIcBNNzYpBUtiEjaZ8sZ%2F%2BnFN0qw3lad&busi_data=eyJncm91cENvZGUiOiIxMDU0NzAwMzg4IiwidG9rZW4iOiJiVnhqemVYN0N5QVc3b1ZkR24wWmZOTUtvUkJoWm1JRWlaWW5iZnlBcXJtZUtGc2FFTHNlUlFZMi9iRm03cWF5IiwidWluIjoiMTM5NTczOTY2MCJ9&data=39UmAihyH_o6CZaOs7nk2mO_lz2ruODoDou6pxxh7utcxP4WF5sbDBDOPvZ_Wqfzeey4441anegsLYQJxkrBAA&svctype=4&tempid=h5_group_info)（群号：1054700388）
- [<img src="BetterLyrics.WinUI3\BetterLyrics.WinUI3\Assets\Discord.png" height="12"> Discord](https://discord.gg/5yAQPnyCKv)
- [<img src="BetterLyrics.WinUI3\BetterLyrics.WinUI3\Assets\Telegram.png" height="16"> Telegram](https://t.me/+svhSLZ7awPsxNGY1)

## 🌟 核心特色

- 🌠 **优雅的用户界面**
  - 丰富流畅的动画与特效
- ↔️ **强大的歌词翻译功能**
  - 支持离线翻译（支持 30 种语言）
  - 自动读取本地歌词文件中的双语翻译内容
- 🧩 **多种歌词来源支持**
  - 本地存储
    - 音乐文件（嵌入式歌词）
    - [.lrc](https://zh.wikipedia.org/wiki/LRC) 文件（支持标准与增强格式）
    - [.eslrc](https://github.com/ESLyric/release) 文件
    - [.ttml](https://zh.wikipedia.org/wiki/Timed_Text_Markup_Language) 文件
  - 在线歌词源
    - QQ 音乐
    - 网易云音乐
    - 酷狗音乐
    - [amll-ttml-db](https://github.com/Steve-xmh/amll-ttml-db)
    - [LRCLIB](https://lrclib.net/)

- 🎶 **支持多种播放器**

  - <details><summary>网易云音乐</summary>

    - 请先安装 [BetterNCM 插件](https://microblock.cc/betterncm)。若安装后出现降级提示，请按提示将网易云降级至 2.10.13；
    - 然后在插件市场安装 InfLink 插件，安装完成后重启网易云即可！

    </details>

  - <details><summary>酷狗音乐</summary>

    - 请确保酷狗设置中已开启“支持系统播放控制，如锁屏界面控制”
    - 当前版本无法获取时间轴信息，意味着无法检测进度条的跳转

    </details>

  - <details><summary>Apple Music</summary>

    - 建议在设置中将“时间轴同步容差”调至 600ms 左右（设置 → 高级选项），否则歌词会前后晃动

    </details>

  - <details><summary>foobar2000</summary>

    - 请安装插件：https://github.com/dumbie/foo_mediacontrol

    </details>

  - Spotify  
  - QQ 音乐  
  - PotPlayer  
  - Windows 自带媒体播放器  

  - <details><summary>LX Music</summary>

    - 请在 LX Music 设置中开启“开放 API”
    - 然后在 BetterLyrics 设置中填写 LX 服务器地址（通常是 http://127.0.0.1:23330）

    </details>

  - <details><summary>MusicBee</summary>

    - 请安装插件：https://github.com/HenryPDT/mb_MediaControl

    </details>

  - <details><summary>iTunes</summary>

    - 请安装插件：https://github.com/thewizrd/iTunes-SMTC

    </details>

  - <details><summary>AIMP</summary>

    - 请安装插件：https://www.aimp.ru/?do=catalog&rec_id=1097

    </details>

- 🪟 **多种展示模式**
  - **标准模式**：沉浸式动画歌词 + 动态背景
  - **吸附模式**：屏幕边缘浮动歌词条
  - **桌面模式**：歌词悬浮在所有窗口之上

- 🧠 **智能行为**
  - 检测播放器关闭自动隐藏歌词

> 本项目仍在开发中，开发分支可能存在 bug 或异常行为。

## 软件截图

### 标准模式

![alt text](Screenshots/image.png)

![alt text](Screenshots/glow-float.gif)

![alt text](Screenshots/fan.png)

![alt text](Screenshots/lyrics-only.png)

![alt text](Screenshots/album-art-only.png)

### 吸附模式

![alt text](Screenshots/dock-1.png)

![alt text](Screenshots/dock-2.png)

### 桌面模式

![alt text](Screenshots/desktop-1.png)

![alt text](Screenshots/desktop-2.png)

## 使用演示

观看我们于 2025 年 7 月 7 日上传的哔哩哔哩介绍视频：[点击这里](https://www.bilibili.com/video/BV1zjGjzfEXh)

## 立即下载试用

### Microsoft Store 推荐安装方式

<a href="https://apps.microsoft.com/detail/9P1WCD1P597R?referrer=appbadge&mode=direct">
	<img src="https://get.microsoft.com/images/en-us%20dark.svg" width="200"/>
</a>

**最方便**的获取方式，支持**永久免费试用**与购买（免费版与付费版**无功能差异**）

☕ 如果你觉得本软件对你有帮助，欢迎在 Microsoft Store **支持我一杯奶茶** 🧧🥰

> 一旦有稳定版发布，Microsoft Store 将是第一更新渠道。

### 备用下载方式：Google Drive

你也可以在 [发布页](https://github.com/jayfunc/BetterLyrics/releases) 找到 Google Drive 下载链接

> 下载为 `.zip` 压缩包，安装方法请参阅：[安装指南](How2Install/How2Install.md)

## 致谢

- [Lyricify-Lyrics-Helper](https://github.com/WXRIW/Lyricify-Lyrics-Helper)  
  - 提供 QQ、网易、酷狗歌词解析与解密支持  
- [LRCLIB](https://lrclib.net/)  
  - 歌词 API 提供方  
- [Audio Tools Library (ATL) for .NET](https://github.com/Zeugma440/atldotnet)  
  - 用于提取音乐封面图像  
- [WinUIEx](https://github.com/dotMorten/WinUIEx)  
  - 提供窗口相关 Win32 API 简化访问方式  
- [TagLib#](https://github.com/mono/taglib-sharp)  
  - 用于读取音频文件中的歌词内容  
- [Vanara](https://github.com/dahall/Vanara)  
  - Win32 API 封装库  
- [StackOverflow - 如何为 WPF 动画 Margin 属性](https://stackoverflow.com/a/21542882/11048731)  
- [DevWinUI](https://github.com/ghost1372/DevWinUI)  
- [Bilibili -【WinUI3】SystemBackdropController 讲解](https://www.bilibili.com/video/BV1PY4FevEkS)  
- [博客园 - .NET App 与 SMTC 控制](https://www.cnblogs.com/TwilightLemon/p/18279496)  
- [Win2D 中的游戏循环介绍](https://www.cnblogs.com/walterlv/p/10236395.html)  
- [Win2D 示例 - Iris Blur](https://github.com/r2d2rigo/Win2D-Samples/blob/master/IrisBlurWin2D/IrisBlurWin2D/MainPage.xaml.cs)  
- [CommunityToolkit 教程](https://mvvm.coldwind.top/)

## 灵感来源

- [refined-now-playing-netease](https://github.com/solstice23/refined-now-playing-netease)
- [Lyricify-App](https://github.com/WXRIW/Lyricify-App)
- [椒盐音乐 Salt Player](https://moriafly.com/program/salt-player)
- [MyToolBar](https://github.com/TwilightLemon/MyToolBar)

## ✍️ 欢迎协助翻译！

找不到你的语言？
没关系！快来成为贡献者的一员吧！😆  
点击此 [链接](https://crowdin.com/project/betterlyrics/invite?h=d767e4f2dbd832d8fcdb6f7e5a198b402502866) 使用 Crowdin 在线翻译！

## Star 增长趋势

[![Star History Chart](https://api.star-history.com/svg?repos=jayfunc/BetterLyrics&type=Date)](https://www.star-history.com/#jayfunc/BetterLyrics&Date)

## 欢迎反馈和 PR！

发现 bug 请提交到 Issues，如有想法欢迎一起参与开发！
