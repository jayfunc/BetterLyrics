<a href="https://github.com/jayfunc/BetterLyrics/blob/dev/README.md">_**Click here to see the English version**_</a>

<div align="center">
  <img src="BetterLyrics.WinUI3/BetterLyrics.WinUI3/Assets/Logo.png" alt="" width="64"/>
</div>

<h2 align="center">
BetterLyrics
</h2>

<h3 align="center">
基于 WinUI 3 打造的丝滑动态歌词显示工具
</h3>

---

- [「BetterLyrics」反馈交流群（简体中文）](https://qun.qq.com/universal-share/share?ac=1&authKey=4Q%2BYTq3wZldYpF5SbS5c19ECFsiYoLZFAIcBNNzYpBUtiEjaZ8sZ%2F%2BnFN0qw3lad&busi_data=eyJncm91cENvZGUiOiIxMDU0NzAwMzg4IiwidG9rZW4iOiJiVnhqemVYN0N5QVc3b1ZkR24wWmZOTUtvUkJoWm1JRWlaWW5iZnlBcXJtZUtGc2FFTHNlUlFZMi9iRm03cWF5IiwidWluIjoiMTM5NTczOTY2MCJ9&data=39UmAihyH_o6CZaOs7nk2mO_lz2ruODoDou6pxxh7utcxP4WF5sbDBDOPvZ_Wqfzeey4441anegsLYQJxkrBAA&svctype=4&tempid=h5_group_info) (QQ群号：1054700388)
- [「BetterLyrics」反馈交流群（繁体中文/英文）](https://discord.gg/5yAQPnyCKv) (Discord)

---

## 核心特色

- 动态模糊专辑封面背景
- 丝滑的歌词淡入/淡出、缩放效果
- 歌曲切换时的流畅界面过渡
- 逐字渐变的卡拉OK效果（带光晕）
- 沉浸式桌面歌词（停靠模式）
- 本地化歌词翻译（支持 30 种语言）

> 项目仍在开发中，最新分支可能存在未修复的 Bug 或异常行为

## 支持的歌词来源

- 本地资源
  - 音乐文件（内嵌歌词）
  - [.lrc](<https://en.wikipedia.org/wiki/LRC_(file_format)>) 文件（标准格式与增强格式）
  - [.eslrc](https://github.com/ESLyric/release) 文件
  - [.ttml](https://en.wikipedia.org/wiki/Timed_Text_Markup_Language) 文件

（歌词下载推荐工具：[LDDC](https://github.com/chenmozhijin/LDDC)）

- 在线歌词源
  - QQ音乐
  - 网易云音乐
  - 酷狗音乐
  - [amll-ttml-db](https://github.com/Steve-xmh/amll-ttml-db)
  - [LRCLIB](https://lrclib.net/)

## 效果展示

### 标准模式

![标准模式](Screenshots/image.png)

![光晕浮动效果](Screenshots/glow-float.gif)

![歌词面板](Screenshots/fan.png)

![纯歌词模式](Screenshots/lyrics-only.png)

![纯专辑封面模式](Screenshots/album-art-only.png)

### 停靠模式

![停靠模式1](Screenshots/dock-1.png)

![停靠模式2](Screenshots/dock-2.png)

### 桌面模式

![桌面模式1](Screenshots/desktop-1.png)

![桌面模式2](Screenshots/desktop-2.png)

## 演示视频

观看 B 站演示视频（2025年7月7日发布）[点击此处](https://www.bilibili.com/video/BV1zjGjzfEXh)

## 立即体验

- 稳定版

<a href="https://apps.microsoft.com/detail/9P1WCD1P597R?referrer=appbadge&mode=direct">
	<img src="https://get.microsoft.com/images/zh-cn%20dark.svg" width="200"/>
</a>

> **最便捷**的获取方式，提供**无限制**免费试用或购买（免费版与付费版**功能完全一致**，购买视为对开发者的支持）

或通过 Google Drive 获取（链接见[发布页](https://github.com/jayfunc/BetterLyrics/releases/latest)）

> 下载的是 ".zip" 压缩包，安装指南请参阅[此文档](How2Install/How2Install.md)

- 开发版
可通过 `git clone` 拉取项目源码自行编译

## 已测试播放器

- 酷狗音乐
  - **时间轴同步限制**：酷狗不广播时间轴信息，调整进度时歌词无法实时跟随
- Apple Music
  - 需在设置中调整时间轴阈值至 600 ms 左右（路径：设置→高级选项）
- foobar2000
  - 需配合安装 [foo_mediacontrol](https://github.com/dumbie/foo_mediacontrol) 插件
- Spotify
- QQ音乐
- PotPlayer
- 系统自带媒体播放器

## 鸣谢

- [Lyricify-Lyrics-Helper](https://github.com/WXRIW/Lyricify-Lyrics-Helper)
  - 提供 QQ/网易云/酷狗歌词获取与解密
- [LRCLIB](https://lrclib.net/)
  - 歌词 API 服务
- [ATL.NET](https://github.com/Zeugma440/atldotnet)
  - 音乐文件封面提取
- [WinUIEx](https://github.com/dotMorten/WinUIEx)
  - Win32 窗口 API 封装
- [TagLib#](https://github.com/mono/taglib-sharp)
  - 歌词内容解析
- [Vanara](https://github.com/dahall/Vanara)
  - Win32 API 封装库
- [Stackoverflow - WPF边距动画实现](https://stackoverflow.com/a/21542882/11048731)
- [DevWinUI](https://github.com/ghost1372/DevWinUI)
- [B站 - WinUI3系统背景效果教程](https://www.bilibili.com/video/BV1PY4FevEkS)
- [博客园 - .NET应用与SMTC交互](https://www.cnblogs.com/TwilightLemon/p/18279496)
- [Win2D游戏循环实现](https://www.cnblogs.com/walterlv/p/10236395.html)
- [Win2D高级示例](https://github.com/r2d2rigo/Win2D-Samples)
- [CommunityToolkit开发指南](https://mvvm.coldwind.top/)

## 灵感来源

- [网易云歌词增强](https://github.com/solstice23/refined-now-playing-netease)
- [Lyricify-App](https://github.com/WXRIW/Lyricify-App)
- [椒盐音乐播放器](https://moriafly.com/program/salt-player)
- [MyToolBar任务栏工具](https://github.com/TwilightLemon/MyToolBar)

## 项目星标历史

[![Star History Chart](https://api.star-history.com/svg?repos=jayfunc/BetterLyrics&type=Date)](https://www.star-history.com/#jayfunc/BetterLyrics&Date)

## 欢迎反馈与贡献

如遇问题请提交 Issue，如有改进建议欢迎提交 PR