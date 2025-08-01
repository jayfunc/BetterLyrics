<div style="text-align: center;">

[cullyよくある質問（FAQ）を表示するには、ここをクリックしてください](#faq)

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

## 🎉このプロジェクトはSSPAIによって紹介されました！

記事をご覧ください：[より良いことをする - ウィンドウ用に設計された没入型で滑らかな歌詞ディスプレイツール](https://sspai.com/post/101028)

## 🔈フィードバックとチャットグループ

-   [<img src="BetterLyrics.WinUI3\BetterLyrics.WinUI3\Assets\QQ.png" height="20">QQ](https://qun.qq.com/universal-share/share?ac=1&authKey=4Q%2BYTq3wZldYpF5SbS5c19ECFsiYoLZFAIcBNNzYpBUtiEjaZ8sZ%2F%2BnFN0qw3lad&busi_data=eyJncm91cENvZGUiOiIxMDU0NzAwMzg4IiwidG9rZW4iOiJiVnhqemVYN0N5QVc3b1ZkR24wWmZOTUtvUkJoWm1JRWlaWW5iZnlBcXJtZUtGc2FFTHNlUlFZMi9iRm03cWF5IiwidWluIjoiMTM5NTczOTY2MCJ9&data=39UmAihyH_o6CZaOs7nk2mO_lz2ruODoDou6pxxh7utcxP4WF5sbDBDOPvZ_Wqfzeey4441anegsLYQJxkrBAA&svctype=4&tempid=h5_group_info)(1054700388)
-   [<img src="BetterLyrics.WinUI3\BetterLyrics.WinUI3\Assets\Discord.png" height="12">不和](https://discord.gg/5yAQPnyCKv)
-   [<img src="BetterLyrics.WinUI3\BetterLyrics.WinUI3\Assets\Telegram.png" height="16">電報](https://t.me/+svhSLZ7awPsxNGY1)

## hidhight盛な機能

-   🌠**心地よいユーザーインターフェイス**
    -   流fluentアニメーションとエフェクト

-   ↔️**強い歌詞翻訳**
    -   オフラインの機械翻訳（30言語をサポート）
    -   埋め込まれた翻訳のための自動読み取り地元の歌詞ファイル

-   🧩**さまざまな歌詞ソース**
    -   ローカルストレージ
        -   音楽ファイル（埋め込まれた歌詞付き）
        -   [.lrc](https://en.wikipedia.org/wiki/LRC_(file_format))ファイル（コア形式と拡張形式の両方）
        -   [.eslrc](https://github.com/ESLyric/release)ファイル
        -   [.ttml](https://en.wikipedia.org/wiki/Timed_Text_Markup_Language)ファイル
    -   オンライン歌詞プロバイダー
        -   QQ音楽
        -   NetEase Cloud Music NetEase Cloud Music
        -   クゴウ音楽
        -   [amll-ttml-db](https://github.com/Steve-xmh/amll-ttml-db)
        -   [lrclib](https://lrclib.net/)

-   🎶**複数の音楽プレーヤーがサポートされています**

    -   <details><summary>⚠️ NetEase Cloud Music</summary>

        -   インストールしてください[BetterNCMプラグイン](https://microblock.cc/betterncm)初め。インストール後にダウングレードガイドがポップアップした場合は、ガイドに従ってNetEase Cloud Musicのダウングレードを完了してください（2.10.13にダウングレード）。
        -   その後、プラグインマーケットにflinkプラグインをインストールしてください。インストールが完了したら、NetEase Cloud Musicを再起動してください。この時点で、すべての準備操作が完了しました、それを楽しんでください！
        -   pluginの問題によりタイムラインに問題があることに注意してください

        </details>

    -   <details><summary>⚠️ Kugou Music</summary>

        -   クゴウ音楽の設定「ロック画面インターフェイスなどのサポートシステムの再生コントロール」がオンになっていることを確認してください
        -   タイムライン情報は放送されていません。つまり、クゴウ音楽のタイムラインポジションを変更すると、この変更を検出する方法はありません。
        -   ⚠️クゴウ自体のためにタイムラインに問題があることに注意してください

        </details>

    -   <details><summary>⚠️ Apple Music</summary>

        -   タイムラインのしきい値を設定で約600ミリ秒に設定していることを確認してください（「設定」 - 「高度なオプション」に変更するには）。そうしないと、歌詞は常に前進します。
        -   shaking shaking歌詞を見るのをやめるには、追加の設定が必要であることに注意してください（詳細については、このドキュメントの最後のFAQを参照してください）

        </details>

    -   <details><summary>⚠️ foobar2000</summary>

        -   あなたが持っていることを確認してください<https://github.com/dumbie/foo_mediacontrol>それでインストールされています
        -   pluginの問題によりタイムラインに問題があることに注意してください

        </details>

    -   Spotify

    -   QQ音楽

    -   ポットプレイヤー

    -   メディアプレーヤー（システム）

    -   <details><summary>LX Music</summary>

        -   LX Music Settingsページで「Open API」を有効にしていることを確認してください
        -   次に、より良くなることを開き、設定に移動し、「Advanced Options」に移動し、LX Music Serverアドレスを入力します（ほとんどのように<http://127.0.0.1:23330>）そしてあなたはそこに行きます！

        </details>

    -   <details><summary>MusicBee</summary>

        -   インストールしてください<https://github.com/HenryPDT/mb_MediaControl>使用する前に

        </details>

    -   <details><summary>iTunes</summary>

        -   インストールしてください<https://github.com/thewizrd/iTunes-SMTC>使用する前に

        </details>

    -   <details><summary>AIMP</summary>

        -   インストールしてください<https://www.aimp.ru/?do=catalog&rec_id=1097>使用する前に

        </details>

-   🪟**複数の表示モード**
    -   **標準モード**
        -   豊かな歌詞のアニメーションと美しくダイナミックな背景を備えた没入型のリスニングの旅をお楽しみください
    -   **ドックモード**
        -   スクリーンエッジにドッキングされたスマートなアニメーション歌詞バー
    -   **デスクトップモード**
        -   アプリの上に浮かぶ没入型の歌詞をお楽しみください

-   🧠**スマートな行動**
    -   音楽が一時停止したときに自動隠す

> このプロジェクトはまだ開発中であり、最新の支店にはバグと予期しない行動が存在する可能性があります。

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

## デモンストレーション

Bilibiliではじめにビデオ（2025年7月7日にアップロード）をご覧ください[ここ](https://www.bilibili.com/video/BV1zjGjzfEXh).

## 今すぐ試してみてください

### マイクロソフトストア

<a href="https://apps.microsoft.com/detail/9P1WCD1P597R?referrer=appbadge&mode=direct">
	<img src="https://get.microsoft.com/images/en-us%20dark.svg" width="200"/>
</a>

**最も簡単です**それを手に入れる方法。**無制限**無料のトレイルまたは購入（あります**違いはありません**無料版と有料版の間）

☕便利だと思う場合は、購入することを検討してください🧧**マイクロソフトストア**、感謝します！ 🥰

> 安定したバージョンが構築されると、Microsoft Storeが更新された最初のチャンネルになります。

### Googleドライブ

または、Googleドライブから入手してください（参照してください[リリース](https://github.com/jayfunc/BetterLyrics/releases)リンクのページ）

> 「.zip」ファイルをダウンロードしていることに注意してください。インストール方法については、親切にフォローしてください[このドキュメント](How2Install/How2Install.md).

## 💖ありがとう

-   [Lyricify-Lyrics-Helper](https://github.com/WXRIW/Lyricify-Lyrics-Helper)
    -   QQ、NetEase、Kugouソースの歌詞フェッチ、復号化、解析を提供する
-   [lrclib](https://github.com/tranxuanthang/lrclib)
    -   lrclib歌詞APIプロバイダー
-   [.NET用のオーディオツールライブラリ（ATL）](https://github.com/Zeugma440/atldotnet)
    -   音楽ファイルに写真を抽出するために使用されます
-   [winuiex](https://github.com/dotMorten/WinUIEx)
    -   ウィンドウィングに関するWin32 APIに簡単にアクセスする方法を提供します
-   [タグリブ＃](https://github.com/mono/taglib-sharp)
    -   オリジナルの歌詞コンテンツを読むために使用されます
-   [古いもの](https://github.com/dahall/Vanara)
    -   Win32 APIラッパー
-   [リブレットランスレート](https://github.com/LibreTranslate/LibreTranslate)
    -   オフラインの歌詞翻訳の機能を提供します
-   [StackOverFlow -WPFでマージンプロパティをアニメーション化する方法](https://stackoverflow.com/a/21542882/11048731)
-   [明らかにする](https://github.com/ghost1372/DevWinUI)
-   [bilibili -【winui3】SystemBackDropController：MICAおよびアクリル効果を定義します](https://www.bilibili.com/video/BV1PY4FevEkS)
-   [CNBLOGS -.NETアプリはWindowsシステムメディアコントロール（SMTC）と対話します](https://www.cnblogs.com/TwilightLemon/p/18279496)
-   [Win2dのゲームループ：CanvasanimatedControl](https://www.cnblogs.com/walterlv/p/10236395.html)
-   [R2D2RIGO/Win2D-Samples](https://github.com/r2d2rigo/Win2D-Samples/blob/master/IrisBlurWin2D/IrisBlurWin2D/MainPage.xaml.cs)
-   [communitytoolkit-初心者から習得まで](https://mvvm.coldwind.top/)

## に触発された

-   [洗練されたnowing-netease](https://github.com/solstice23/refined-now-playing-netease)
-   [lyricify-app](https://github.com/WXRIW/Lyricify-App)
-   [ソルトプレーヤー](https://moriafly.com/program/salt-player)
-   [mytoolbar](https://github.com/TwilightLemon/MyToolBar)

## ✍唱私たちがあなたの言語に翻訳するのを手伝ってください

あなたの言語が見つかりませんか？
心配しないで！翻訳を開始して、貢献者の1人になりましょう！ 😆
クリックします[リンク](https://crowdin.com/project/betterlyrics/invite?h=d767e4f2dbd832d8fcdb6f7e5a198b402502866)今すぐCrowdinを介してこのアプリをあなたの言語に翻訳するために！

## 星の歴史

[![](https://api.star-history.com/svg?repos=jayfunc/BetterLyrics&type=Date)](https://www.star-history.com/#jayfunc/BetterLyrics&Date)

## 問題やPRを歓迎します

バグが見つかった場合は、問題で提出してください。または、アイデアがある場合は、ここでお気軽に共有してください。

* * *

## よくある質問

### ドックモードでボタンが表示されませんでした

「ドッキングモード」を入力すると、アクションボタンが非表示になることに注意することが重要です。マウスを上部にホバリングして、「浸す」、「その他」、「閉じる」ボタンにアクセスします。

![alt text](FAQ/image-10.png)

窓の下端の少し上にマウスをホバリングして、下部に白いコントロールフローティングウィンドウを表示します

![alt text](FAQ/image-11.png)

「Little White Bar」をタップして、フローティングウィンドウ形式のボトムコントロールバーを表示します（現在の再生プログレスビュー、タイムラインオフセット調整、前の曲、Pause/Play、次の曲、翻訳、レイアウト、設定を含む）

![alt text](FAQ/image-12.png)

### ウィンドウをデスクトップモードでロックするにはどうすればよいですか

![alt text](FAQ/image-6.png)

上部にマウスを置き、ロックアイコンをクリックすると、行ってもいいです！または、または押します`Ctrl + Alt + U`.

### デスクトップモードでウィンドウのロックを解除するにはどうすればよいですか

![alt text](FAQ/image-7.png)

システムトレイにあり、アイコンを右クリックすると、「ウィンドウのロックを解除する」が表示されます。または、または押します`Ctrl + Alt + U`.

### 歌詞のタイムラインに遅延があります

アプリの最下部にマウスを置く、

![alt text](FAQ/image.png)

次に、最初のアイコンボタン（歌詞タイムラインオフセット）をクリックします。ここでは、オフセットを自由に調整できます。

### 歌詞は頻繁にジャンプします（例：Apple Music）

![alt text](FAQ/image-2.png)

「Advanced Options」セクションに移動し、歌詞が適切に機能するまで、しきい値（より大きな赤い長方形でマークされた）を増やします。
