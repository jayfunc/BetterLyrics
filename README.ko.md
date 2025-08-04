![](Promotion/banner.png)

<div style="text-align: center;">

[❓ 자주 묻는 질문(FAQ) 보러가기](#faq)

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
WinUI 3와 Win2D로 제작된 동적 가사 디스플레이 도구 — 로컬 재생 및 다양한 플레이어 지원
</h3>

## 🎉 이 프로젝트는 SSPAI에 소개되었습니다!

기사 보기: [BetterLyrics – Windows용 몰입감 있고 부드러운 가사 디스플레이 도구](https://sspai.com/post/101028)

## 🔈 피드백 및 채팅 그룹

- [<img src="BetterLyrics.WinUI3\BetterLyrics.WinUI3\Assets\QQ.png" height="20"> QQ](https://qun.qq.com/universal-share/share?ac=1&authKey=4Q%2BYTq3wZldYpF5SbS5c19ECFsiYoLZFAIcBNNzYpBUtiEjaZ8sZ%2F%2BnFN0qw3lad&busi_data=eyJncm91cENvZGUiOiIxMDU0NzAwMzg4IiwidG9rZW4iOiJiVnhqemVYN0N5QVc3b1ZkR24wWmZOTUtvUkJoWm1JRWlaWW5iZnlBcXJtZUtGc2FFTHNlUlFZMi9iRm03cWF5IiwidWluIjoiMTM5NTczOTY2MCJ9&data=39UmAihyH_o6CZaOs7nk2mO_lz2ruODoDou6pxxh7utcxP4WF5sbDBDOPvZ_Wqfzeey4441anegsLYQJxkrBAA&svctype=4&tempid=h5_group_info) (1054700388)
- [<img src="BetterLyrics.WinUI3\BetterLyrics.WinUI3\Assets\Discord.png" height="12"> Discord](https://discord.gg/5yAQPnyCKv)
- [<img src="BetterLyrics.WinUI3\BetterLyrics.WinUI3\Assets\Telegram.png" height="16"> Telegram](https://t.me/+svhSLZ7awPsxNGY1)

## 🌟 주요 기능

- 🌠 **아름다운 사용자 인터페이스**
  - 부드러운 애니메이션과 효과
- ↔️ **강력한 가사 번역**
  - 오프라인 기계 번역(30개 언어 지원)
  - 로컬 가사 파일에서 내장 번역 자동 읽기
- 🧩 **다양한 가사 소스**
  - 로컬 저장소
    - 음악 파일(내장 가사 포함)
    - [.lrc](<https://en.wikipedia.org/wiki/LRC_(file_format)>) 파일(표준 및 확장 형식 지원)
    - [.eslrc](https://github.com/ESLyric/release) 파일
    - [.ttml](https://en.wikipedia.org/wiki/Timed_Text_Markup_Language) 파일
  - 온라인 가사 제공자
    - QQ Music
    - NetEase Cloud Music(网易云音乐)
    - Kugou Music(酷狗音乐)
    - [amll-ttml-db](https://github.com/Steve-xmh/amll-ttml-db)
    - [LRCLIB](https://lrclib.net/)
- 🎶 **다양한 음악 플레이어 지원**

  - <details><summary>⚠️ NetEase Cloud Music</summary>

    - 먼저 [BetterNCM 플러그인](https://microblock.cc/betterncm)을 설치하세요. 설치 후 다운그레이드 안내가 나오면 안내에 따라 NetEase Cloud Music을 2.10.13으로 다운그레이드하세요.
    - 그 후 PluginMarket에서 InfLink 플러그인을 설치하고 NetEase Cloud Music을 재시작하세요.
    - ⚠️ 플러그인 문제로 타임라인에 이슈가 있을 수 있습니다

    </details>

  - <details><summary>⚠️ Kugou Music</summary>

    - Kugou Music 설정에서 "시스템 재생 컨트롤(잠금 화면 등) 지원"을 켜세요
    - Kugou Music은 타임라인 정보를 전송하지 않아, 재생 위치를 변경해도 BetterLyrics에서 감지할 수 없습니다
    - ⚠️ 타임라인 문제는 Kugou 자체의 한계입니다

    </details>

  - <details><summary>⚠️ Apple Music</summary>

    - 설정의 "고급 옵션"에서 타임라인 임계값을 약 600ms로 설정하세요. 그렇지 않으면 가사가 계속 앞뒤로 흔들립니다.
    - ⚠️ 가사 흔들림을 방지하려면 추가 설정이 필요합니다(자세한 내용은 문서 하단 FAQ 참조)

    </details>

  - <details><summary>⚠️ foobar2000</summary>

    - https://github.com/dumbie/foo_mediacontrol 을 설치하세요
    - ⚠️ 플러그인 문제로 타임라인에 이슈가 있을 수 있습니다

    </details>

  - Spotify
  - QQ Music
  - PotPlayer
  - 미디어 플레이어(시스템)

  - <details><summary>LX Music</summary>

    - LX Music 설정 페이지에서 "Open API"를 활성화하세요
    - BetterLyrics 설정-고급 옵션에서 LX Music 서버 주소(보통 http://127.0.0.1:23330)를 입력하세요

    </details>

  - <details><summary>MusicBee</summary>

    - https://github.com/HenryPDT/mb_MediaControl 을 설치하세요

    </details>

  - <details><summary>iTunes</summary>

    - https://github.com/thewizrd/iTunes-SMTC 를 설치하세요

    </details>

  - <details><summary>AIMP</summary>

    - https://www.aimp.ru/?do=catalog&rec_id=1097 을 설치하세요

    </details>

- 🪟 **다양한 표시 모드**
  - **표준 모드**
    - 몰입감 있는 가사 애니메이션과 동적 배경
  - **도킹 모드**
    - 화면 가장자리에 고정되는 스마트 가사 바
  - **데스크톱 모드**
    - 앱 위에 가사를 띄워서 표시
- 🧠 **스마트 동작**
  - 음악이 일시정지되면 자동으로 숨김

> 본 프로젝트는 개발 중입니다. 최신 브랜치에는 버그나 예기치 않은 동작이 있을 수 있습니다.

## 스크린샷

### 표준 모드

![alt text](Screenshots/image.png)

![alt text](Screenshots/glow-float.gif)

![alt text](Screenshots/fan.png)

![alt text](Screenshots/lyrics-only.png)

![alt text](Screenshots/album-art-only.png)

### 도킹 모드

![alt text](Screenshots/dock-1.png)

![alt text](Screenshots/dock-2.png)

### 데스크톱 모드

![alt text](Screenshots/desktop-1.png)

![alt text](Screenshots/desktop-2.png)

## 데모

Bilibili에서 소개 영상을 시청하세요(2025년 7월 7일 업로드): [여기서 보기](https://www.bilibili.com/video/BV1zjGjzfEXh)

## 지금 사용해보세요

### Microsoft Store

<a href="https://apps.microsoft.com/detail/9P1WCD1P597R?referrer=appbadge&mode=direct">
	<img src="https://get.microsoft.com/images/en-us%20dark.svg" width="200"/>
</a>

**가장 쉬운** 설치 방법. **무제한** 무료 체험 또는 구매(무료와 유료 버전 차이 없음)

☕ 유용하다면 **Microsoft Store**에서 구매로 응원해 주세요! 🥰

> 안정 버전이 출시되면 Microsoft Store가 가장 먼저 업데이트됩니다.

### Google Drive

Google Drive에서도 다운로드할 수 있습니다([릴리즈](https://github.com/jayfunc/BetterLyrics/releases) 페이지 참조)

> ".zip" 파일을 다운로드합니다. 설치 방법은 [이 문서](How2Install/How2Install.md)를 참고하세요.

## 💖 특별 감사

- [Lyricify-Lyrics-Helper](https://github.com/WXRIW/Lyricify-Lyrics-Helper)
  - QQ, NetEase, Kugou 가사 가져오기, 복호화, 파싱 지원
- [lrclib](https://github.com/tranxuanthang/lrclib)
  - LRCLIB 가사 API 제공자
- [Audio Tools Library (ATL) for .NET](https://github.com/Zeugma440/atldotnet)
  - 음악 파일에서 이미지 추출에 사용
- [WinUIEx](https://github.com/dotMorten/WinUIEx)
  - Win32 창 API에 쉽게 접근 가능
- [TagLib#](https://github.com/mono/taglib-sharp)
  - 원본 가사 내용 읽기에 사용
- [Vanara](https://github.com/dahall/Vanara)
  - Win32 API 래퍼
- [LibreTranslate](https://github.com/LibreTranslate/LibreTranslate)
  - 오프라인 가사 번역 기능 제공
- [Stackoverflow - WPF에서 Margin 속성 애니메이션 적용 방법](https://stackoverflow.com/a/21542882/11048731)
- [DevWinUI](https://github.com/ghost1372/DevWinUI)
- [Bilibili -【WinUI3】SystemBackdropController: 운모, 아크릴 효과 정의](https://www.bilibili.com/video/BV1PY4FevEkS)
- [cnblogs - .NET 앱과 Windows 시스템 미디어 컨트롤(SMTC) 연동](https://www.cnblogs.com/TwilightLemon/p/18279496)
- [Win2D의 게임 루프: CanvasAnimatedControl](https://www.cnblogs.com/walterlv/p/10236395.html)
- [r2d2rigo/Win2D-Samples](https://github.com/r2d2rigo/Win2D-Samples/blob/master/IrisBlurWin2D/IrisBlurWin2D/MainPage.xaml.cs)
- [CommunityToolkit - 입문부터 고급까지](https://mvvm.coldwind.top/)

## 영감 받은 프로젝트

- [refined-now-playing-netease](https://github.com/solstice23/refined-now-playing-netease)
- [Lyricify-App](https://github.com/WXRIW/Lyricify-App)
- [소금 음악 Salt Player](https://moriafly.com/program/salt-player)
- [MyToolBar](https://github.com/TwilightLemon/MyToolBar)

## ✍️ 번역에 참여해 주세요

원하는 언어가 없나요?
걱정 마세요! 번역에 참여해 기여자가 되어 주세요! 😆
[이 링크](https://crowdin.com/project/betterlyrics/invite?h=d767e4f2dbd832d8fcdb6f7e5a198b402502866)에서 Crowdin으로 번역에 참여할 수 있습니다.

## Star 기록

[![](https://api.star-history.com/svg?repos=jayfunc/BetterLyrics&type=Date)](https://www.star-history.com/#jayfunc/BetterLyrics&Date)

## 이슈 및 PR 환영

버그를 발견하면 issues에 남겨주시고, 아이디어도 자유롭게 공유해 주세요.

---

## FAQ

### 도킹 모드에서 버튼이 보이지 않아요

"도킹 모드"에 들어가면 동작 버튼이 숨겨집니다. 창 상단에 마우스를 올리면 "몰입", "더보기", "닫기" 버튼이 나타납니다.

![alt text](FAQ/image-10.png)

창 하단 가장자리 바로 위에 마우스를 올리면 하단에 흰색 컨트롤 플로팅 창이 표시됩니다

![alt text](FAQ/image-11.png)

"작은 흰색 바"를 클릭하면 하단 플로팅 컨트롤 바(재생 진행, 타임라인 오프셋 조정, 이전곡/일시정지/다음곡, 번역, 레이아웃, 설정 포함)가 표시됩니다

![alt text](FAQ/image-12.png)

### 데스크톱 모드에서 창을 잠그는 방법

![alt text](FAQ/image-6.png)

상단에 마우스를 올리고 자물쇠 아이콘을 클릭하거나 `Ctrl + Alt + U`를 누르세요.

### 데스크톱 모드에서 창 잠금 해제 방법

![alt text](FAQ/image-7.png)

시스템 트레이 아이콘을 우클릭해 "창 잠금 해제"를 선택하거나 `Ctrl + Alt + U`를 누르세요.

### 가사 타임라인이 지연될 때

앱 맨 아래에 마우스를 올리세요.

![alt text](FAQ/image.png)

첫 번째 아이콘 버튼(가사 타임라인 오프셋)을 클릭하면 오프셋을 자유롭게 조정할 수 있습니다.

### 가사가 자주 앞뒤로 움직여요(예: Apple Music)

![alt text](FAQ/image-2.png)

"고급 옵션"에서 임계값(빨간 사각형 표시)을 늘리면 가사가 정상적으로 표시됩니다.
