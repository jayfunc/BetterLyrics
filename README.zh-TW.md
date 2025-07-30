<div style="text-align: center;">

[❓單擊此處查看常見問題（常見問題解答）](#faq)

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

## 🎉該項目由Sspai展出！

查看文章：[更好的詞 - 一個為窗戶設計的沉浸式和光滑的歌詞展示工具](https://sspai.com/post/101028)

## 🔈反饋和聊天組

-   [<img src="BetterLyrics.WinUI3\BetterLyrics.WinUI3\Assets\QQ.png" height="20">QQ](https://qun.qq.com/universal-share/share?ac=1&authKey=4Q%2BYTq3wZldYpF5SbS5c19ECFsiYoLZFAIcBNNzYpBUtiEjaZ8sZ%2F%2BnFN0qw3lad&busi_data=eyJncm91cENvZGUiOiIxMDU0NzAwMzg4IiwidG9rZW4iOiJiVnhqemVYN0N5QVc3b1ZkR24wWmZOTUtvUkJoWm1JRWlaWW5iZnlBcXJtZUtGc2FFTHNlUlFZMi9iRm03cWF5IiwidWluIjoiMTM5NTczOTY2MCJ9&data=39UmAihyH_o6CZaOs7nk2mO_lz2ruODoDou6pxxh7utcxP4WF5sbDBDOPvZ_Wqfzeey4441anegsLYQJxkrBAA&svctype=4&tempid=h5_group_info)(1054700388)
-   [<img src="BetterLyrics.WinUI3\BetterLyrics.WinUI3\Assets\Discord.png" height="12">不和諧](https://discord.gg/5yAQPnyCKv)
-   [<img src="BetterLyrics.WinUI3\BetterLyrics.WinUI3\Assets\Telegram.png" height="16">電報](https://t.me/+svhSLZ7awPsxNGY1)

## 🌟突出顯示功能

-   🌠**取悅用戶界面**
    -   流利的動畫和效果

-   ↔️**強烈的歌詞翻譯**
    -   離線機器翻譯（支持30種語言）
    -   自動讀取嵌入式翻譯的本地歌詞文件

-   🧩**各種歌詞來源**
    -   本地存儲
        -   音樂文件（帶有嵌入式歌詞）
        -   [.lrc](https://en.wikipedia.org/wiki/LRC_(file_format))文件（具有核心格式和增強格式）
        -   [.eslrc](https://github.com/ESLyric/release)文件
        -   [.ttml](https://en.wikipedia.org/wiki/Timed_Text_Markup_Language)文件
    -   在線歌詞提供商
        -   QQ音樂
        -   網易云音樂 NetEase Cloud Music
        -   酷狗音樂 Kugou Music
        -   [AMLL-TTML-DB](https://github.com/Steve-xmh/amll-ttml-db)
        -   [lrclib](https://lrclib.net/)

-   🎶**支持多個音樂播放器**

    -   <details><summary>⚠️ NetEase Cloud Music</summary>

        -   請安裝[BetterNCM插件](https://microblock.cc/betterncm)第一的。如果安裝後降級指南彈出，請遵循指南以完成降級NetEases Cloud Music（降級為2.10.13）；
        -   之後，請在Pluginmarket中安裝Afflink插件。安裝完成後，請重新啟動NetASE Cloud Music。在這一點上，所有準備操作都已經完成，請享受！
        -   ⚠️請注意，由於插件問題，時間表存在問題

        </details>

    -   <details><summary>⚠️ Kugou Music</summary>

        -   請確保Kugou音樂設置“支持系統播放控件，例如鎖定屏幕接口”
        -   沒有廣播的時間表信息，這意味著當您更改Kugou Music中的時間軸位置時，Betterlyrics無法檢測到此更改。
        -   ⚠️請注意，由於Kugou本身，時間表有問題

        </details>

    -   <details><summary>⚠️ Apple Music</summary>

        -   確保您將時間軸閾值設置為設置約600毫秒左右（轉到“設置”  - “高級選項”以進行更改），否則，歌詞將不斷向前發展。
        -   ⚠️請注意，您需要其他設置才能阻止搖晃歌詞（有關更多信息，請參見本文檔末尾的常見問題解答）

        </details>

    -   <details><summary>⚠️ foobar2000</summary>

        -   確保你有<https://github.com/dumbie/foo_mediacontrol>與之安裝
        -   請注意，由於插件問題，時間表存在問題

        </details>

    -   Spotify

    -   QQ音樂

    -   Potplayer

    -   媒體播放器（系統）

    -   <details><summary>LX Music</summary>

        -   請確保您在LX音樂設置頁面中啟用了“打開API”
        -   然後打開更好的瀏覽器，轉到設置，轉到“高級選項”，輸入您的LX音樂服務器地址（主要是喜歡<http://127.0.0.1:23330>）然後你去！

        </details>

    -   <details><summary>MusicBee</summary>

        -   請安裝<https://github.com/HenryPDT/mb_MediaControl>使用之前

        </details>

    -   <details><summary>iTunes</summary>

        -   請安裝<https://github.com/thewizrd/iTunes-SMTC>使用之前

        </details>

    -   <details><summary>AIMP</summary>

        -   請安裝<https://www.aimp.ru/?do=catalog&rec_id=1097>使用之前

        </details>

-   🪟**多個顯示模式**
    -   **標準模式**
        -   享受帶有豐富歌詞動畫和充滿活力的背景的沉浸式聽力旅程
    -   **碼頭模式**
        -   一個聰明的動畫歌詞酒吧停靠在您的屏幕邊緣
    -   **桌面模式**
        -   享受漂浮在應用上方的身臨其境的歌詞

-   🧠**聰明的行為**
    -   音樂停頓時自動隱藏

> 該項目仍在開發中，最新分支可能存在錯誤和意外行為。

## 屏幕截圖

### 標準模式

![alt text](Screenshots/image.png)

![alt text](Screenshots/glow-float.gif)

![alt text](Screenshots/fan.png)

![alt text](Screenshots/lyrics-only.png)

![alt text](Screenshots/album-art-only.png)

### 碼頭模式

![alt text](Screenshots/dock-1.png)

![alt text](Screenshots/dock-2.png)

### 桌面模式

![alt text](Screenshots/desktop-1.png)

![alt text](Screenshots/desktop-2.png)

## 示範

在比利比利觀看我們的介紹視頻（於2025年7月7日上傳）[這裡](https://www.bilibili.com/video/BV1zjGjzfEXh).

## 現在嘗試

### Microsoft Store

<a href="https://apps.microsoft.com/detail/9P1WCD1P597R?referrer=appbadge&mode=direct">
	<img src="https://get.microsoft.com/images/en-us%20dark.svg" width="200"/>
</a>

**最簡單**獲取它的方法。**無限**免費步道或購買（有**沒有區別**在免費版本和付費版本之間）

☕如果您覺得有用，請考慮購買🧧**Microsoft Store**，我會感謝它！ 🥰

> 當建立穩定版本時，Microsoft Store將是第一個更新的頻道。

### Google Drive

或從Google Drive獲取（請參閱[發布](https://github.com/jayfunc/BetterLyrics/releases)鏈接的頁面）

> 請注意，您正在下載“ .zip”文件，以獲取有關如何安裝的指南[這個文檔](How2Install/How2Install.md).

## 💖非常感謝

-   [抒情式 - 萊errics-helper](https://github.com/WXRIW/Lyricify-Lyrics-Helper)
    -   為QQ，NetEase，Kugou來源提供歌詞獲取，解密和解析
-   [lrclib](https://github.com/tranxuanthang/lrclib)
    -   LRCLIB歌詞API提供商
-   [.NET的音頻工具庫（ATL）](https://github.com/Zeugma440/atldotnet)
    -   用於在音樂文件中提取圖片
-   [winuiex](https://github.com/dotMorten/WinUIEx)
    -   提供簡單的方法來訪問Win32 API有關窗口
-   [taglib＃](https://github.com/mono/taglib-sharp)
    -   用於閱讀原始歌詞內容
-   [老式](https://github.com/dahall/Vanara)
    -   Win32 API包裝器
-   [librenslate](https://github.com/LibreTranslate/LibreTranslate)
    -   提供離線歌詞翻譯的能力
-   [stackoverflow-如何在WPF中使用Anim Anim Anim Anim Anim Anim Anive Margin屬性](https://stackoverflow.com/a/21542882/11048731)
-   [揭示](https://github.com/ghost1372/DevWinUI)
-   [Bilibili -【WinUI3】SystemBackdropController：定義雲母、亞克力效果](https://www.bilibili.com/video/BV1PY4FevEkS)
-   [cnblogs - .NET App 與 Windows 系統媒體控制(SMTC)交互](https://www.cnblogs.com/TwilightLemon/p/18279496)
-   [Win2D 中的遊戲循環：CanvasAnimatedControl](https://www.cnblogs.com/walterlv/p/10236395.html)
-   [R2D2RIGO/WIN2D-SPALES](https://github.com/r2d2rigo/Win2D-Samples/blob/master/IrisBlurWin2D/IrisBlurWin2D/MainPage.xaml.cs)
-   [CommunityToolkit - 從入門到精通](https://mvvm.coldwind.top/)

## 受到啟發

-   [改進的網狀網絡](https://github.com/solstice23/refined-now-playing-netease)
-   [Lyricify-App](https://github.com/WXRIW/Lyricify-App)
-   [椒鹽音樂 Salt Player](https://moriafly.com/program/salt-player)
-   [mytoolbar](https://github.com/TwilightLemon/MyToolBar)

## ✍️幫助我們轉化為您的語言

找不到您的語言？
不用擔心！開始翻譯並成為貢獻者之一！ 😆
單擊[關聯](https://crowdin.com/project/betterlyrics/invite?h=d767e4f2dbd832d8fcdb6f7e5a198b402502866)現在通過Crowdin將此應用轉換為您的語言！

## 星曆史

[![](https://api.star-history.com/svg?repos=jayfunc/BetterLyrics&type=Date)](https://www.star-history.com/#jayfunc/BetterLyrics&Date)

## 任何問題和公關都受到歡迎

如果您找到錯誤，請在問題中提交錯誤，或者您有任何想法可以隨時在這里分享。

* * *

## FAQ

### 我在碼頭模式下看不到任何按鈕

重要的是要注意，當您輸入“停靠模式”時，操作按鈕就會隱藏。將鼠標懸停在頂部，以訪問“浸入”，“更多”和“關閉”按鈕。

![alt text](FAQ/image-10.png)

將鼠標懸停在窗戶底部邊緣上方，以顯示底部的白色控制浮動窗口

![alt text](FAQ/image-11.png)

點擊“小白色欄”以浮動窗口形式顯示底部控制欄（包括當前的播放進度視圖，時間表偏移調整；先前的歌曲，暫停/播放，下一首歌；翻譯，佈局，設置）

![alt text](FAQ/image-12.png)

### 如何在桌面模式下鎖定窗口

![alt text](FAQ/image-6.png)

將鼠標懸停在頂部，單擊鎖定圖標，您可以走！或者，請按`Ctrl + Alt + U`.

### 如何在桌面模式下解鎖窗口

![alt text](FAQ/image-7.png)

它在系統托盤中，右鍵單擊圖標，您會看到“解鎖窗口”。或者，請按`Ctrl + Alt + U`.

### 歌詞時間表延遲

將鼠標懸停在應用程序的底部，

![alt text](FAQ/image.png)

然後單擊第一個圖標按鈕（歌詞時間線偏移），在這裡您可以自由調整偏移量。

### 歌詞經常來回跳動（例如，蘋果音樂）

![alt text](FAQ/image-2.png)

轉到“高級選項”部分，增加閾值值（用較大的紅色矩形標記），直到歌詞正常工作。
