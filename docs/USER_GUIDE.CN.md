[**English**](USER_GUIDE.md) | **中文**

# ❓ 常见问题解答 (FAQ)

## 🎵 播放与连接

### 🤔 我在使用网易云音乐作为音乐播放器

强烈建议首先浏览 [此处](#已知支持的音乐播放器配置指南) 的配置指南。

### 🤔 显示没有音乐正在播放

部分播放器需要额外配置，请查看 [此处](#已知支持的音乐播放器配置指南)。

### 🤔 怎么通过本软件播放本地歌曲？

1. 请确保本地歌曲文件夹已添加到软件内（参考 [如何设置本地歌词源？](#-如何设置本地歌词源) 中的第一步）
2. 打开 `音乐库`
    - 可通过歌词窗口左上区域的图书按钮打开
    - 或在系统托盘右键本软件，选择打开音乐库

## 🪟 窗口与交互

### 🤔 如何移动和调整窗口大小？

如果您无法选择和移动窗口，请检查 `歌词窗口管理` 的 `窗口` 设置中，`可拖动区域` 的选项是否误设为 `无`。

### 🤔 我看不到任何按钮 / 底部命令栏被隐藏了

**顶部与底部命令栏：**

- 默认情况下，当鼠标移出顶部命令栏和底部命令栏（播放控制面板）区域时，它们会自动隐藏。只需将鼠标悬停在这些区域即可再次显示它们。
- 如果窗口已锁定，悬浮到窗口上放即可显示解锁按钮。

**无法唤出底部面板：**
当窗口太小无法显示该面板时，只有将鼠标悬停在歌词窗口底部并单击白线，才能显示播放控制面板。

### 🤔 软件有哪些默认快捷键？

- `Ctrl + Alt + H`：显示/隐藏歌词窗口
- `Ctrl + Alt + S`：切换歌词窗口状态
- `Ctrl + Alt + P`：播放/暂停当前曲目
- `Ctrl + Alt + Right`：下一曲目
- `Ctrl + Alt + Left`：上一曲目

> 可前往 `设置` -> `应用外观与行为` 更改上述快捷键

## 📃 歌词显示

### 🤔 歌词显示错误（匹配错误）

- 请前往 `设置` -> `播放源` -> `歌词搜索策略` 选择 `最佳匹配`
> 注意：对于每首歌曲，首次使用最佳匹配时将耗时 10 秒左右，请耐心等待

- 或打开歌词搜索窗口手动搜索正确的歌词。

> 歌词搜索窗口可通过下述一种方式打开：
> - 将鼠标移至窗口靠下区域，直至底部控制栏出现，点击右下侧放大镜图标（🔍）以打开歌词搜索窗口
> - 右键系统托盘内本软件的图标，选择 “打开歌词窗口”

### 🤔 歌词一直在来回移动（时间轴跳动）

前往 `设置` > `播放源` > 禁用 `歌词时间轴同步` 或提高 `歌词时间轴同步阈值`。

### 🤔 如何设置本地歌词源？

**第一步：添加文件夹**
前往 `设置` -> `媒体库`，点击 `添加` 按钮，将您存放歌词（或包含内嵌歌词的音乐文件）的文件夹添加进去，并确保右侧开关为 **“开启”** 状态。

**第二步：准备文件**
软件支持两种方式：

1.  **外挂歌词文件 (.lrc/.eslrc/.ttml)**
    软件进行比对时，**不区分词语顺序**，且**自动忽略标点符号**。
    * **推荐命名：** `歌手名 - 歌名.lrc`
    * **也支持：** `歌名 歌手名.lrc`
    * **同样支持：** `歌名 (Live) - 歌手名.lrc`
    * *例：播放 Taylor Swift 的 "Love Story" 时，文件名为 `love_story_taylor_swift.lrc` 也能被精准识别。*

2.  **内嵌歌词**
    如果您的本地音乐文件（.mp3/.flac等）标签中已写入歌词，软件也会自动读取。

> 推荐下述几种方式检查音乐文件的标签信息：
> - [音乐标签](https://www.cnblogs.com/vinlxc/p/11347744.html)：可浏览本地歌曲元信息
> - 在 **第一步：添加文件夹** 已完成的情况下可前往 BetterLyrics 软件 `音乐库` 单击音乐条目的右侧三个点图标（···）显示解析的元信息（可能与实际信息有出入）

> 为了确保 **内嵌歌词** 方式的顺利匹配，推荐下述工具：
> - [音乐标签](https://www.cnblogs.com/vinlxc/p/11347744.html)：可批量将 **所有元信息** 写入本地音乐文件标签
> - [LDDC](https://github.com/chenmozhijin/LDDC)：可批量将 **逐字歌词** 写入本地音乐文件标签

### 🤔 歌词显示部分乱码

一般情况是默认字体设置问题，前往 `设置` -> `歌词窗口管理` -> `歌词样式` -> `歌词字体` -> 键入 `Arial` 以获得最稳妥的展示效果

## 🎨 模式与外观

### 🤔 如何添加更多模式？

软件初始已为您添加了所有预设的模式。

您可以导入他人与您分享的窗口状态文件：转到 `设置` -> `歌词窗口管理` -> 导入（向下箭头图标） -> 选择需要导入的文件（`.json` 格式）

> 要导出窗口状态，请右键窗口状态，选择 `导出`

### 🤔 如何切换模式？

- 按下默认快捷键 `Ctrl + Alt + S`，然后选择屏幕上显示的模式来切换模式。（按 `Esc` 键关闭选择窗口）
- 在歌词窗口左上区域找到双向箭头图标，点按即可唤出歌词窗口切换器

## 📂 数据与反馈

### 🤔 歌词缓存在哪里？

`%LocalAppData%\Packages\37412.BetterLyrics_rd1g0rsrrtxw8\LocalCache\lyrics`

### 🤔 如何反馈问题？（日志位置）

反馈时请附带软件日志，日志位置如下：
`%LocalAppData%\Packages\37412.BetterLyrics_rd1g0rsrrtxw8\LocalCache\logs`

# 已知支持的音乐播放器（配置指南）

使用方法：找到音乐播放器，安装表格中所列出的 `插件`。

> <mark>支持的音乐播放器包括但不限于下述列表，请以实际体验为准。</mark>

> <mark>凡是接入了 [SMTC](https://learn.microsoft.com/zh-cn/windows/uwp/audio-video-camera/integrate-with-systemmediatransportcontrols) 的媒体播放器均受支持。</mark>

| 音乐播放器 | 插件 | 注意事项 |
| :--- | :--- | :--- |
| [AIMP](https://www.aimp.ru) | [插件](https://www.aimp.ru/?do=catalog&rec_id=1097) | - |
| [AMLL Player](https://github.com/Steve-xmh/applemusic-like-lyrics/blob/full-refractor/packages/player/README.md) | - | - |
| [Apple Music](https://music.apple.com) | - | - |
| [BetterLyrics](https://github.com/jayfunc/BetterLyrics) | - | - |
| [foobar2000](https://www.foobar2000.org/windows) | [插件](https://github.com/dumbie/foo_mediacontrol) | 时间轴有问题 |
| [Google Chrome](https://www.google.com/chrome) | - | - |
| [HyPlayer](https://github.com/HyPlayer/HyPlayer) | - | - |
| [iTunes](https://apps.microsoft.com/detail/9pb2mz1zmb1s) | [插件](https://github.com/thewizrd/iTunes-SMTC) | - |
| [酷狗音乐](https://www.kugou.com) | - | ![](docs/assets/faq/Snipaste_2025-11-01_12-51-44.png?raw=true) 时间轴有问题 |
| [Listen 1](https://listen1.github.io/listen1) | - | - |
| [LX Music](https://github.com/lyswhut/lx-music-desktop) | - | 在 LX Music 设置 “开放 API” 中勾选 “启用开放 API 服务”；<br>在 BetterLyrics 设置 “播放源” 板块下的 “LX 音乐服务器” 中输入 LX Music 服务器地址。 |
| [Microsoft Edge](https://www.microsoft.com/edge) | - | - |
| [MoeKoe Music](https://github.com/MoeKoeMusic/MoeKoeMusic) | - | - |
| [MusicBee](https://getmusicbee.com) | [插件](https://github.com/HenryPDT/mb_MediaControl) | - |
| [MusicPlayer2](https://github.com/zhongyang219/MusicPlayer2) | - | - |
| [PlanetMusic](https://apps.microsoft.com/detail/9nt5122pwqb8) | - | - |
| [PotPlayer](https://potplayer.daum.net) | - | - |
| [QQ 音乐](https://y.qq.com) | - | 确保使用较新版本，并在 QQ 音乐设置中启用 “显示系统媒体传输控制 (SMTC)”。 |
| [Salt Player for Windows](https://moriafly.com/program/spw.html) | - | - |
| [Spotify](https://open.spotify.com) | - | - |
| [网易云音乐](https://music.163.com) | [BetterNCM](https://microblock.cc/betterncm) <br> [InfLink-rs](https://github.com/apoint123/inflink-rs) | ![](docs/assets/faq/Snipaste_2025-11-01_19-25-56.png?raw=true) |
| [Windows Media Player](https://apps.microsoft.com/detail/9wzdncrfj3pt) | - | - |
| ... | ... | ... |

# 歌词源配置

## Apple Music

- 打开 [Apple Music 网站](https://music.apple.com/) 和开发者工具窗口。
- 登录您的帐户。
- 刷新页面。
- 返回开发者工具窗口，选择 `Fetch/XHR`，选择一个请求，在请求头中找到 `media-user-token` 标头，并复制其值。
- 打开 BetterLyrics 并转到 `播放源` 设置。
- 将复制的值输入到 `Apple Music media-user-token` 设置中，然后点击右侧的接受图标。