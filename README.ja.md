[_よくある質問（FAQ）はこちらをクリック_](#faq)

![](Promotion/banner.png)

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
WinUI 3とWin2Dで構築された動的歌詞表示ツール — ローカル再生や他のプレーヤーにも対応
</h3>

## 🎉 本プロジェクトはSSPAIで特集されました！

記事はこちら：[BetterLyrics – Windows向けの没入型で滑らかな歌詞表示ツール](https://sspai.com/post/101028)

## 🔈 フィードバック・チャットグループ

- [<img src="BetterLyrics.WinUI3\BetterLyrics.WinUI3\Assets\QQ.png" height="20"> QQ](https://qun.qq.com/universal-share/share?ac=1&authKey=4Q%2BYTq3wZldYpF5SbS5c19ECFsiYoLZFAIcBNNzYpBUtiEjaZ8sZ%2F%2BnFN0qw3lad&busi_data=eyJncm91cENvZGUiOiIxMDU0NzAwMzg4IiwidG9rZW4iOiJiVnhqemVYN0N5QVc3b1ZkR24wWmZOTUtvUkJoWm1JRWlaWW5iZnlBcXJtZUtGc2FFTHNlUlFZMi9iRm03cWF5IiwidWluIjoiMTM5NTczOTY2MCJ9&data=39UmAihyH_o6CZaOs7nk2mO_lz2ruODoDou6pxxh7utcxP4WF5sbDBDOPvZ_Wqfzeey4441anegsLYQJxkrBAA&svctype=4&tempid=h5_group_info) (1054700388)
- [<img src="BetterLyrics.WinUI3\BetterLyrics.WinUI3\Assets\Discord.png" height="12"> Discord](https://discord.gg/5yAQPnyCKv)
- [<img src="BetterLyrics.WinUI3\BetterLyrics.WinUI3\Assets\Telegram.png" height="16"> Telegram](https://t.me/+svhSLZ7awPsxNGY1)

## 🌟 主な特徴

- 🌠 **美しいユーザーインターフェース**
  - 滑らかなアニメーションとエフェクト
- ↔️ **強力な歌詞翻訳**
  - オフライン機械翻訳（30言語対応）
  - ローカル歌詞ファイルの埋め込み翻訳を自動読み取り
- 🧩 **多様な歌詞ソース**
  - ローカルストレージ
    - 音楽ファイル（埋め込み歌詞付き）
    - [.lrc](<https://en.wikipedia.org/wiki/LRC_(file_format)>) ファイル（標準・拡張フォーマット対応）
    - [.eslrc](https://github.com/ESLyric/release) ファイル
    - [.ttml](https://en.wikipedia.org/wiki/Timed_Text_Markup_Language) ファイル
  - オンライン歌詞プロバイダー
    - QQ Music
    - NetEase Cloud Music（网易云音乐）
    - Kugou Music（酷狗音乐）
    - [amll-ttml-db](https://github.com/Steve-xmh/amll-ttml-db)
    - [LRCLIB](https://lrclib.net/)
- 🎶 **複数の音楽プレーヤーに対応**

  - <details><summary>⚠️ NetEase Cloud Music</summary>

    - まず[BetterNCMプラグイン](https://microblock.cc/betterncm)をインストールしてください。インストール後にダウングレード案内が表示された場合は、案内に従いNetEase Cloud Musicを2.10.13にダウングレードしてください。
    - その後、PluginMarketでInfLinkプラグインをインストールし、NetEase Cloud Musicを再起動してください。
    - ⚠️ プラグインの問題によりタイムラインに不具合がある場合があります

    </details>

  - <details><summary>⚠️ Kugou Music</summary>

    - Kugou Musicの設定で「システム再生コントロール（ロック画面など）をサポート」を有効にしてください
    - Kugou Musicはタイムライン情報を送信しないため、再生位置を変更してもBetterLyricsは検出できません
    - ⚠️ タイムラインの問題はKugou自体の制限です

    </details>

  - <details><summary>⚠️ Apple Music</summary>

    - 設定の「詳細オプション」でタイムラインしきい値を約600msに設定してください。そうしないと歌詞が前後に揺れ続けます。
    - ⚠️ 歌詞の揺れを防ぐには追加設定が必要です（詳細は末尾FAQ参照）

    </details>

  - <details><summary>⚠️ foobar2000</summary>

    - https://github.com/dumbie/foo_mediacontrol をインストールしてください
    - ⚠️ プラグインの問題によりタイムラインに不具合がある場合があります

    </details>

  - Spotify
  - QQ Music
  - PotPlayer
  - メディアプレーヤー（システム）

  - <details><summary>LX Music</summary>

    - LX Musicの設定ページで「Open API」を有効にしてください
    - BetterLyricsの設定→詳細オプションでLX Musicサーバーアドレス（通常は http://127.0.0.1:23330）を入力してください

    </details>

  - <details><summary>MusicBee</summary>

    - https://github.com/HenryPDT/mb_MediaControl をインストールしてください

    </details>

  - <details><summary>iTunes</summary>

    - https://github.com/thewizrd/iTunes-SMTC をインストールしてください

    </details>

  - <details><summary>AIMP</summary>

    - https://www.aimp.ru/?do=catalog&rec_id=1097 をインストールしてください

    </details>

- 🪟 **多様な表示モード**
  - **標準モード**
    - 没入感のある歌詞アニメーションと動的背景
  - **ドックモード**
    - 画面端に固定されるスマートな歌詞バー
  - **デスクトップモード**
    - アプリの上に歌詞をフロート表示
- 🧠 **スマートな動作**
  - 音楽が一時停止すると自動的に非表示

> 本プロジェクトは開発中です。最新ブランチにはバグや予期しない動作が含まれる場合があります。

## スクリーンショット

### 標準モード

![alt text](Screenshots/image.png)

![alt text](Screenshots/glow-float.gif)

![alt text](Screenshots/fan.png)

![alt text](Screenshots/lyrics-only.png)

![alt text](Screenshots/album-art-only.png)

### ドックモード

![alt text](Screenshots/dock-1.png)

![alt text](Screenshots/dock-2.png)

### デスクトップモード

![alt text](Screenshots/desktop-1.png)

![alt text](Screenshots/desktop-2.png)

## デモ

Bilibiliで紹介動画を見る（2025年8月18日アップロード）：[こちら](https://www.bilibili.com/video/BV1yLYtzQEME/)

## 今すぐ試す

### Microsoft Store

<a href="https://apps.microsoft.com/detail/9P1WCD1P597R?referrer=appbadge&mode=direct">
	<img src="https://get.microsoft.com/images/en-us%20dark.svg" width="200"/>
</a>

**最も簡単**な入手方法。**無制限**の無料トライアルまたは購入（無料版と有料版の違いはありません）

☕ 役に立ったら、ぜひ **Microsoft Store** でご購入・ご支援ください！🥰

> 安定版がリリースされると、Microsoft Storeが最初に更新されます。

### Google Drive

Google Driveからも入手可能です（[リリース](https://github.com/jayfunc/BetterLyrics/releases)ページ参照）

> ダウンロードするのは「.zip」ファイルです。インストール方法は[こちらのドキュメント](How2Install/How2Install.md)をご参照ください。

## 💖 特別感謝

- [Lyricify-Lyrics-Helper](https://github.com/WXRIW/Lyricify-Lyrics-Helper)
  - QQ、NetEase、Kugouの歌詞取得・復号・解析を提供
- [lrclib](https://github.com/tranxuanthang/lrclib)
  - LRCLIB歌詞APIプロバイダー
- [Audio Tools Library (ATL) for .NET](https://github.com/Zeugma440/atldotnet)
  - 音楽ファイルから画像抽出に使用
- [WinUIEx](https://github.com/dotMorten/WinUIEx)
  - Win32ウィンドウAPIへの簡単なアクセスを提供
- [TagLib#](https://github.com/mono/taglib-sharp)
  - 元の歌詞内容の読み取りに使用
- [Vanara](https://github.com/dahall/Vanara)
  - Win32 APIラッパー
- [LibreTranslate](https://github.com/LibreTranslate/LibreTranslate)
  - オフライン歌詞翻訳機能を提供
- [Stackoverflow - WPFでMarginプロパティをアニメーション化する方法](https://stackoverflow.com/a/21542882/11048731)
- [DevWinUI](https://github.com/ghost1372/DevWinUI)
- [Bilibili -【WinUI3】SystemBackdropController：雲母・アクリル効果の定義](https://www.bilibili.com/video/BV1PY4FevEkS)
- [cnblogs - .NETアプリとWindowsシステムメディアコントロール(SMTC)の連携](https://www.cnblogs.com/TwilightLemon/p/18279496)
- [Win2Dのゲームループ：CanvasAnimatedControl](https://www.cnblogs.com/walterlv/p/10236395.html)
- [r2d2rigo/Win2D-Samples](https://github.com/r2d2rigo/Win2D-Samples/blob/master/IrisBlurWin2D/IrisBlurWin2D/MainPage.xaml.cs)
- [CommunityToolkit - 入門から上級まで](https://mvvm.coldwind.top/)

## インスピレーションを受けたプロジェクト

- [refined-now-playing-netease](https://github.com/solstice23/refined-now-playing-netease)
- [Lyricify-App](https://github.com/WXRIW/Lyricify-App)
- [塩音楽 Salt Player](https://moriafly.com/program/salt-player)
- [MyToolBar](https://github.com/TwilightLemon/MyToolBar)

## ✍️ 翻訳にご協力ください

ご希望の言語が見つかりませんか？
ご安心ください！翻訳に参加してコントリビューターになりましょう！😆
[こちらのリンク](https://crowdin.com/project/betterlyrics/invite?h=d767e4f2dbd832d8fcdb6f7e5a198b402502866)からCrowdinで翻訳にご参加いただけます。

## Star履歴

[![](https://api.star-history.com/svg?repos=jayfunc/BetterLyrics&type=Date)](https://www.star-history.com/#jayfunc/BetterLyrics&Date)

## 不具合・PR歓迎

バグを見つけた場合はissuesでご報告ください。アイデアもお気軽にお寄せください。

---

## FAQ

### ドックモードでボタンが見えない

「ドックモード」に入ると操作ボタンは非表示になります。ウィンドウ上部にマウスを重ねると「没入」「その他」「閉じる」ボタンが表示されます。

![alt text](FAQ/image-10.png)

ウィンドウ下端の少し上にマウスを重ねると、下部に白いコントロールフローティングウィンドウが表示されます

![alt text](FAQ/image-11.png)

「小さな白いバー」をクリックすると、下部のフローティングコントロールバー（再生進行状況、タイムラインオフセット調整、前の曲/一時停止/次の曲、翻訳、レイアウト、設定）が表示されます

![alt text](FAQ/image-12.png)

### デスクトップモードでウィンドウをロックする方法

![alt text](FAQ/image-6.png)

上部にマウスを重ねてロックアイコンをクリック、または `Ctrl + Alt + U` を押してください。

### デスクトップモードでウィンドウのロックを解除する方法

![alt text](FAQ/image-7.png)

システムトレイのアイコンを右クリックし、「ウィンドウのロック解除」を選択、または `Ctrl + Alt + U` を押してください。

### 歌詞のタイムラインに遅延がある

アプリの一番下にマウスを重ねてください。

![alt text](FAQ/image.png)

最初のアイコンボタン（歌詞タイムラインオフセット）をクリックすると、オフセットを自由に調整できます。

### 歌詞が頻繁に前後にジャンプする（例：Apple Music）

![alt text](FAQ/image-2.png)

「詳細オプション」セクションでしきい値（赤い四角でマーク）を上げると、歌詞が正常に動作します。
