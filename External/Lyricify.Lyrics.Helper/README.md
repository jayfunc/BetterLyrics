# Lyricify Lyrics Helper

为 Lyricify 歌词相关功能竭力打造。

## 主要功能
- 歌词解析
  - Lyricify Syllable
  - Lyricify Lines
  - LRC
  - QRC
  - KRC
  - YRC
  - TTML
  - Spotify Lyrics (原始 JSON)
  - Musixmatch (原始 JSON)
- 歌词生成
  - Lyricify Syllable
  - Lyricify Lines
  - LRC
  - QRC
  - KRC
  - YRC
- 歌词歌曲搜索
  - QQ 音乐
  - 网易云音乐
  - 酷狗音乐
  - 汽水音乐
  - Apple Music
  - Musixmatch
  - Spotify (暂不支持)
- 歌词处理优化
  - Explicit 歌词处理及修复
  - YRC 歌词优化
  - 对唱识别 (暂不支持)
  - 信息行 (标题行) 识别
- 歌词解密
  - QRC
  - KRC
- 内嵌通用帮助类
  - 中文帮助类 (简繁转换等)
  - 字符串帮助类
  - 数学帮助类

## 项目架构
### Lyricify.Lyrics.Helper
- Decrypter // 歌词解密相关
  - Krc
  - Qrc
- Generators // 歌词生成
- Helpers // 帮助静态类
  - General // 内嵌通用帮助
    - ChineseHelper // 中文帮助
    - StringHelper // 字符串帮助
  - Optimization // 歌词处理优化
    - Explicit // Explicit 歌词处理及修复
    - Yrc // YRC 歌词优化
    - Musixmatch // Musixmatch 歌词优化
    - Apple Music // Apple Music 歌词处理及优化
    - SyncDowngrade // 同步级别降级 (如逐字降级至逐行)
    - InfoLines // 信息行处理
  - Types // 歌词类型
    - Lrc // LRC 歌词类型特性
  - GeneratorHelper // 生成帮助
  - OffsetHelper // 偏移帮助 (用于对歌词添加 Offset 偏移)
  - ParserHelper // 解析帮助
  - SearchHelper // 搜索帮助
  - TypeHelper // 歌词类型帮助
- Models // 歌词模型
- Parsers // 歌词解析
- Providers // 歌词提供者
  - Web // 提供者相关接口
- Searchers // 歌曲搜索
  - Helpers
    - ArtistHelper // 艺人帮助 (艺人中英文名对照)
    - CompareHelper // 信息匹配帮助
  - SearcherHelper // 实例化的搜索类

### Lyricify.Lyrics.Demo
- Program
  - ParsersDemo // 歌词解析演示
  - GeneratorsDemo // 歌词生成演示
  - TypeDetectorDemo // 歌词类型判断演示
  - SearchDemo // 歌曲搜索演示

## 感谢与支持
特别感谢 [@cnbluefire](https://github.com/cnbluefire), [@Raspberry Kan](https://github.com/Raspberry-Monster) 提供的帮助和支持。  
#### 感谢以下第三方代码
- LyricParser (MIT License): https://github.com/HyPlayer/LyricParser
- 163MusicLyrics (Apache-2.0 License): https://github.com/jitwxs/163MusicLyrics
