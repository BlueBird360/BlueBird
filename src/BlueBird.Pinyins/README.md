# BlueBird.Pinyins

A lightweight, zero-dependency .NET library for converting Chinese characters to Pinyin, extracting initials, and finding characters by Pinyin.

## Installation

```bash
dotnet add package BlueBird.Pinyins
```

## Quick Start

```csharp
using System.Text;
using BlueBird.Pinyins;

Pinyin.GetPinyin("中国");          // "zhongguo"
Pinyin.GetPinyin("中国", " ");     // "zhong guo"

Pinyin.GetInitials("你好");        // "nh"
Pinyin.GetInitials("你好", "-");   // "n-h"

Pinyin.GetPinyin(new Rune('中'));  // "zhong"
Pinyin.TryGetPinyin(new Rune('中'), out string? pinyin); // true

Pinyin.GetCharacters("zhong");    // "中种重众钟..."
```

## API

| Method | Description |
|--------|-------------|
| `GetPinyin(string?, string?)` | Converts text to Pinyin. An optional separator is inserted between the result for each input character. |
| `GetInitials(string?, string?)` | Extracts Pinyin initials. An optional separator is inserted between the result for each input character. |
| `GetPinyin(Rune)` | Returns the Pinyin for one Unicode character, or the original character when it is not found. |
| `TryGetPinyin(Rune, out string?)` | Attempts to find the Pinyin for one Unicode character. |
| `GetCharacters(string?)` | Returns the characters matching a complete, tone-free Pinyin syllable. |

The string APIs return `null` when their input is `null`. `GetCharacters` trims surrounding whitespace, ignores case, and returns an empty string when no match is found.

## Limitations

- Each character maps to one default tone-free Pinyin reading. Context-dependent pronunciations and tones are not supported.
- The library uses `v` for `ü`, as in `lv`, `nv`, `lve`, and `nve`.
- When converting text, characters not included in the Pinyin data are returned unchanged.

## Upgrading from 1.2

Version 1.3 renames `GetChineseText` to `GetCharacters` and replaces `GetPinyin(char)` with the Unicode-safe `GetPinyin(Rune)` API.

## Implementation

- No runtime dependencies; all Pinyin data is embedded in the assembly.
- Text is processed with `Rune` to preserve Unicode surrogate pairs.

## License

MIT
