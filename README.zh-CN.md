<div style="text-align: center;">

[❓单击此处查看常见问题（常见问题解答）](#faq)

</div>

<div align="center">
  <img src="BetterLyrics.WinUI3/BetterLyrics.WinUI3/Assets/Logo.png" alt="" width="64">
</div>

<h2 align="center">
BetterLyrics
</h2>

<div style="text-align: center;">

[![](https://img.shields.io/badge/zh--CN-%E7%AE%80%E4%BD%93%E4%B8%AD%E6%96%87-blue)](https://github.com/jayfunc/BetterLyrics/blob/dev/README.zh-CN.md)[![Static Badge](https://img.shields.io/badge/zh--TW-%E7%B9%81%E9%AB%94%E4%B8%AD%E6%96%87-blue)](https://github.com/jayfunc/BetterLyrics/blob/dev/README.zh-TW.md)[![Static Badge](https://img.shields.io/badge/ja-%E6%97%A5%E6%9C%AC%E8%AA%9E-blue)](https://github.com/jayfunc/BetterLyrics/blob/dev/README.ja.md)[![Static Badge](https://img.shields.io/badge/ko-%ED%95%9C%EA%B5%AD%EC%9D%B8-blue)](https://github.com/jayfunc/BetterLyrics/blob/dev/README.ko.md)

</div>

<div style="text-align: center;">

![Static Badge](https://img.shields.io/badge/Language-C%23-purple)![Static Badge](https://img.shields.io/badge/License-MIT-red)![Static Badge](https://img.shields.io/badge/IDE-Visual%20Studio-purple)![Static Badge](https://img.shields.io/badge/Framework-WinUI%203-blue)

</div>

<h4 align="center">
Your dynamic lyrics display tool built with WinUI 3 and Win2D — works with local playback and other players
</h3>

## 🎉该项目由Sspai展出！

查看文章：[更好的词 - 一个为窗户设计的沉浸式和光滑的歌词展示工具](https://sspai.com/post/101028)

## 🔈反馈和聊天组

-   [<img src="BetterLyrics.WinUI3\BetterLyrics.WinUI3\Assets\QQ.png" height="20">QQ](https://qun.qq.com/universal-share/share?ac=1&authKey=4Q%2BYTq3wZldYpF5SbS5c19ECFsiYoLZFAIcBNNzYpBUtiEjaZ8sZ%2F%2BnFN0qw3lad&busi_data=eyJncm91cENvZGUiOiIxMDU0NzAwMzg4IiwidG9rZW4iOiJiVnhqemVYN0N5QVc3b1ZkR24wWmZOTUtvUkJoWm1JRWlaWW5iZnlBcXJtZUtGc2FFTHNlUlFZMi9iRm03cWF5IiwidWluIjoiMTM5NTczOTY2MCJ9&data=39UmAihyH_o6CZaOs7nk2mO_lz2ruODoDou6pxxh7utcxP4WF5sbDBDOPvZ_Wqfzeey4441anegsLYQJxkrBAA&svctype=4&tempid=h5_group_info)(1054700388)
-   [<img src="BetterLyrics.WinUI3\BetterLyrics.WinUI3\Assets\Discord.png" height="12">不和谐](https://discord.gg/5yAQPnyCKv)
-   [<img src="BetterLyrics.WinUI3\BetterLyrics.WinUI3\Assets\Telegram.png" height="16">电报](https://t.me/+svhSLZ7awPsxNGY1)

## 🌟突出显示功能

-   🌠**取悦用户界面**
    -   流利的动画和效果

-   ↔️**强烈的歌词翻译**
    -   离线机器翻译（支持30种语言）
    -   自动读取嵌入式翻译的本地歌词文件

-   🧩**各种歌词来源**
    -   本地存储
        -   音乐文件（带有嵌入式歌词）
        -   [.lrc](https://en.wikipedia.org/wiki/LRC_(file_format))文件（具有核心格式和增强格式）
        -   [.eslrc](https://github.com/ESLyric/release)文件
        -   [.ttml](https://en.wikipedia.org/wiki/Timed_Text_Markup_Language)文件
    -   在线歌词提供商
        -   QQ音乐
        -   网易云音乐 NetEase Cloud Music
        -   酷狗音乐 Kugou Music
        -   [AMLL-TTML-DB](https://github.com/Steve-xmh/amll-ttml-db)
        -   [lrclib](https://lrclib.net/)

-   🎶**支持多个音乐播放器**

    -   <details><summary>⚠️ NetEase Cloud Music</summary>

        -   请安装[BetterNCM插件](https://microblock.cc/betterncm) first. If a downgrade guide pops up after the installation, please follow the guide to complete the downgrade of NetEase Cloud Music (downgrade to 2.10.13);
        -   之后，请在Pluginmarket中安装Afflink插件。安装完成后，请重新启动NetASE Cloud Music。在这一点上，所有准备操作都已经完成，请享受！
        -   ⚠️请注意，由于插件问题，时间表存在问题

        </details>

    -   <details><summary>⚠️ Kugou Music</summary>

        -   请确保Kugou音乐设置“支持系统播放控件，例如锁定屏幕接口”
        -   没有广播的时间表信息，这意味着当您更改Kugou Music中的时间轴位置时，Betterlyrics无法检测到此更改。
        -   ⚠️请注意，由于Kugou本身，时间表有问题

        </details>

    -   <details><summary>⚠️ Apple Music</summary>

        -   确保您将时间轴阈值设置为设置约600毫秒左右（转到“设置”  - “高级选项”以进行更改），否则，歌词将不断向前发展。
        -   ⚠️请注意，您需要其他设置才能阻止摇晃歌词（有关更多信息，请参见本文档末尾的常见问题解答）

        </details>

    -   <details><summary>⚠️ foobar2000</summary>

        -   确保你有<https://github.com/dumbie/foo_mediacontrol>与之安装
        -   ⚠️请注意，由于插件问题，时间表存在问题

        </details>

    -   Spotify

    -   QQ音乐

    -   Potplayer

    -   媒体播放器（系统）

    -   <details><summary>LX Music</summary>

        -   请确保您在LX音乐设置页面中启用了“打开API”
        -   然后打开更好的浏览器，转到设置，转到“高级选项”，输入您的LX音乐服务器地址（主要是喜欢<http://127.0.0.1:23330>）然后你去！

        </details>

    -   <details><summary>MusicBee</summary>

        -   请安装<https://github.com/HenryPDT/mb_MediaControl>使用之前

        </details>

    -   <details><summary>iTunes</summary>

        -   请安装<https://github.com/thewizrd/iTunes-SMTC>使用之前

        </details>

    -   <details><summary>AIMP</summary>

        -   请安装<https://www.aimp.ru/?do=catalog&rec_id=1097>使用之前

        </details>

-   🪟**多个显示模式**
    -   **标准模式**
        -   享受带有丰富歌词动画和充满活力的背景的沉浸式听力旅程
    -   **码头模式**
        -   一个聪明的动画歌词酒吧停靠在您的屏幕边缘
    -   **桌面模式**
        -   享受漂浮在应用上方的身临其境的歌词

-   🧠**聪明的行为**
    -   音乐停顿时自动隐藏

> 该项目仍在开发中，最新分支可能存在错误和意外行为。

## 屏幕截图

### 标准模式

![alt text](Screenshots/image.png)

![alt text](Screenshots/glow-float.gif)

![alt text](Screenshots/fan.png)

![alt text](Screenshots/lyrics-only.png)

![alt text](Screenshots/album-art-only.png)

### 码头模式

![alt text](Screenshots/dock-1.png)

![alt text](Screenshots/dock-2.png)

### 桌面模式

![alt text](Screenshots/desktop-1.png)

![alt text](Screenshots/desktop-2.png)

## 示范

在比利比利观看我们的介绍视频（于2025年7月7日上传）[这里](https://www.bilibili.com/video/BV1zjGjzfEXh).

## 现在尝试

### Microsoft Store

<a href="https://apps.microsoft.com/detail/9P1WCD1P597R?referrer=appbadge&mode=direct">
	<img src="https://get.microsoft.com/images/en-us%20dark.svg" width="200"/>
</a>

**最简单**获取它的方法。**无限**免费步道或购买（有**没有区别**在免费版本和付费版本之间）

☕如果您觉得有用，请考虑购买🧧**Microsoft Store**，我会感谢它！ 🥰

> 当建立稳定版本时，Microsoft Store将是第一个更新的频道。

### Google Drive

或从Google Drive获取（请参阅[发布](https://github.com/jayfunc/BetterLyrics/releases)链接的页面）

> 请注意，您正在下载“ .zip”文件，以获取有关如何安装的指南[这个文档](How2Install/How2Install.md).

## 💖非常感谢

-   [抒情式 - 莱errics-helper](https://github.com/WXRIW/Lyricify-Lyrics-Helper)
    -   为QQ，NetEase，Kugou来源提供歌词获取，解密和解析
-   [lrclib](https://github.com/tranxuanthang/lrclib)
    -   LRCLIB歌词API提供商
-   [.NET的音频工具库（ATL）](https://github.com/Zeugma440/atldotnet)
    -   用于在音乐文件中提取图片
-   [winuiex](https://github.com/dotMorten/WinUIEx)
    -   提供简单的方法来访问Win32 API有关窗口
-   [taglib＃](https://github.com/mono/taglib-sharp)
    -   用于阅读原始歌词内容
-   [老式](https://github.com/dahall/Vanara)
    -   Win32 API包装器
-   [librenslate](https://github.com/LibreTranslate/LibreTranslate)
    -   提供离线歌词翻译的能力
-   [stackoverflow-如何在WPF中使用Anim Anim Anim Anim Anim Anim Anive Margin属性](https://stackoverflow.com/a/21542882/11048731)
-   [揭示](https://github.com/ghost1372/DevWinUI)
-   [Bilibili -【WinUI3】SystemBackdropController：定义云母、亚克力效果](https://www.bilibili.com/video/BV1PY4FevEkS)
-   [cnblogs - .NET App 与 Windows 系统媒体控制(SMTC)交互](https://www.cnblogs.com/TwilightLemon/p/18279496)
-   [Win2D 中的游戏循环：CanvasAnimatedControl](https://www.cnblogs.com/walterlv/p/10236395.html)
-   [R2D2RIGO/WIN2D-SPALES](https://github.com/r2d2rigo/Win2D-Samples/blob/master/IrisBlurWin2D/IrisBlurWin2D/MainPage.xaml.cs)
-   [CommunityToolkit - 从入门到精通](https://mvvm.coldwind.top/)

## 受到启发

-   [改进的网状网络](https://github.com/solstice23/refined-now-playing-netease)
-   [Lyricify-App](https://github.com/WXRIW/Lyricify-App)
-   [椒盐音乐 Salt Player](https://moriafly.com/program/salt-player)
-   [mytoolbar](https://github.com/TwilightLemon/MyToolBar)

## ✍️帮助我们转化为您的语言

找不到您的语言？
不用担心！开始翻译并成为贡献者之一！ 😆
单击[关联](https://crowdin.com/project/betterlyrics/invite?h=d767e4f2dbd832d8fcdb6f7e5a198b402502866)现在通过Crowdin将此应用转换为您的语言！

## 星历史

[![](https://api.star-history.com/svg?repos=jayfunc/BetterLyrics&type=Date)](https://www.star-history.com/#jayfunc/BetterLyrics&Date)

## 任何问题和公关都受到欢迎

如果您找到错误，请在问题中提交错误，或者您有任何想法可以随时在这里分享。

* * *

## FAQ

### 我在码头模式下看不到任何按钮

重要的是要注意，当您输入“停靠模式”时，操作按钮就会隐藏。将鼠标悬停在顶部，以访问“浸入”，“更多”和“关闭”按钮。

![alt text](FAQ/image-10.png)

将鼠标悬停在窗户底部边缘上方，以显示底部的白色控制浮动窗口

![alt text](FAQ/image-11.png)

点击“小白色栏”以浮动窗口形式显示底部控制条（包括当前的播放进度视图，时间表偏移调整；上一首歌，暂停/播放，下一首歌；翻译，布局，设置）

![alt text](FAQ/image-12.png)

### 如何在桌面模式下锁定窗口

![alt text](FAQ/image-6.png)

将鼠标悬停在顶部，单击锁定图标，您可以走！或者，请按`Ctrl + Alt + U`.

### 如何在桌面模式下解锁窗口

![alt text](FAQ/image-7.png)

它在系统托盘中，右键单击图标，您会看到“解锁窗口”。或者，请按`Ctrl + Alt + U`.

### 歌词时间表延迟

将鼠标悬停在应用程序的底部，

![alt text](FAQ/image.png)

然后单击第一个图标按钮（歌词时间线偏移），在这里您可以自由调整偏移量。

### 歌词经常来回跳动（例如，苹果音乐）

![alt text](FAQ/image-2.png)

转到“高级选项”部分，增加阈值值（用较大的红色矩形标记），直到歌词正常工作。
