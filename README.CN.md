<a href="https://github.com/jayfunc/BetterLyrics/blob/dev/README.md">_**Click here to see the English version**_</a>

<div align="center">
  <img src="BetterLyrics.WinUI3/BetterLyrics.WinUI3/Assets/Logo.png" alt="" width="64"/>
</div>

<h2 align="center">
BetterLyrics
</div>

<h3 align="center">
您的流畅动态歌词显示工具，基于 WinUI 3 构建
</div>

---

## 主要特色功能

- 动态模糊专辑封面作为背景
- 流畅的歌词淡入淡出、缩放效果
- 歌曲切换时界面的平滑过渡
- 每个字符的渐变卡拉OK（带发光）效果
- 沉浸式桌面歌词（停靠模式）

> 本项目仍在开发中，最新的开发分支可能存在 bug 和意外行为。

## 支持的歌词来源

- 本地存储
  - 音乐文件（内嵌歌词）
  - [.lrc](https://en.wikipedia.org/wiki/LRC_(file_format)) 文件（支持核心格式和增强格式）
  - [.eslrc](https://github.com/ESLyric/release) 文件
  - [.ttml](https://en.wikipedia.org/wiki/Timed_Text_Markup_Language) 文件

- 在线歌词提供商
  - QQ音乐
  - 网易云音乐
  - 酷狗音乐
  - [amll-ttml-db](https://github.com/Steve-xmh/amll-ttml-db)
  - [LRCLIB](https://lrclib.net/)

## 按您的方式定制

我们提供多种设置选项来更好地满足您的偏好

- 主题
  - 浅色
  - 深色
  - 跟随系统

- 背景效果
  - 无
  - 云母效果
  - 亚克力效果
  - 透明

- 专辑封面作为背景
  - 动态效果
  - 模糊程度
  - 不透明度

- 专辑封面作为封面
  - 圆角半径

- 歌词
  - 对齐方式
  - 字体大小
  - 字体颜色 **（来自专辑封面主题色）**
  - 行间距
  - 不透明度
  - 模糊程度
  - 动态**发光**效果
    - 逐行
    - 逐词

- 语言
  - 英语
  - 简体中文
  - 繁体中文
  - 日语
  - 韩语

## 截图展示

![模式展示](Screenshots/mode.png)

![发光效果](Screenshots/glow.png)

![发光动画](Screenshots/glow.gif)

![停靠模式](Screenshots/dock.png)

![沉浸式停靠](Screenshots/immersive-dock.gif)

![停靠动画](Screenshots/dock.gif)

![画中画模式](Screenshots/pip.png)

![设置界面](Screenshots/settings.png)

![全屏模式](Screenshots/fs.png)

## 演示视频

观看我们在 Bilibili 上的介绍视频（2025年5月31日上传）[点击这里](https://b23.tv/QjKkYmL)。

## 立即体验

### 稳定版本

<a href="https://apps.microsoft.com/detail/9P1WCD1P597R?referrer=appbadge&mode=direct">
	<img src="https://get.microsoft.com/images/en-us%20dark.svg" width="200"/>
</a>

> 获取软件的**最简单**方式。**无限制**免费试用或购买（免费版和付费版**没有区别**，如果您喜欢的话可以购买来支持我）

或者从 Google Drive 下载（请查看 [发布页面](https://github.com/jayfunc/BetterLyrics/releases/latest) 获取链接）

> 请注意您下载的是".zip"文件，关于如何安装的指南，请参考 [这个文档](How2Install/How2Install.md)。

### 最新开发版本

您可以 `git clone` 这个项目并自己构建。

## 应用设置

本项目依赖于监听来自 [SMTC](https://learn.microsoft.com/en-ca/windows/uwp/audio-video-camera/integrate-with-systemmediatransportcontrols) 的消息，因此大多数音乐播放器都可以使用。

### 关于歌词

为了获得更好的体验，您可以使用 [LDDC](https://github.com/chenmozhijin/LDDC) 来下载歌词。

## 未来计划

稍后添加。

## 特别感谢

- [Lyricify-Lyrics-Helper](https://github.com/WXRIW/Lyricify-Lyrics-Helper)
  - 提供开箱即用的 QQ，网易，酷狗歌词获取、解密、解析帮助类
- [LRCLIB](https://lrclib.net/)
  - 在线歌词 API 提供商
- [Audio Tools Library (ATL) for .NET](https://github.com/Zeugma440/atldotnet)
  - 用于提取音乐文件中的图片
- [WinUIEx](https://github.com/dotMorten/WinUIEx)
  - 提供访问 Win32 窗口 API 的便捷方式
- [TagLib#](https://github.com/mono/taglib-sharp)
  - 用于读取原始歌词内容
- [Stackoverflow - How to animate Margin property in WPF](https://stackoverflow.com/a/21542882/11048731)
- [DevWinUI](https://github.com/ghost1372/DevWinUI)
- [Bilibili -【WinUI3】SystemBackdropController：定义云母、亚克力效果](https://www.bilibili.com/video/BV1PY4FevEkS)
- [博客园 - .NET App 与 Windows 系统媒体控制(SMTC)交互](https://www.cnblogs.com/TwilightLemon/p/18279496)
- [Win2D 中的游戏循环：CanvasAnimatedControl](https://www.cnblogs.com/walterlv/p/10236395.html)
- [r2d2rigo/Win2D-Samples](https://github.com/r2d2rigo/Win2D-Samples/blob/master/IrisBlurWin2D/IrisBlurWin2D/MainPage.xaml.cs)
- [CommunityToolkit - 从入门到精通](https://mvvm.coldwind.top/)

## 灵感来源

- [refined-now-playing-netease](https://github.com/solstice23/refined-now-playing-netease)
- [Lyricify-App](https://github.com/WXRIW/Lyricify-App)
- [椒盐音乐 Salt Player](https://moriafly.com/program/salt-player)
- [MyToolBar](https://github.com/TwilightLemon/MyToolBar)

## Star 历史

[![Star History Chart](https://api.star-history.com/svg?repos=jayfunc/BetterLyrics&type=Date)](https://www.star-history.com/#jayfunc/BetterLyrics&Date)

## 欢迎任何问题反馈和 PR

如果您发现 bug，请在 issues 中提交，或者如果您有任何想法，请随时在这里分享。

或者您也可以加入群聊来分享您宝贵的反馈：
- [「BetterLyrics」反馈交流群](https://qun.qq.com/universal-share/share?ac=1&authKey=4Q%2BYTq3wZldYpF5SbS5c19ECFsiYoLZFAIcBNNzYpBUtiEjaZ8sZ%2F%2BnFN0qw3lad&busi_data=eyJncm91cENvZGUiOiIxMDU0NzAwMzg4IiwidG9rZW4iOiJiVnhqemVYN0N5QVc3b1ZkR24wWmZOTUtvUkJoWm1JRWlaWW5iZnlBcXJtZUtGc2FFTHNlUlFZMi9iRm03cWF5IiwidWluIjoiMTM5NTczOTY2MCJ9&data=39UmAihyH_o6CZaOs7nk2mO_lz2ruODoDou6pxxh7utcxP4WF5sbDBDOPvZ_Wqfzeey4441anegsLYQJxkrBAA&svctype=4&tempid=h5_group_info) (1054700388) QQ群
- [「BetterLyrics」Feedback Chat Group](https://chat.whatsapp.com/Gye4K87FlwQ7dBvz0E06v0?mode=ac_c) WhatsApp群组