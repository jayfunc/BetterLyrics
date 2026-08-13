# BetterLyrics.Sdk

The BetterLyrics SDK provides the essential interfaces, base classes, and services required to build plugins for BetterLyrics. By implementing the provided interfaces, you can extend the app with custom lyrics sources, translators, and transliterators.

## Compatibility

| BetterLyrics version | BetterLyrics.Sdk version |
| --- | --- |
| <=1.3.479 | 1.0.0 |
| >1.3.497 | 1.0.3 |

> We recommend using the latest version of both BetterLyrics and BetterLyrics.Sdk to ensure you have access to the latest features and improvements.

---

## Core Interfaces & Base Classes

### `IPlugin`
The entry point for all plugins. Every plugin MUST implement `IPlugin`.
It defines the metadata of your plugin (Title, Description, Author, Id, Version, etc.) and provides the lifecycle hooks `InitializeAsync()` and `DisposeAsync()`.

### `PluginBase<TConfig>` (Recommended)
An abstract base class that implements `IPlugin` and significantly simplifies the boilerplate code for handling plugin settings. 
Instead of manually mapping configuration dictionaries, you can define a strongly typed `TConfig` class. `PluginBase` uses Reflection and the `SettingBuilder` to automatically generate the settings UI in BetterLyrics and automatically bind configuration changes to your properties.

---

## Plugin Capabilities (Feature Interfaces)

To add capabilities to BetterLyrics, your plugin class should implement one or more of the following feature interfaces:

### `ILyricsSource`
Implement this to provide a custom lyrics search and fetching engine.
- **`GetLyricsAsync(title, artist, album, duration, token)`**: 
  Called by the host when searching for lyrics. You must return a `LyricsSearchResult` record which can contain the Raw lyrics text, Translation, Transliteration, and a Reference URL.

### `ILyricsTranslator`
Implement this to provide lyrics translation services.
- **`GetTranslationAsync(text, targetLangTag)`**: 
  Translates the given lyrics string into the language specified by the `LanguageTag`.

### `ILyricsTransliterator`
Implement this to provide transliteration (e.g., converting Japanese Kanji to Romaji, or Chinese characters to Pinyin).
- **`GetTransliterationAsync(text, targetLangTag, token)`**: 
  Returns the transliterated string.

---

## SDK Services & Context (`IPluginContext`)

When your plugin's `InitializeAsync(IPluginContext context)` is called, the host passes an `IPluginContext` which provides access to several built-in services:

### `ILocalizer`
Used for internationalization and retrieving localized strings. 
- You can access language strings via `Localizer["Key"]`. 
- Provides the `CurrentLanguage` property.

### `IAIService`
Provides access to the built-in AI capabilities of BetterLyrics.
- **`ChatAsync(systemPrompt, userPrompt)`**: Allows your plugin to leverage LLM generation natively.

### `IConfigurator`
Allows raw access to read and write plugin settings. 
- *Note: If you inherit from `PluginBase<TConfig>`, you rarely need to use this directly, as `PluginBase` manages settings injection for you.*

---

## Settings & UI Schema

BetterLyrics automatically generates a settings UI for your plugin without you writing any XAML. You do this by returning a dictionary of `SettingDef` from `GetSettingDefDict()`. 

If you use `PluginBase<TConfig>`, you can define properties in your config class and use the `SettingBuilder` helpers to expose them:

- **`BoolSettingDef`** (ToggleSwitch): Created via `SettingBuilder.Bool()`
- **`TextSettingDef`** (TextBox): Created via `SettingBuilder.Text()` or `SettingBuilder.Password()` (for masked input)
- **`NumberSettingDef`** (NumberBox): Created via `SettingBuilder.Number()`
- **`ChoiceSettingDef`** (ComboBox): Created via `SettingBuilder.Choice()`
- **`ActionSettingDef`** (Button): Created via `SettingBuilder.Action()`
