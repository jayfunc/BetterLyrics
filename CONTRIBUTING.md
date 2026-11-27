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

- If a string has no punctuation inside it, do not add a mark at the end.
- If a string already contains punctuation inside (comma, semicolon, period, etc.), end it with the language's sentence mark — `.` in English, `。` in Chinese/Japanese, `।` in Hindi, `.` in Arabic; Thai usually omits it.
- UI labels, option names, templates, and other fragments are not sentences even when they contain dashes, braces, slashes, or quotation marks; do not add a sentence mark to them (e.g. `MediaFieldRuleKindTemplate.Content` = `Template — {field} placeholders`).
- Keep meaningful ending marks such as `?`/`？`, `!`/`！`, `:`/`：`, trailing commas, and ellipses; do not strip dots from abbreviations or extensions (`Hrs.`, `.lrc`).
- When a sentence is split into multiple resource entries, apply the rule to each entry and keep commas or spaces on earlier fragments.
- The Music Memoir / StoryPage slides are an exception: they must be elegant, poetic, and rhyme within each scene, but never end with a sentence mark. Avoid flattening them into literal UI phrasing.

保持一致，但使用各语言自己的标点：

- 文案中间没有任何标点时，句尾不加标点。
- 文案中间已有逗号、分号、句号等标点时，句尾按语言补上对应句末标点：英文 `.`、中/日 `。`、印地语 `।`、阿拉伯语 `.`；泰语通常不加。
- 界面标签、选项名、模板等非句子片段即使包含破折号、占位符 `{}`、斜杠或引号，也不加句末标点（如 `MediaFieldRuleKindTemplate.Content` = `模板 —— {field} 占位符`）。
- 保留有意义的结尾符号（`?`/`？`、`!`/`！`、`:`/`：`、结尾逗号、省略号）；不要删掉缩写或扩展名里的点（`Hrs.`、`.lrc`）。
- 一句文案被拆成多条资源时，每条分别按此规则处理，中间片段保留逗号或空格。
- 音乐回忆录（StoryPage）slides 是特例：文案必须优雅、诗意，并在每个场景内押韵，但句尾不加句点，不要写成生硬的界面提示语。

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
