## Contributing / 贡献

### Code / 代码

We welcome code contributions through GitHub Pull Requests:

1. Fork the repository and create a branch from `dev`.
2. Make your changes and keep each commit focused.
3. Open a Pull Request targeting `dev`, describing what changed and how you tested it.

欢迎通过 GitHub Pull Request 贡献代码：

1. Fork 本仓库，并基于 `dev` 分支创建自己的分支。
2. 完成修改，并尽量让每个提交聚焦于一件事。
3. 向 `dev` 分支提交 Pull Request，说明改动内容与验证方式。

[Open a Pull Request / 提交 Pull Request](https://github.com/jayfunc/BetterLyrics/pulls)

---

## 🌍 Translation / 翻译

We want **BetterLyrics** to be accessible to everyone, and we are grateful to everyone who has helped improve its translations.

我们致力于让 **BetterLyrics** 面向全球用户，感谢每一位曾为翻译改进提供帮助的朋友。

### Punctuation / 标点

Keep punctuation consistent across languages, but use each language's own marks.

- Do not end UI labels, headers, option names, placeholders, or single-sentence descriptions with a sentence mark — even when they contain commas, semicolons, dashes, placeholders, slashes, or quotation marks (e.g. `MediaFieldRuleKindTemplate.Content` = `Template: {field} placeholders`).
- End multi-sentence descriptions and messages (InfoBars, errors, notices) with the language's sentence mark: `.` in English, `。` in Chinese/Japanese, `।` in Hindi, `.` in Arabic; Thai usually omits it.
- Keep meaningful ending marks such as `?`/`？`, `!`/`！`, `:`/`：`, trailing commas, and ellipses. Do not strip dots from abbreviations or file extensions (`e.g.`, `Hrs.`, `.lrc`).
- In Chinese and Japanese, use full-width punctuation and do not mix half-width `, . ; : ? ! ( )` into CJK text. Use `……` for Chinese ellipsis and `…` for Japanese.
- Simplified Chinese uses curly double quotes `“”`; Traditional Chinese and Japanese use corner brackets `「」`. English uses straight double quotes `"..."`.
- Do not put a space after full-width punctuation. Keep a space between CJK and Latin/digits (`LX Music 服务器`), and between numbers and units (`0 毫秒`, `3 分钟`).
- French uses a non-breaking space before `:`, `;`, `?`, `!` and inside `« »`.
- When a sentence is split across multiple resource entries, apply the rule to each entry and keep commas or spaces on earlier fragments.
- The Music Memoir / StoryPage slides are an exception: they must be elegant, poetic, and rhyme within each scene, but never end with a sentence mark. Avoid flattening them into literal UI phrasing.

保持一致，但使用各语言自己的标点：

- 界面标签、标题、选项名、占位符和单句说明，句尾都不加句末标点，即使中间包含逗号、分号、破折号、占位符 `{}`、斜杠或引号（如 `MediaFieldRuleKindTemplate.Content` = `模板：{field} 占位符`）。
- 多句说明和提示消息（InfoBar、错误、通知）按语言补上句末标点：英文 `.`、中/日 `。`、印地语 `।`、阿拉伯语 `.`；泰语通常不加。
- 保留有意义的结尾符号（`?`/`？`、`!`/`！`、`:`/`：`、结尾逗号、省略号）；不要删掉缩写或扩展名里的点（`e.g.`、`Hrs.`、`.lrc`）。
- 中文和日文使用全角标点，不要把半角 `, . ; : ? ! ( )` 混进中日文。中文省略号用 `……`，日文用 `…`。
- 简体中文用弯引号 `“”`，繁体中文和日文用直角引号 `「」`，英文用直引号 `"..."`。
- 全角标点后不加空格；中英文、数字与单位之间保留空格（`LX Music 服务器`、`0 毫秒`、`3 分钟`）。
- 法文在 `:`、`;`、`?`、`!` 前和 `« »` 内使用不换行空格。
- 一句文案被拆成多条资源时，每条分别按此规则处理，中间片段保留逗号或空格。
- 音乐回忆录（StoryPage）slides 是特例：文案必须优雅、诗意，并在每个场景内押韵，但句尾不加句点，不要写成生硬的界面提示语。

---

### Resource files / 资源文件

- All UI copy lives in `src/BetterLyrics.DotNet/BetterLyrics.WinUI3/BetterLyrics.WinUI3/Strings/<locale>/Resources.resw`. Do not hard-code user-facing text in XAML or C#.
- `en` is the source. When adding or removing a key, update every locale folder (currently 18) and keep the key order aligned.
- Put locale-specific separators in resources instead of hard-coding them. `ListSeparator` is shared by list-style messages: `、` for Chinese/Japanese, `، ` for Arabic, `, ` for other languages.

- 所有界面文案都在 `src/BetterLyrics.DotNet/BetterLyrics.WinUI3/BetterLyrics.WinUI3/Strings/<locale>/Resources.resw`，不要在 XAML 或 C# 里硬编码用户可见文本。
- 以 `en` 为源；新增或删除键时，所有语言目录（当前 18 个）要同步修改，并保持键顺序一致。
- 语言相关的分隔符也应放进资源，不要硬编码。`ListSeparator` 用于列表类消息：中日文用 `、`，阿拉伯语用 `، `，其他语言用 `, `。

---

### Translation workflow / 翻译方式

The contributors listed below contributed through our former Crowdin platform. Crowdin has reached its free usage limit, so translations for all languages will be generated with AI going forward. We still welcome translation contributions — if you would like to improve a translation, please [submit a Pull Request](https://github.com/jayfunc/BetterLyrics/pulls) directly.

下方列出的贡献者是通过我们以往使用的 Crowdin 平台参与翻译的。由于 Crowdin 已达到免费额度上限，今后各语言的翻译将由 AI 完成。项目仍然接受翻译贡献；如果你希望改进译文，请[直接提交 Pull Request](https://github.com/jayfunc/BetterLyrics/pulls)。

---

### Past translation contributors / 往期翻译贡献者

| Language / 语言 | Code | Contributors / 贡献者 |
| --- | --- | --- |
| English | en | jayfunc |
| Italian (Italiano) | it | pascal-00 |
| Japanese (日本語) | ja | SuHeAndZl |
| Simplified Chinese (简体中文) | zh-Hans | jayfunc borcolasky |
| Traditional Chinese (繁體中文) | zh-Hant | borcolasky SuHeAndZl |

> 💡 **Note / 提示:** To keep the list concise, minor contributions (e.g., translating only a few strings) may not be explicitly listed above, but we are deeply grateful for every helper!
> 为了保持列表简洁，较小的贡献（如仅翻译了个别词条）可能不会在上方明确列出，但我们由衷感谢每一位热心帮助的朋友！
