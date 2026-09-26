**English** | [**中文**](README.CN.md)

<div align="center">
  <img src="docs/assets/promotion/logo.png" alt="BetterLyrics Logo" width="120">
  
  <h1>BetterLyrics</h1>

  <h3>Strums the Heartstrings, Graces the Wordscapes</h3>

  <p>
    An elegant, highly customizable lyrics visualizer and versatile music player built with WinUI 3 & Win2D.
  </p>

  <p>
    <a href="https://github.com/jayfunc/BetterLyrics/stargazers"><img src="https://img.shields.io/github/stars/jayfunc/BetterLyrics" alt="Stars"></a>
    <a href="https://github.com/jayfunc/BetterLyrics/releases/latest"><img src="https://img.shields.io/github/downloads/jayfunc/BetterLyrics/total?label=Downloads" alt="Downloads"></a>
    <img src="https://img.shields.io/badge/Language-C%23-purple" alt="C#">
    <img src="https://img.shields.io/badge/Framework-WinUI%203-blue" alt="WinUI 3">
    <a href="https://github.com/jayfunc/BetterLyrics/blob/main/LICENSE"><img src="https://img.shields.io/badge/License-GPL_v3.0-blue" alt="License"></a>
  </p>

  <p>
    <a href="https://betterlyrics.github.io"><img alt="Official Website" src="https://img.shields.io/github/actions/workflow/status/BetterLyrics/BetterLyrics.github.io/pages%2Fpages-build-deployment?label=Official%20Website"></a>
    <a href="https://crowdin.com/project/betterlyrics"><img src="https://badges.crowdin.net/betterlyrics/localized.svg" alt="Crowdin"></a>
    <a href="https://luizvbo.github.io/kstars/pages/language.html?lang=CSharp"><img src="https://img.shields.io/badge/GitHub-Top%201000%20(C%23)-purple" alt="GitHub C# Top 1000"></a>
  </p>

  <img src="docs/assets/promotion/banner.png" alt="Banner" width="100%" style="border-radius: 10px; margin-top: 20px; margin-bottom: 20px;">
  
  <p>
    <a href="https://hellogithub.com/repository/jayfunc/BetterLyrics" target="_blank"><img src="https://abroad.hellogithub.com/v1/widgets/recommend.svg?rid=d2af74f0aea146ad8e4b2086982f5777&claim_uid=SgtQs9c54C8wjnv" alt="HelloGitHub" height="40"></a>
    <a href="https://atomgit.com/jayfunc/BetterLyrics" target="_blank"><img src="https://atomgit.com/jayfunc/BetterLyrics/star/new_badge.svg" alt="AtomGit G-Star" height="40"></a>
    <a href="https://trendshift.io/repositories/16452" target="_blank"><img src="https://trendshift.io/api/badge/repositories/16452" alt="Trendshift" height="40"/></a>
  </p>
  
  <p>
    <a href="https://deepwiki.com/jayfunc/BetterLyrics" target="_blank"><img src="https://deepwiki.com/badge.svg" alt="DeepWiki" height="20"></a>
    <a href="https://zread.ai/jayfunc/BetterLyrics" target="_blank"><img src="https://img.shields.io/badge/Ask_Zread-_.svg?style=flat&color=00b0aa&labelColor=000000&logo=data%3Aimage%2Fsvg%2Bxml%3Bbase64%2CPHN2ZyB3aWR0aD0iMTYiIGhlaWdodD0iMTYiIHZpZXdCb3g9IjAgMCAxNiAxNiIgZmlsbD0ibm9uZSIgeG1sbnM9Imh0dHA6Ly93d3cudzMub3JnLzIwMDAvc3ZnIj4KPHBhdGggZD0iTTQuOTYxNTYgMS42MDAxSDIuMjQxNTZDMS44ODgxIDEuNjAwMSAxLjYwMTU2IDEuODg2NjQgMS42MDE1NiAyLjI0MDFWNC45NjAxQzEuNjAxNTYgNS4zMTM1NiAxLjg4QEkgNS42MDAxIDIuMjQxNTYgNS42MDAxSDQuOTYxNTZDNS4zMTUwMiA1LjYwMDEgNS42MDE1NiAxLjMxMzU2IDUuNjAxNTYgNC45NjAxVjIuMjQwMUM1LjYwMTU2IDEuODg2NjQgNS4zMTM1MiAxLjYwMDEgNC45NjE1NiAxLjYwMDFaIiBmaWxsPSIjZmZmIi8%2BCjxwYXRoIGQ9Ik00Ljk2MTU2IDEwLjM5OTlIMi4yNDE1NkMxLjg4ODEgMTAuMzk5OSAxLjYwMTU2IDEwLjY4NjQgMS42MDE1NiAxMS4wMzk5VjEzLjc1OTlDMS42MDE1NiAxNC4xMTM0IDEuODg4MSAxNC4zOTk5IDIuMjQxNTYgMTQuMzk5OUg0Ljk2MTU2QzUuMzE1MDIgMTQuMzk5OSA1LjYwMTU2IDE0LjExMzQgNS42MDE1NiAxMy43NTk5VjExLjAzOTlDNS42MDE1NiAxMC42RHZ6IDUuMzE1MDIgMTAuMzk5OSA0Ljk2MTU2IDEwLjM5OTlaIiBmaWxsPSIjZmZmIi8%2BCjxwYXRoIGQ9Ik0xMy43NTg0IDEuNjAwMUgxMS4wMzg0QzEwLjY4NSAxLjYwMDEgMTAuMzk4NCAxLjg4NjY0IDEwLjM5ODQgMi4yNDAxVjQuOTkxMUMxMC4zOTg0IDUuMzEzNTYgMTAuNjg1IDUuNjAwMSAxMS4wMzg0IDUuNjAwMUgxMy43NTk5QzE0LjExMTkgNS42MDAxIDE0LjM5ODQgNS4zMTM1NiAxNC4zOTk5IDQuOTYwMVYyLjI0MDFDMTQuMzk5OCAxLjg4NjY0IDE0LjExMTkgMS42MDAxIDEzLjc1ODQgMS42MDAxWiIgZmlsbD0iI2ZmZiIvPgo8Y2F0aCBkPSJNNCAxMkwxMiA0TDQgMTJaIiBmaWxsPSIjZmZmIi8%2BCjxwYXRoIGQ9Ik00IDEyTDEyIDQiIHN0cm9rZT0iI2ZmZiIgc3Ryb2tlLXdpZHRoPSIxLjUiIHN0cm9rZS1saW5lY2FwPSJyb3VuZCIvPgo8L3N2Zz4K&logoColor=ffffff" alt="Zread"></a>
  </p>

</div>

---

## Table of Contents

- [Highlighted Features](#highlighted-features)
- [Download & Install](#download--install)
- [Screenshots](#screenshots)
- [Community](#community)
- [Contribute & Build](#contribute--build)
- [Donations](#donations)
- [Star History](#star-history)
- [Media Mentions](#media-mentions)
- [License & Credits](#license--credits)

---

## Highlighted Features

### Stunning Visuals & Expressive Lyrics
- **Buttery-Smooth UI:** A modern experience powered by WinUI 3 & Win2D, featuring fluid animations, highly customizable playback controls, and extensive personalization.
- **Advanced Lyric Motion:** Every word breathes with the music. Includes **per-syllable highlighting**, **long-note duration glows**, and **perspective-based fading** for distant lines.
- **Interactive Layout Editor:** A full-fledged drag-and-drop editor allowing you to design your perfect layout by freely positioning and resizing lyrics, album art, and playback controls.
- **Total Visual Control:** Beyond presets, you have full control to fine-tune lyric animations, fonts, theme color extraction, and individual visual behaviors to match your unique taste.
- **Chameleon Adaptive Theme:** Smart theme engine that dynamically samples your screen edge environment or album art to seamlessly blend the player UI with your desktop.
- **Immersive Backgrounds:** Beautiful visual effects including Dynamic Fluid, Blur, Fog, and Snowflake particles.
- **Audio Visualizer:** Built-in real-time spectrum analyzer that brings your music to life.
- **Lyrics Cards:** Generate and share gorgeous lyric cards with 10+ artistic themes (Vinyl, CD, Polaroid, Cyberpunk, and more).

### Smart Playback & Library Management
- **Versatile Player:** Play from **Local Drives** or stream via **SMB, WebDav, and FTP**. Features **playback memory** to resume where you left off.
- **Live Library Sync:** A high-performance media library that stays in sync with your local folder changes in real-time.
- **Universal Integration:** Seamlessly visualizes music from Spotify, Apple Music, NetEase, and [many other players](https://betterlyrics.github.io/docs/player-cfg/intro.html).
- **Social Presence:** Show what you're looping with **Discord Rich Presence** and sync your journey via **Last.fm** scrobbling.

### Precision Lyrics & Plugin System
- **Smart Matching:** Accurate matching with **customizable thresholds**, manual metadata mapping, and persistent source memory.
- **Lyrics Refinement:** Features **noise reduction** (filters non-lyric content) and **Simplified/Traditional Chinese conversion**.
- **Modular Architecture:** A plugin system for community-driven expansion of lyric sources, translation engines, and transliteration tools (e.g., Romaji).
- **Translation & AI Ready:** Built-in offline translation with a framework ready for **Local LLM** (AI) integration via plugins.
- **Core Support:** Native handling of `.lrc` (Standard/Enhanced), `.eslrc`, and `.ttml` formats.

### Adaptive Modes for Every Setup
- **Standard / Full Screen:** For a pure, immersive focus on the music.
- **Desktop / Wallpaper:** Floating "Always-on-Top" window or **embedded behind icons**—perfect for creative **Wallpaper Engine** setups.
- **Docked:** A dedicated, sleek Appbar that snaps to the top or bottom edge of your screen.
- **Narrow:** Optimized for vertical layouts, featuring track info at the top and a massive, high-density lyric area.
- **Taskbar:** Save your desktop real estate by living directly inside the Windows Taskbar.

### Intelligence & Analytics
- **Smart Automation:** Automatically stays out of your way by hiding when the music stops.
- **Stats Dashboard & "Wrapped" Story:** A beautiful analytics hub to track your play history, complete with an immersive visual story revealing your top artists, listener persona, and special streaks.

---

## Download & Install

<div align="center">

| Microsoft Store (Recommended) | Manual Install |
| :---: | :---: |
| <a href="https://apps.microsoft.com/detail/9P1WCD1P597R?referrer=appbadge&mode=direct"><img src="https://get.microsoft.com/images/en-us%20dark.svg" width="160" alt="Get it from Microsoft"/></a><br>Unlimited free trial (Same as paid) | [**Latest Release (.zip)**](https://github.com/jayfunc/BetterLyrics/releases/latest)<br>See [Installation Guide](https://betterlyrics.github.io/docs/get-started/install.html) |

**[Docs](https://betterlyrics.github.io/docs/get-started/welcome.html) | [Plugin Store](https://betterlyrics.github.io/docs/add-ons/plugins-store.html) | [Privacy Policy](docs/PRIVACY_POLICY.md) | [Terms of Service](docs/TERMS_OF_SERVICE.md)**

</div>

---

## Screenshots

<p align="center">
  <img src="docs/assets/screenshots/std.png" width="49%">
  <img src="docs/assets/screenshots/narrow.png" width="49%">
  <br>
  <img src="docs/assets/screenshots/effect.png" width="49%">
  <img src="docs/assets/screenshots/all-in-one.png" width="49%">
  <br>
  <img src="docs/assets/screenshots/fs3.png" width="49%">
  <img src="docs/assets/screenshots/fs2.png" width="49%">
  <br>
  <img src="docs/assets/screenshots/music-gallery.png" width="49%">
  <img src="docs/assets/screenshots/stats.png" width="49%">
</p>

### Lyrics Card

<p align="center">
  <img src="docs/assets/screenshots/card-feat.png" alt="Lyrics Card Feature">
</p>

<details>
<summary><b>Click here to view all card styles</b></summary>
<br>

#### Classic Design
<img src="docs/assets/screenshots/card-0.png">

#### Physical
<img src="docs/assets/screenshots/card-1.png">

#### Digital Retro
<img src="docs/assets/screenshots/card-2.png">

#### Atmosphere
<img src="docs/assets/screenshots/card-3.png">

#### Chinese Elegance
<img src="docs/assets/screenshots/card-4.png">

#### Traces of Time
<img src="docs/assets/screenshots/card-5.png">

</details>

### Demonstration

> Watch our demo video on Bilibili [here](https://www.bilibili.com/video/BV1QRstz1EGt/).

---

## Community

Join our communities to get support, share your layouts, and stay updated:

<div align="center">

[QQ Group 1](https://qm.qq.com/q/yArcw3n8pq) (1054700388) • [QQ Group 2](https://qm.qq.com/q/27rzSjFXt6) (1076554669) • [QQ Channel](https://pd.qq.com/s/1u1ntkyzr?b=9) • [Discord](https://discord.gg/5yAQPnyCKv) • [Telegram](https://t.me/+svhSLZ7awPsxNGY1)

</div>

---

## Contribute & Build

We welcome community contributions! Here is how you can help:

- **Help us translate:** Cannot find your language? [Start translating here](https://github.com/jayfunc/BetterLyrics?tab=contributing-ov-file).
- **Develop Plugins:** Want to extend functionality? [Read the Developer Guide](https://betterlyrics.github.io/plugin-dev/intro/).
- **Build from source:** [View build instructions](https://betterlyrics.github.io/get-started/download/#visual-studio).

---

## Donations

If you enjoy using BetterLyrics, please consider supporting the project. Your contributions help keep the development active!

<div align="center">

| Web Platforms | Alipay / WeChat Pay |
| :---: | :---: |
| [PayPal](https://paypal.me/zhefangpay)<br><br>[Buy Me a Coffee](https://buymeacoffee.com/founchoo)<br><br>[爱发电 (Afdian)](https://afdian.com/a/jayfunc) | <img src="docs/assets/donation/alipay_wechatpay.png" height="250" alt="Alipay and WeChat Pay QR Codes"> |

**[View the full Hall of Fame (Sponsors)](docs/SPONSORS.md)**

</div>

---

## Star History

<a href="https://star-history.dera.page/#jayfunc/BetterLyrics&type=Date">
 <picture>
   <source media="(prefers-color-scheme: dark)" srcset="https://star-history.dera.page/svg?repos=jayfunc/BetterLyrics&type=Date&theme=dark" />
   <source media="(prefers-color-scheme: light)" srcset="https://star-history.dera.page/svg?repos=jayfunc/BetterLyrics&type=Date" />
   <img alt="Star History Chart" src="https://star-history.dera.page/svg?repos=jayfunc/BetterLyrics&type=Date" />
 </picture>
</a>

---

## Media Mentions & Community Features

Discover what the community and media are saying about BetterLyrics! Check out our full [Media & Community Features List](docs/MEDIA.md).

---

## License & Credits

This project is licensed under the **[GNU General Public License v3.0](LICENSE)**.

### Special Thanks, Credits & Inspiration

#### Contributors

- [jayfunc](https://github.com/jayfunc) `C` `I` `Q` `D`
- [Raspberry-Monster](https://github.com/Raspberry-Monster) `C`
- [zxbmmmmmmmmm](https://github.com/zxbmmmmmmmmm) `C`
- [ZHider](https://github.com/ZHider) `C`
- [YUZU384](https://github.com/YUZU384) `C`
- [kusutori](https://github.com/kusutori) `C`
- [PiYuanZhouLv](https://github.com/PiYuanZhouLv) `C`
- [SuHeAndZl](https://crowdin.com/profile/SuHeAndZl) `I` `Q` `D`
- [borcolasky](https://crowdin.com/profile/borcolasky) `I`

> `C` Code ╹ `I` i18n ╹ `Q` QA ╹ `D` Docs

#### Dependencies & References

| Projects / Packages | Description |
| :--- | :--- |
| [Isolation](https://github.com/Storyteller-Studios/Isolation) | Dynamic fluid background implementation |
| [LibreTranslate](https://github.com/LibreTranslate/LibreTranslate) | Offline lyrics translation provider |
| [lrclib](https://github.com/tranxuanthang/lrclib) | LRCLIB lyrics API provider |
| [Lyricify-Lyrics-Helper](https://github.com/WXRIW/Lyricify-Lyrics-Helper) | Lyrics fetch, decryption, and parsing for various sources |
| [Manzana-Apple-Music-Lyrics](https://github.com/dropcreations/Manzana-Apple-Music-Lyrics) | Apple Music lyrics fetch using Python |
| [SpectrumVisualization](https://github.com/Johnwikix/SpectrumVisualization) | Audio visualization reference |

*See [dependencies](https://github.com/jayfunc/BetterLyrics/network/dependencies) for the full list.*

#### Inspired By

Some design ideas are referenced from the following projects (design inspiration only):
- [refined-now-playing-netease](https://github.com/solstice23/refined-now-playing-netease) `FOSS`
- [Lyricify-App](https://github.com/WXRIW/Lyricify-App) `Prop`
- [Salt Player for Windows](https://moriafly.com/program/spw.html) `Paid` `Prop`
- [MyToolBar](https://github.com/TwilightLemon/MyToolBar) `FOSS`

> `FOSS` Free and Open Source Software ╹ `Prop` Proprietary ╹ `Paid` Paid

---

## Share on Social Media

<details>
<summary><b>Click to expand</b></summary>
<br>
<div align="center">
  <img src="https://socialify.git.ci/jayfunc/BetterLyrics/image?description=1&forks=1&issues=1&language=1&name=1&owner=1&pulls=1&stargazers=1&theme=Light" width="48%">
  <img src="https://opengraph.githubassets.com/<any_hash_number>/jayfunc/BetterLyrics" width="48%">
</div>
</details>

<br>

<div align="center">
  <mark><i>This project is under active development; unexpected issues may occur.</i></mark><br>
  <sub>Disclaimer: This project is provided "as is". All third-party resources belong to their respective owners.</sub>
</div>
