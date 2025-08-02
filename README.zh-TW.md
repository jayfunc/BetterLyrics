<div style="text-align: center;">

[❓ 點此查看常見問題 (FAQ)](#faq)

</div>

<div align="center">
  <img src="BetterLyrics.WinUI3/BetterLyrics.WinUI3/Assets/Logo.png" alt="" width="64">
</div>

<h2 align="center">
BetterLyrics
</h2>

<div style="text-align: center;">

[![](https://img.shields.io/badge/zh--CN-%E7%AE%80%E4%BD%93%E4%B8%AD%E6%96%87-blue)](https://github.com/jayfunc/BetterLyrics/blob/dev/README.zh-CN.md) [![Static Badge](https://img.shields.io/badge/zh--TW-%E7%B9%81%E9%AB%94%E4%B8%AD%E6%96%87-blue)](https://github.com/jayfunc/BetterLyrics/blob/dev/README.zh-TW.md) [![Static Badge](https://img.shields.io/badge/ja-%E6%97%A5%E6%9C%AC%E8%AA%9E-blue)](https://github.com/jayfunc/BetterLyrics/blob/dev/README.ja.md) [![Static Badge](https://img.shields.io/badge/ko-%ED%95%9C%EA%B5%AD%EC%9D%B8-blue)](https://github.com/jayfunc/BetterLyrics/blob/dev/README.ko.md)

</div>

<div style="text-align: center;">

![Static Badge](https://img.shields.io/badge/Language-C%23-purple) ![Static Badge](https://img.shields.io/badge/License-MIT-red) ![Static Badge](https://img.shields.io/badge/IDE-Visual%20Studio-purple) ![Static Badge](https://img.shields.io/badge/Framework-WinUI%203-blue)

</div>

<h4 align="center">
你的動態歌詞顯示工具，基於 WinUI 3 和 Win2D 構建 —— 支援本地播放及多種播放器
</h3>

## 🎉 本專案被少數派推薦！

查看文章：[BetterLyrics – 一款為 Windows 設計的沉浸式流暢歌詞顯示工具](https://sspai.com/post/101028)

## 🔈 反饋與交流群

- [<img src="BetterLyrics.WinUI3\BetterLyrics.WinUI3\Assets\QQ.png" height="20"> QQ](https://qun.qq.com/universal-share/share?ac=1&authKey=4Q%2BYTq3wZldYpF5SbS5c19ECFsiYoLZFAIcBNNzYpBUtiEjaZ8sZ%2F%2BnFN0qw3lad&busi_data=eyJncm91cENvZGUiOiIxMDU0NzAwMzg4IiwidG9rZW4iOiJiVnhqemVYN0N5QVc3b1ZkR24wWmZOTUtvUkJoWm1JRWlaWW5iZnlBcXJtZUtGc2FFTHNlUlFZMi9iRm03cWF5IiwidWluIjoiMTM5NTczOTY2MCJ9&data=39UmAihyH_o6CZaOs7nk2mO_lz2ruODoDou6pxxh7utcxP4WF5sbDBDOPvZ_Wqfzeey4441anegsLYQJxkrBAA&svctype=4&tempid=h5_group_info) (1054700388)
- [<img src="BetterLyrics.WinUI3\BetterLyrics.WinUI3\Assets\Discord.png" height="12"> Discord](https://discord.gg/5yAQPnyCKv)
- [<img src="BetterLyrics.WinUI3\BetterLyrics.WinUI3\Assets\Telegram.png" height="16"> Telegram](https://t.me/+svhSLZ7awPsxNGY1)

## 🌟 主要特色

- 🌠 **美觀的使用者介面**
  - 流暢動畫與特效
- ↔️ **強大的歌詞翻譯**
  - 離線機器翻譯（支援 30 種語言）
  - 自動讀取本地歌詞檔案中的嵌入翻譯
- 🧩 **多樣的歌詞來源**
  - 本地儲存
    - 音樂檔案（含嵌入歌詞）
    - [.lrc](<https://en.wikipedia.org/wiki/LRC_(file_format)>) 檔案（支援標準與增強格式）
    - [.eslrc](https://github.com/ESLyric/release) 檔案
    - [.ttml](https://en.wikipedia.org/wiki/Timed_Text_Markup_Language) 檔案
  - 線上歌詞來源
    - QQ 音樂
    - 網易雲音樂 NetEase Cloud Music
    - 酷狗音樂 Kugou Music
    - [amll-ttml-db](https://github.com/Steve-xmh/amll-ttml-db)
    - [LRCLIB](https://lrclib.net/)
- 🎶 **支援多種音樂播放器**

  - <details><summary>⚠️ 網易雲音樂</summary>

    - 請先安裝 [BetterNCM 插件](https://microblock.cc/betterncm)。如安裝後彈出降級指引，請依指引將網易雲音樂降級至 2.10.13；
    - 然後在插件市集安裝 InfLink 插件，安裝完成後重啟網易雲音樂即可。
    - ⚠️ 由於插件問題，時間軸可能存在異常

    </details>

  - <details><summary>⚠️ 酷狗音樂</summary>

    - 請確保酷狗音樂設定中「支援系統播放控制（如鎖屏介面）」已開啟
    - 酷狗音樂不會廣播時間軸資訊，導致切換進度時 BetterLyrics 無法偵測
    - ⚠️ 時間軸問題為酷狗本身限制

    </details>

  - <details><summary>⚠️ Apple Music</summary>

    - 請在設定中將時間軸閾值設為約 600ms（「設定」-「進階選項」），否則歌詞會不斷前後跳動
    - ⚠️ 需額外設定以避免歌詞抖動（詳見文末 FAQ）

    </details>

  - <details><summary>⚠️ foobar2000</summary>

    - 請安裝 https://github.com/dumbie/foo_mediacontrol
    - ⚠️ 由於插件問題，時間軸可能存在異常

    </details>

  - Spotify
  - QQ 音樂
  - PotPlayer
  - 媒體播放器（系統）

  - <details><summary>LX Music</summary>

    - 請確保已在 LX Music 設定頁開啟「Open API」
    - 然後在 BetterLyrics 設定-進階選項中填寫 LX Music 伺服器位址（通常為 http://127.0.0.1:23330）即可

    </details>

  - <details><summary>MusicBee</summary>

    - 請安裝 https://github.com/HenryPDT/mb_MediaControl

    </details>

  - <details><summary>iTunes</summary>

    - 請安裝 https://github.com/thewizrd/iTunes-SMTC

    </details>

  - <details><summary>AIMP</summary>

    - 請安裝 https://www.aimp.ru/?do=catalog&rec_id=1097

    </details>

- 🪟 **多種顯示模式**
  - **標準模式**
    - 沉浸式歌詞動畫與動態背景
  - **停靠模式**
    - 智慧歌詞條停靠螢幕邊緣
  - **桌面模式**
    - 歌詞懸浮於桌面應用之上
- 🧠 **智慧行為**
  - 音樂暫停時自動隱藏

> 本專案仍在開發中，最新分支可能存在 bug 或異常行為。

## 截圖

### 標準模式

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

在 B 站觀看我們的介紹影片（2025 年 7 月 7 日上傳）：[點此觀看](https://www.bilibili.com/video/BV1zjGjzfEXh)

## 立即體驗

### Microsoft Store

<a href="https://apps.microsoft.com/detail/9P1WCD1P597R?referrer=appbadge&mode=direct">
	<img src="https://get.microsoft.com/images/en-us%20dark.svg" width="200"/>
</a>

**最簡單**的獲取方式。**無限制**免費試用或購買（免費與付費無差別）

☕ 如果覺得好用，歡迎在 **Microsoft Store** 購買支持 🧧，感謝！🥰

> 穩定版發布後，Microsoft Store 會第一時間更新。

### Google Drive

也可透過 Google Drive 取得（見 [release](https://github.com/jayfunc/BetterLyrics/releases) 頁面）

> 下載的是 ".zip" 檔案，安裝方法請參考 [此文件](How2Install/How2Install.md)。

## 💖 特別感謝

- [Lyricify-Lyrics-Helper](https://github.com/WXRIW/Lyricify-Lyrics-Helper)
  - 提供 QQ、網易雲、酷狗歌詞獲取、解密與解析
- [lrclib](https://github.com/tranxuanthang/lrclib)
  - LRCLIB 歌詞 API 提供方
- [Audio Tools Library (ATL) for .NET](https://github.com/Zeugma440/atldotnet)
  - 用於提取音樂檔案中的圖片
- [WinUIEx](https://github.com/dotMorten/WinUIEx)
  - 提供便捷的 Win32 視窗 API
- [TagLib#](https://github.com/mono/taglib-sharp)
  - 用於讀取原始歌詞內容
- [Vanara](https://github.com/dahall/Vanara)
  - Win32 API 封裝庫
- [LibreTranslate](https://github.com/LibreTranslate/LibreTranslate)
  - 提供離線歌詞翻譯能力
- [Stackoverflow - 如何在 WPF 中動畫化 Margin 屬性](https://stackoverflow.com/a/21542882/11048731)
- [DevWinUI](https://github.com/ghost1372/DevWinUI)
- [Bilibili -【WinUI3】SystemBackdropController：定義雲母、壓克力效果](https://www.bilibili.com/video/BV1PY4FevEkS)
- [cnblogs - .NET App 與 Windows 系統媒體控制(SMTC)互動](https://www.cnblogs.com/TwilightLemon/p/18279496)
- [Win2D 中的遊戲循環：CanvasAnimatedControl](https://www.cnblogs.com/walterlv/p/10236395.html)
- [r2d2rigo/Win2D-Samples](https://github.com/r2d2rigo/Win2D-Samples/blob/master/IrisBlurWin2D/IrisBlurWin2D/MainPage.xaml.cs)
- [CommunityToolkit - 從入門到精通](https://mvvm.coldwind.top/)

## 靈感來源

- [refined-now-playing-netease](https://github.com/solstice23/refined-now-playing-netease)
- [Lyricify-App](https://github.com/WXRIW/Lyricify-App)
- [椒鹽音樂 Salt Player](https://moriafly.com/program/salt-player)
- [MyToolBar](https://github.com/TwilightLemon/MyToolBar)

## ✍️ 歡迎協助翻譯

沒有找到你的語言？
別擔心！快來參與翻譯，成為貢獻者之一吧！😆
點擊[此連結](https://crowdin.com/project/betterlyrics/invite?h=d767e4f2dbd832d8fcdb6f7e5a198b402502866)透過 Crowdin 翻譯本應用！

## Star 記錄

[![](https://api.star-history.com/svg?repos=jayfunc/BetterLyrics&type=Date)](https://www.star-history.com/#jayfunc/BetterLyrics&Date)

## 歡迎提 issue 和 PR

如發現 bug 請在 issues 提出，或有想法歡迎在此分享。

---

## FAQ

### 停靠模式下看不到按鈕

進入「停靠模式」後，操作按鈕會隱藏。將滑鼠懸停在視窗頂部即可顯示「沉浸」、「更多」、「關閉」按鈕。

![alt text](FAQ/image-10.png)

將滑鼠懸停在視窗底部邊緣稍上方，會顯示底部白色懸浮控制視窗

![alt text](FAQ/image-11.png)

點擊「小白條」可顯示底部懸浮控制欄（含目前播放進度、時間軸偏移調整、上一曲/暫停/下一曲、翻譯、佈局、設定）

![alt text](FAQ/image-12.png)

### 桌面模式如何鎖定視窗

![alt text](FAQ/image-6.png)

將滑鼠懸停在頂部，點擊鎖定圖示即可，或按 `Ctrl + Alt + U`。

### 桌面模式如何解鎖視窗

![alt text](FAQ/image-7.png)

在系統匣右鍵圖示，選擇「解鎖視窗」，或按 `Ctrl + Alt + U`。

### 歌詞時間軸有延遲

將滑鼠懸停在應用底部，

![alt text](FAQ/image.png)

點擊第一個圖示按鈕（歌詞時間軸偏移），即可自由調整偏移量。

### 歌詞頻繁跳動（如 Apple Music）

![alt text](FAQ/image-2.png)

進入「進階選項」，增大閾值（紅框標記處），直到歌詞正常。
