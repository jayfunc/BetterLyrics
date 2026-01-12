**English** | [**中文**](USER_GUIDE.CN.md)

# ❓ Frequently Asked Questions (FAQ)

## 🎵 Playback & Connection

### 🤔 I use Netease Cloud Music as my music player

It is strongly recommended to first browse the configuration guide [here](#known-supported-music-players-configuration-guide).

### 🤔 Shows "No music playing"

Some players require extra configuration. Please check [here](#known-supported-music-players-configuration-guide).

### 🤔 How to play local songs via this software?

1. Ensure the local songs folder has been added to the software (Refer to Step 1 in [How to set up local lyrics sources?](#-how-to-set-up-local-lyrics-sources)).
2. Open `Music Library`.
    - Can be opened via the book button in the top-left area of the lyrics window.
    - Or by right-clicking the software icon in the system tray and selecting "Open Music Library".

## 🪟 Window & Interaction

### 🤔 How to move and resize the window?

If you cannot select or move the window, please check if you have set the `Draggable Area` option to `None` by mistake in the `Window` settings under `Lyrics Window Management`.

### 🤔 I can't see any buttons / The bottom command bar is hidden

**Top & Bottom Command Bars:**

- By default, when the mouse moves out of the top command bar and bottom command bar (playback control panel) areas, they automatically hide. Simply hover your mouse over these areas to show them again.
- If the window is locked, just hover over the window, and you'll see the unlock button.

**Cannot summon bottom panel:**
When the window is too small to display the panel, you can only show the playback control panel by hovering over the bottom of the lyrics window and clicking the white line.

### 🤔 What are the default shortcuts?

- `Ctrl + Alt + H`: Show/Hide lyrics window
- `Ctrl + Alt + S`: Switch lyrics window status (mode)
- `Ctrl + Alt + P`: Play/Pause current track
- `Ctrl + Alt + Right`: Next track
- `Ctrl + Alt + Left`: Previous track

> You can go to `Settings` -> `App Appearance & Behavior` to change the above shortcuts.

## 📃 Lyrics Display

### 🤔 Incorrect lyrics displayed (Mismatch)

- Please go to `Settings` -> `Playback Sources` -> `Lyrics Search Strategy` and select `Best Match`.
> Note: For each song, the first time using Best Match will take about 10 seconds. Please be patient.

- Or open the lyrics search window to manually search for the correct lyrics.

> The lyrics search window can be opened in one of the following ways:
> - Move the mouse to the lower area of the window until the bottom control bar appears, then click the magnifying glass icon (🔍) on the bottom right to open the search window.
> - Right-click the software icon in the system tray and select "Open Lyrics Window".

### 🤔 Lyrics keep moving back and forth (Timeline jumping)

Go to `Settings` > `Playback Sources` > Disable `Lyrics Timeline Sync` or increase the `Lyrics Timeline Sync Threshold`.

### 🤔 How to set up local lyrics sources?

**Step 1: Add Folder**
Go to `Settings` -> `Media Library`, click the `Add` button, add the folder where you store lyrics (or music files containing embedded lyrics), and ensure the switch on the right is set to **"On"**.

**Step 2: Prepare Files**
The software supports two methods:

1.  **External Lyrics Files (.lrc/.eslrc/.ttml)**
    When comparing, the software **does not distinguish word order** and **automatically ignores punctuation**.
    * **Recommended Naming:** `Artist - Title.lrc`
    * **Also Supports:** `Title Artist.lrc`
    * **Also Supports:** `Title (Live) - Artist.lrc`
    * *Example: When playing Taylor Swift's "Love Story", the filename `love_story_taylor_swift.lrc` can also be accurately identified.*

2.  **Embedded Lyrics**
    If lyrics are already written into the tags of your local music files (.mp3/.flac, etc.), the software will also automatically read them.

> We recommend the following ways to check music file tag information:
> - [Music Tag](https://www.cnblogs.com/vinlxc/p/11347744.html): Browse local song metadata.
> - If **Step 1: Add Folder** is completed, go to the BetterLyrics `Music Library` and click the three dots icon (···) on the right of a music entry to show parsed metadata (may differ from actual info).

> To ensure smooth matching for the **Embedded Lyrics** method, the following tools are recommended:
> - [Music Tag](https://www.cnblogs.com/vinlxc/p/11347744.html): Batch write **all metadata** into local music file tags.
> - [LDDC](https://github.com/chenmozhijin/LDDC): Batch write **verbatim lyrics** into local music file tags.

### 🤔 Partial garbled text in lyrics

This is usually a default font setting issue. Go to `Settings` -> `Lyrics Window Management` -> `Lyrics Style` -> `Lyrics Font` -> Type `Arial` to get the most stable display effect.

## 🎨 Modes & Appearance

### 🤔 How to add more modes?

The software has initially added all preset modes for you.

You can import window status files shared by others: Go to `Settings` -> `Lyrics Window Management` -> Import (Down Arrow Icon) -> Select the file to import (`.json` format).

> To export window status, right-click the window status and select `Export`.

### 🤔 How to switch modes?

- Press the default shortcut `Ctrl + Alt + S`, then select the mode displayed on the screen to switch. (Press `Esc` to close the selection window).
- Find the double-headed arrow icon in the top-left area of the lyrics window and click it to summon the lyrics window switcher.

## 📂 Data & Feedback

### 🤔 Where are lyrics cached?

`%LocalAppData%\Packages\37412.BetterLyrics_rd1g0rsrrtxw8\LocalCache\lyrics`

### 🤔 How to report issues? (Log location)

Please attach software logs when reporting issues. The log location is as follows:
`%LocalAppData%\Packages\37412.BetterLyrics_rd1g0rsrrtxw8\LocalCache\logs`

# Known Supported Music Players (Configuration Guide)

Usage: Find your music player and install the `Plugin` listed in the table.

> <mark>Supported music players include, but are not limited to, the list below. Please refer to your actual experience for confirmation.</mark>

> <mark>All media players that have integrated with [SMTC](https://learn.microsoft.com/en-us/windows/uwp/audio-video-camera/integrate-with-systemmediatransportcontrols) are supported.</mark>

| Music Player | Plugin | Notes |
| :--- | :--- | :--- |
| [AIMP](https://www.aimp.ru) | [Plugin](https://www.aimp.ru/?do=catalog&rec_id=1097) | - |
| [AMLL Player](https://github.com/Steve-xmh/applemusic-like-lyrics/blob/full-refractor/packages/player/README.md) | - | - |
| [Apple Music](https://music.apple.com) | - | - |
| [BetterLyrics](https://github.com/jayfunc/BetterLyrics) | - | - |
| [foobar2000](https://www.foobar2000.org/windows) | [Plugin](https://github.com/dumbie/foo_mediacontrol) | Timeline issues |
| [Google Chrome](https://www.google.com/chrome) | - | - |
| [HyPlayer](https://github.com/HyPlayer/HyPlayer) | - | - |
| [iTunes](https://apps.microsoft.com/detail/9pb2mz1zmb1s) | [Plugin](https://github.com/thewizrd/iTunes-SMTC) | - |
| [Kugou Music](https://www.kugou.com) | - | ![](docs/assets/faq/Snipaste_2025-11-01_12-51-44.png?raw=true) Timeline issues |
| [Listen 1](https://listen1.github.io/listen1) | - | - |
| [LX Music](https://github.com/lyswhut/lx-music-desktop) | - | Check "Enable Open API Service" in LX Music Settings "Open API";<br>Input LX Music server address in BetterLyrics Settings under "Playback Sources" -> "LX Music Server". |
| [Microsoft Edge](https://www.microsoft.com/edge) | - | - |
| [MoeKoe Music](https://github.com/MoeKoeMusic/MoeKoeMusic) | - | - |
| [MusicBee](https://getmusicbee.com) | [Plugin](https://github.com/HenryPDT/mb_MediaControl) | - |
| [MusicPlayer2](https://github.com/zhongyang219/MusicPlayer2) | - | - |
| [Netease Cloud Music](https://music.163.com) | [BetterNCM](https://microblock.cc/betterncm) <br> [InfLink-rs](https://github.com/apoint123/inflink-rs) | ![](docs/assets/faq/Snipaste_2025-11-01_19-25-56.png?raw=true) |
| [PlanetMusic](https://apps.microsoft.com/detail/9nt5122pwqb8) | - | - |
| [PotPlayer](https://potplayer.daum.net) | - | - |
| [QQ Music](https://y.qq.com) | - | Ensure you are using a newer version, and enable "Show System Media Transport Controls (SMTC)" in QQ Music settings. |
| [Salt Player for Windows](https://moriafly.com/program/spw.html) | - | - |
| [Spotify](https://open.spotify.com) | - | - |
| [Windows Media Player](https://apps.microsoft.com/detail/9wzdncrfj3pt) | - | - |
| - | - | - |

# Lyrics Source Configuration

## Apple Music

- Open the [Apple Music website](https://music.apple.com/) and the Developer Tools window.
- Log in to your account.
- Refresh the page.
- Return to the Developer Tools window, select `Fetch/XHR`, choose a request, find the `media-user-token` header in the request headers, and copy its value.
- Open BetterLyrics and go to `Playback Sources` settings.
- Input the copied value into the `Apple Music media-user-token` setting, then click the accept icon on the right.