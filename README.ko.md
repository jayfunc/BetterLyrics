<a href="https://github.com/jayfunc/BetterLyrics/blob/dev/FAQ/FAQ.md">_**❓ 자주 묻는 질문을 보려면 여기를 클릭하십시오 (FAQ)**_</a>

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

## 🎉이 프로젝트는 SSPAI가 소개했습니다!

기사를 확인하십시오.[Betterlyrics - Windows 용으로 설계된 몰입감 있고 부드러운 가사 디스플레이 도구](https://sspai.com/post/101028)

## 🔈 피드백 및 채팅 그룹

-   [<img src="BetterLyrics.WinUI3\BetterLyrics.WinUI3\Assets\QQ.png" height="20">QQ](https://qun.qq.com/universal-share/share?ac=1&authKey=4Q%2BYTq3wZldYpF5SbS5c19ECFsiYoLZFAIcBNNzYpBUtiEjaZ8sZ%2F%2BnFN0qw3lad&busi_data=eyJncm91cENvZGUiOiIxMDU0NzAwMzg4IiwidG9rZW4iOiJiVnhqemVYN0N5QVc3b1ZkR24wWmZOTUtvUkJoWm1JRWlaWW5iZnlBcXJtZUtGc2FFTHNlUlFZMi9iRm03cWF5IiwidWluIjoiMTM5NTczOTY2MCJ9&data=39UmAihyH_o6CZaOs7nk2mO_lz2ruODoDou6pxxh7utcxP4WF5sbDBDOPvZ_Wqfzeey4441anegsLYQJxkrBAA&svctype=4&tempid=h5_group_info)(1054700388)
-   [<img src="BetterLyrics.WinUI3\BetterLyrics.WinUI3\Assets\Discord.png" height="12">불화](https://discord.gg/5yAQPnyCKv)
-   [<img src="BetterLyrics.WinUI3\BetterLyrics.WinUI3\Assets\Telegram.png" height="16">전보](https://t.me/+svhSLZ7awPsxNGY1)

## 🌟 강조 된 기능

-   🌠**유쾌한 사용자 인터페이스**
    -   유창한 애니메이션과 효과

-   ↔️**강한 가사 번역**
    -   오프라인 기계 번역 (30 개 언어 지원)
    -   내장 번역을위한 자동 읽기 로컬 가사 파일

-   🧩**다양한 가사 소스**
    -   로컬 스토리지
        -   음악 파일 (포함 된 가사 포함)
        -   [.lrc](https://en.wikipedia.org/wiki/LRC_(file_format))파일 (핵심 형식 및 향상된 형식 모두)
        -   [.eslrc](https://github.com/ESLyric/release)파일
        -   [.ttml](https://en.wikipedia.org/wiki/Timed_Text_Markup_Language)파일
    -   온라인 가사 제공 업체
        -   QQ 음악
        -   Netease Cloud Music Netease Cloud Music
        -   쿠구 음악
        -   [AMLL-TTML-DB](https://github.com/Steve-xmh/amll-ttml-db)
        -   [lrclib](https://lrclib.net/)

-   🎶**여러 음악 플레이어가 지원했습니다**

    -   <details><summary>NetEase Cloud Music</summary>

        -   설치하십시오[더 나은 플러그인](https://microblock.cc/betterncm)첫 번째. 설치 후 다운 그레이드 가이드가 나타나면 안내서를 따라 Netease Cloud Music의 다운 그레이드를 완료하십시오 (다운 그레이드 2.10.13);
        -   그런 다음 플러그인 마켓에 link 링크 플러그인을 설치하십시오. 설치가 완료되면 Netease Cloud Music을 다시 시작하십시오. 이 시점에서 모든 준비 작업이 완료되었으며 즐기십시오!

        </details>

    -   <details><summary>Kugou Music</summary>

        -   Kugou Music 설정 "Lock Screen Interface와 같은 시스템 재생 컨트롤 지원"이 켜져 있는지 확인하십시오.
        -   타임 라인 정보가 방송되지 않으므로 Kugou Music의 타임 라인 위치를 변경할 때 Beterlyrics는 이러한 변화를 감지 할 방법이 없습니다.

        </details>

    -   <details><summary>Apple Music</summary>

        -   설정에서 타임 라인 임계 값을 약 600ms로 설정했는지 확인하십시오 ( "설정" - "고급 옵션"으로 이동). 그렇지 않으면 가사가 앞으로 계속 진행됩니다.

        </details>

    -   <details><summary>foobar2000</summary>

        -   당신이 가지고 있는지 확인하십시오<https://github.com/dumbie/foo_mediacontrol>그것으로 설치

        </details>

    -   Spotify

    -   QQ 음악

    -   포트 플레이어

    -   미디어 플레이어 (시스템)

    -   <details><summary>LX Music</summary>

        -   LX Music 설정 페이지에서 "API 열기"를 활성화했는지 확인하십시오.
        -   그런 다음 더 나은 문학을 열고, 설정으로 이동하고, "고급 옵션"으로 이동하고, LX Music Server 주소를 입력하십시오 (주로 좋아요.<http://127.0.0.1:23330>) 그리고 당신은 간다!

        </details>

    -   <details><summary>MusicBee</summary>

        -   설치하십시오<https://github.com/HenryPDT/mb_MediaControl>사용하기 전에

        </details>

    -   <details><summary>iTunes</summary>

        -   설치하십시오<https://github.com/thewizrd/iTunes-SMTC>사용하기 전에

        </details>

    -   <details><summary>AIMP</summary>

        -   설치하십시오<https://www.aimp.ru/?do=catalog&rec_id=1097>사용하기 전에

        </details>

-   🪟**다중 디스플레이 모드**
    -   **표준 모드**
        -   풍부한 가사 애니메이션과 아름답고 역동적 인 배경으로 몰입 형 청취 여행을 즐기십시오.
    -   **도크 모드**
        -   스마트 애니메이션 가사 바가 화면 가장자리에 도킹되었습니다.
    -   **데스크탑 모드**
        -   앱 위에 떠 다니는 몰입 형 가사를 즐기십시오

-   🧠**현명한 행동**
    -   음악이 잠시 멈췄을 때 자동 숨기십시오

> 이 프로젝트는 여전히 개발 중이며 버그 및 예기치 않은 행동은 최신 지점에 존재할 수 있습니다.

## 스크린 샷

### 표준 모드

![alt text](Screenshots/image.png)

![alt text](Screenshots/glow-float.gif)

![alt text](Screenshots/fan.png)

![alt text](Screenshots/lyrics-only.png)

![alt text](Screenshots/album-art-only.png)

### 도크 모드

![alt text](Screenshots/dock-1.png)

![alt text](Screenshots/dock-2.png)

### 데스크탑 모드

![alt text](Screenshots/desktop-1.png)

![alt text](Screenshots/desktop-2.png)

## 데모

Bilibili에서 소개 비디오 (2025 년 7 월 7 일에 업로드) 시청[여기](https://www.bilibili.com/video/BV1zjGjzfEXh).

## 지금 시도하십시오

### Microsoft Store

<a href="https://apps.microsoft.com/detail/9P1WCD1P597R?referrer=appbadge&mode=direct">
	<img src="https://get.microsoft.com/images/en-us%20dark.svg" width="200"/>
</a>

**가장 쉬운**그것을 얻는 방법.**제한 없는**무료 트레일 또는 구매 (있습니다**차이가 없습니다**무료 버전과 유료 버전 사이)

☕ 유용하다고 생각되면 구매를 고려하십시오.**Microsoft Store**, 감사합니다! 🥰

> 안정적인 버전이 구축되면 Microsoft Store가 최초로 업데이트되는 채널이 될 것입니다.

### 구글 드라이브

또는 Google 드라이브에서 가져 오십시오 (참조[풀어 주다](https://github.com/jayfunc/BetterLyrics/releases)링크 페이지)

> 설치 방법에 대한 가이드 ".zip"파일을 다운로드하는 데 주목하십시오.[이 문서](How2Install/How2Install.md).

## 💖 Many thanks to

-   [가사-리크스 헬퍼](https://github.com/WXRIW/Lyricify-Lyrics-Helper)
    -   QQ, Netease, Kugou Sources의 가사 페치, 암호 해독 및 구문 분석 제공
-   [lrclib](https://github.com/tranxuanthang/lrclib)
    -   lrclib 가사 API 제공 업체
-   [.NET 용 오디오 도구 라이브러리 (ATL)](https://github.com/Zeugma440/atldotnet)
    -   음악 파일에서 사진을 추출하는 데 사용됩니다
-   [Winuiex](https://github.com/dotMorten/WinUIEx)
    -   윈도우와 관련하여 Win32 API에 쉽게 액세스 할 수있는 방법을 제공하십시오
-   [taglib#](https://github.com/mono/taglib-sharp)
    -   오리지널 가사 콘텐츠를 읽는 데 사용됩니다
-   [오래된 -지식](https://github.com/dahall/Vanara)
    -   Win32 API 래퍼
-   [libretranslate](https://github.com/LibreTranslate/LibreTranslate)
    -   오프라인 가사 번역 능력을 제공하십시오
-   [stackoverflow- WPF에서 마진 속성을 애니메이션하는 방법](https://stackoverflow.com/a/21542882/11048731)
-   [드러내다](https://github.com/ghost1372/DevWinUI)
-   [BILIBILI -il WINUI3】 SystemBackbackDropController : 운모 및 아크릴 효과를 정의하십시오](https://www.bilibili.com/video/BV1PY4FevEkS)
-   [CNBLOGS- .NET 앱은 Windows System Media Control (SMTC)과 상호 작용합니다.](https://www.cnblogs.com/TwilightLemon/p/18279496)
-   [WIN2D의 게임 루프 : CanvasanImatedControl](https://www.cnblogs.com/walterlv/p/10236395.html)
-   [r2d2rigo/win2d 샘플](https://github.com/r2d2rigo/Win2D-Samples/blob/master/IrisBlurWin2D/IrisBlurWin2D/MainPage.xaml.cs)
-   [CommunityToolkit- 초보자부터 숙달까지](https://mvvm.coldwind.top/)

## 영감을 받았습니다

-   [정제-노우 플레이 네테이트](https://github.com/solstice23/refined-now-playing-netease)
-   [가사 애플](https://github.com/WXRIW/Lyricify-App)
-   [소금 플레이어](https://moriafly.com/program/salt-player)
-   [MyToolbar](https://github.com/TwilightLemon/MyToolBar)

## ✍️ 우리가 당신의 언어로 번역하도록 도와줍니다

당신의 언어를 찾을 수 없습니까?
괜찮아요! 번역을 시작하고 기고자 중 하나가 되십시오! 😆
클릭하십시오[링크](https://crowdin.com/project/betterlyrics/invite?h=d767e4f2dbd832d8fcdb6f7e5a198b402502866)이 앱을 Crowdin을 통해 언어로 번역하려면 지금!

## 스타 역사

[![Star History Chart](https://api.star-history.com/svg?repos=jayfunc/BetterLyrics&type=Date)](https://www.star-history.com/#jayfunc/BetterLyrics&Date)

## 모든 문제와 PR은 환영됩니다

버그를 찾으면 문제로 제출하거나 아이디어가 있으면 여기에서 자유롭게 공유하십시오.
