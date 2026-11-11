# TTML parser

Native C# port of the parsing/model/normalization portions of
[apoint123/ttml-processor-rs](https://github.com/apoint123/ttml-processor-rs),
based on local revision `4b01f5ab8ce040b4b26413c414654d8b9833a293`.
The upstream MIT license is preserved in `LICENSE`; it also covers the copied
regression fixture and snapshot in `tests/BetterLyrics.Ttml.Tests/Fixtures`.

`TtmlParser` parses Apple Music/AMLL lyrics, head and inline auxiliary tracks,
background vocals, Ruby annotations, agents, platform/custom metadata and audio
metadata. It uses only the .NET XML APIs; no Rust binary or native dependency is
required. The generator, WASM bindings and AMLL serialization converters are
outside this parser replacement.

`TtmlLyricsAdapter` maps the result to the existing application models. Background
vocals become separate lyric lines with the main singer ID. Word spaces and UTF-16
character indices are reconstructed together. The existing translation and
romanization preferences still select tracks. Ruby base text/timing is rendered;
Ruby readings, song parts, audio roles and other richer fields remain available
in the parser result but have no corresponding display fields in the app.
Audio offsets are returned as metadata and do not automatically shift timestamps.
Romanization without a language tag uses the original language with the Latin
script tag, so the host can select it and fill its tertiary text field. Translation
fills secondary text using the host target language; an unspecified target script
can match an explicit script (for example `zh` matches `zh-Hans`).

Behavior changes from the old parser:

- Required line/word timestamps are validated instead of silently becoming zero;
  zero itself remains a valid timestamp. Invalid documents produce no partial
  lyric tracks. Diagnostics include XML line/column and lyric ID.
- Timestamp syntax and truncation follow the Rust parser (seconds or colon clock
  forms, maximum `uint` milliseconds; no `s` suffix). The application adapter
  rejects values above `int.MaxValue` instead of wrapping them.
- Background parentheses and whitespace follow upstream normalization; head
  word-by-word romanization uses spaces between words.
- XML namespace URIs, including both Apple namespace variants found in upstream
  fixtures, are recognized independently of prefix spelling. Unlike quick-xml's
  permissive reader, .NET requires well-formed XML with declared prefixes. DTDs
  and external entities are prohibited. CDATA is handled as text.

Run the dependency-free regression executable from the repository root:

```powershell
dotnet run --project tests/BetterLyrics.Ttml.Tests
```

It links the production parser, adapter and lyric models directly and targets
.NET 9 so it can run independently of the app's .NET 10/WinUI SDK requirements.
To compare every fixture against the original Rust JSON snapshots:

```powershell
dotnet run --project tests/BetterLyrics.Ttml.Tests -- D:\Data\vsCode\Git\ttml-processor-rs
```

All eight upstream snapshots were compared, including every serialized metadata,
word, Ruby, background and auxiliary-track field. The bundled complex fixture
keeps the default regression run independent of that external checkout.
