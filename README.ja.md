�?<div style="text-align: center;">

[�? よくある�?問（FAQ）はこちらを�?�?ック](#faq)

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
WinUI 3とWin2Dで�?�築された動的歌詞表示ツール �? �?ーカ�?再生や他�?プレーヤーにも�?�応
</h3>

## 🎉 �?プロジェ�?トはSSPAIで特集されました�?

記事�?こちら：[BetterLyrics �? Windows向け�?没入型で滑らかな歌�?�表示ツール](https://sspai.com/post/101028)

## 🔈 フィードバッ�?・チャットグ�?ープ

- [<img src="BetterLyrics.WinUI3\BetterLyrics.WinUI3\Assets\QQ.png" height="20"> QQ](https://qun.qq.com/universal-share/share?ac=1&authKey=4Q%2BYTq3wZldYpF5SbS5c19ECFsiYoLZFAIcBNNzYpBUtiEjaZ8sZ%2F%2BnFN0qw3lad&busi_data=eyJncm91cENvZGUiOiIxMDU0NzAwMzg4IiwidG9rZW4iOiJiVnhqemVYN0N5QVc3b1ZkR24wWmZOTUtvUkJoWm1JRWlaWW5iZnlBcXJtZUtGc2FFTHNlUlFZMi9iRm03cWF5IiwidWluIjoiMTM5NTczOTY2MCJ9&data=39UmAihyH_o6CZaOs7nk2mO_lz2ruODoDou6pxxh7utcxP4WF5sbDBDOPvZ_Wqfzeey4441anegsLYQJxkrBAA&svctype=4&tempid=h5_group_info) (1054700388)
- [<img src="BetterLyrics.WinUI3\BetterLyrics.WinUI3\Assets\Discord.png" height="12"> Discord](https://discord.gg/5yAQPnyCKv)
- [<img src="BetterLyrics.WinUI3\BetterLyrics.WinUI3\Assets\Telegram.png" height="16"> Telegram](https://t.me/+svhSLZ7awPsxNGY1)

## 🌟 主な特徴

- 🌠 **美しいユーザーインターフェー�?**
  - 滑らかな�?ニメーションとエフェ�?�?
- ↔️ **強力�?歌�?�翻�?**
  - �?フライン機�?�翻訳（30言語�?�応�?
  - �?ーカ�?歌�?�ファイ�?�?埋め込み翻訳を自動�??み取�?
- 🧩 **多�?�な歌�?�ソース**
  - �?ーカ�?スト�?ージ
    - 音楽ファイル（埋め込み歌詞付き）
    - [.lrc](<https://en.wikipedia.org/wiki/LRC_(file_format)>) ファイル（�?�準・拡張フォーマット�?�応�?
    - [.eslrc](https://github.com/ESLyric/release) ファイル
    - [.ttml](https://en.wikipedia.org/wiki/Timed_Text_Markup_Language) ファイル
  - �?ンライン歌�?�プ�?バイダ�?
    - QQ Music
    - NetEase Cloud Music（网易云音乐�?
    - Kugou Music（酷狗音乐）
    - [amll-ttml-db](https://github.com/Steve-xmh/amll-ttml-db)
    - [LRCLIB](https://lrclib.net/)
- 🎶 **複数�?音楽プレーヤーに対応**

  - <details><summary>⚠️ NetEase Cloud Music</summary>

    - まず[BetterNCMプラグイン](https://microblock.cc/betterncm)をインストー�?してください。インストー�?後にダウングレード案内が表示された場合�?、�?�内�?従いNetEase Cloud Music�?2.10.13�?ダウングレードしてください�?
    - その後、PluginMarket�?InfLinkプラグインをインストールし、NetEase Cloud Musicを再起動してください�?
    - ⚠️ プラグインの問�?�によりタイムライン�?不具合がある場合がありま�?

    </details>

  - <details><summary>⚠️ Kugou Music</summary>

    - Kugou Music�?�?定で「システム再生コント�?ール（ロック画面�?ど）をサポート」を有効�?してください
    - Kugou Music�?タイムライン情報を送信しないため、再生位�?を�?�更してもBetterLyrics�?検出できませ�?
    - ⚠️ タイムライン�?問�?�はKugou�?体の制限です

    </details>

  - <details><summary>⚠️ Apple Music</summary>

    - �?定の「詳細オプション」でタイムラインしきい値を�?600ms�?�?定してください。そうし�?いと歌�?�が前後�?揺れ続けます�?
    - ⚠️ 歌�?�の揺れを防ぐに�?追加�?定が必�?�です（詳細�?�?尾FAQ参照�?

    </details>

  - <details><summary>⚠️ foobar2000</summary>

    - https://github.com/dumbie/foo_mediacontrol をインストー�?してください
    - ⚠️ プラグインの問�?�によりタイムライン�?不具合がある場合がありま�?

    </details>

  - Spotify
  - QQ Music
  - PotPlayer
  - メディアプレーヤー（システム�?

  - <details><summary>LX Music</summary>

    - LX Music�?�?定ページで「Open API」を有効�?してください
    - BetterLyrics�?�?定→詳細�?プション�?LX Musicサーバー�?ドレス（通常�? http://127.0.0.1:23330）を入力してください

    </details>

  - <details><summary>MusicBee</summary>

    - https://github.com/HenryPDT/mb_MediaControl をインストー�?してください

    </details>

  - <details><summary>iTunes</summary>

    - https://github.com/thewizrd/iTunes-SMTC をインストー�?してください

    </details>

  - <details><summary>AIMP</summary>

    - https://www.aimp.ru/?do=catalog&rec_id=1097 をインストー�?してください

    </details>

- 🪟 **多�?�な表示�?ード**
  - **標準�?ード**
    - 没入感のある歌�?�アニメーションと動的背�?
  - **ドッ�?�?ード**
    - 画面�?�?固定されるスマートな歌�?�バ�?
  - **デス�?トップモード**
    - �?プリ�?上に歌�?�をフロート表示
- �?? **スマート�?動作**
  - 音楽が一時停�?すると自動的�?非表�?

> �?プロジェ�?トは開発�?です。最新ブランチに�?バグや予期し�?い動作が�?まれる場合があります�?

## スク�?ーンショット

### 標準�?ード

![alt text](Screenshots/image.png)

![alt text](Screenshots/glow-float.gif)

![alt text](Screenshots/fan.png)

![alt text](Screenshots/lyrics-only.png)

![alt text](Screenshots/album-art-only.png)

### ドッ�?�?ード

![alt text](Screenshots/dock-1.png)

![alt text](Screenshots/dock-2.png)

### デス�?トップモード

![alt text](Screenshots/desktop-1.png)

![alt text](Screenshots/desktop-2.png)

## デモ

Bilibiliで紹介動画を見る�?2025�?7�?7日アップ�?ード）：[こちら](https://www.bilibili.com/video/BV1zjGjzfEXh)

## 今すぐ試�?

### Microsoft Store

<a href="https://apps.microsoft.com/detail/9P1WCD1P597R?referrer=appbadge&mode=direct">
	<img src="https://get.microsoft.com/images/en-us%20dark.svg" width="200"/>
</a>

**最も簡�?**�?入手方法�?**無制�?**�?無料トライア�?また�?購入（無料版と有料版�?違い�?ありません）

�? 役に立ったら、ぜ�? **Microsoft Store** でご購入・ご�?援ください！🥰

> 安定版が�?�?ースされると、Microsoft Storeが最初に更新されます�?

### Google Drive

Google Driveからも入手可能です（[�?�?ース](https://github.com/jayfunc/BetterLyrics/releases)ページ参照）

> ダウン�?ードする�?�?�?.zip」ファイ�?です。インストー�?方法は[こちらのドキュメント](How2Install/How2Install.md)をご参照ください�?

## 💖 特別感謝

- [Lyricify-Lyrics-Helper](https://github.com/WXRIW/Lyricify-Lyrics-Helper)
  - QQ、NetEase、Kugou�?歌�?�取得・復号・解析を提供
- [lrclib](https://github.com/tranxuanthang/lrclib)
  - LRCLIB歌�?�APIプロバイダ�?
- [Audio Tools Library (ATL) for .NET](https://github.com/Zeugma440/atldotnet)
  - 音楽ファイルから画像抽出�?使用
- [WinUIEx](https://github.com/dotMorten/WinUIEx)
  - Win32ウィンド�?APIへの簡単�?�?�?セスを提�?
- [TagLib#](https://github.com/mono/taglib-sharp)
  - 元の歌�?�内容の�?み取りに使用
- [Vanara](https://github.com/dahall/Vanara)
  - Win32 APIラッパー
- [LibreTranslate](https://github.com/LibreTranslate/LibreTranslate)
  - �?フライン歌�?�翻訳�?�能を提�?
- [Stackoverflow - WPF�?Marginプロパティを�?ニメーション化する方法](https://stackoverflow.com/a/21542882/11048731)
- [DevWinUI](https://github.com/ghost1372/DevWinUI)
- [Bilibili -【WinUI3】SystemBackdropController：雲母・�?�?�?�?効果�?定義](https://www.bilibili.com/video/BV1PY4FevEkS)
- [cnblogs - .NET�?プリとWindowsシステムメディアコントロール(SMTC)�?連携](https://www.cnblogs.com/TwilightLemon/p/18279496)
- [Win2D�?ゲームループ：CanvasAnimatedControl](https://www.cnblogs.com/walterlv/p/10236395.html)
- [r2d2rigo/Win2D-Samples](https://github.com/r2d2rigo/Win2D-Samples/blob/master/IrisBlurWin2D/IrisBlurWin2D/MainPage.xaml.cs)
- [CommunityToolkit - 入門から上級まで](https://mvvm.coldwind.top/)

## インスピ�?ーションを受けたプロジェ�?�?

- [refined-now-playing-netease](https://github.com/solstice23/refined-now-playing-netease)
- [Lyricify-App](https://github.com/WXRIW/Lyricify-App)
- [塩音�? Salt Player](https://moriafly.com/program/salt-player)
- [MyToolBar](https://github.com/TwilightLemon/MyToolBar)

## ✍️ 翻訳�?ご協力くださ�?

ご希望の言語が見つかりませんか�?
ご安心ください！翻訳�?参加してコントリビューターに�?りましょう！😆
[こちらの�?ンク](https://crowdin.com/project/betterlyrics/invite?h=d767e4f2dbd832d8fcdb6f7e5a198b402502866)からCrowdinで翻訳にご参加いただけます�?

## Star履�??

[![](https://api.star-history.com/svg?repos=jayfunc/BetterLyrics&type=Date)](https://www.star-history.com/#jayfunc/BetterLyrics&Date)

## 不具合・PR歓迎

バグを�?�つけた場合はissuesでご報告ください。アイデ�?もお気軽�?お寄せください�?

---

## FAQ

### ドッ�?�?ードでボタンが�?�え�?�?

「ドック�?ード」に入ると操作ボタン�?非表示に�?ります。ウィンドウ上部�?マウスを重ねると「没入」「そ�?他」「閉じる」ボタンが表示されます�?

![alt text](FAQ/image-10.png)

ウィンドウ下�?�?少し上にマウスを重ねると、下部に白いコントロールフローティングウィンドウが表示されま�?

![alt text](FAQ/image-11.png)

「小さな白いバー」を�?�?ックすると、下部のフローティングコント�?ールバー（再生進�?�状況、タイムラインオフセット調整、前�?�?/一時停�?/次の曲、翻訳、レイアウト、設定）が表示されま�?

![alt text](FAQ/image-12.png)

### デス�?トップモードでウィンドウをロックする方法

![alt text](FAQ/image-6.png)

上部�?マウスを重ねてロック�?イコンを�?�?ック、または `Ctrl + Alt + U` を押してください�?

### デス�?トップモードでウィンドウ�?�?ックを解除する方�?

![alt text](FAQ/image-7.png)

システムトレイの�?イコンを右ク�?ックし、「ウィンドウ�?�?ック解除」を選択、または `Ctrl + Alt + U` を押してください�?

### 歌�?�のタイムライン�?遅延があ�?

�?プリ�?一�?下にマウスを重ねてください�?

![alt text](FAQ/image.png)

最初の�?イコンボタン（歌詞タイムラインオフセット）を�?�?ックすると、オフセットを自由に調整できます�?

### 歌�?�が頻繁�?前後�?ジャンプする（例：Apple Music�?

![alt text](FAQ/image-2.png)

「詳細オプション」セ�?ションでしきい値（赤い四�?�でマー�?）を上げると、歌詞が正常�?動作します�?
