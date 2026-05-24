# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with this repository.

## Project Overview

**BlueBird.Pinyins** — a zero-dependency .NET library for converting between Chinese characters and Pinyin. Targets .NET 8.0 and .NET 10.0. MIT licensed. NuGet package includes `BlueBird.png` icon and Chinese readme. Namespace: `BlueBird.Pinyins`. Repository: <https://github.com/BlueBird360/BlueBird>.

## Repository Structure

```
src/BlueBird.Pinyins/            — library project (strong-name signed with BlueBird.snk)
  Pinyin.cs                      — public API: GetPinyin, GetInitials, GetChineseText
  PyCode.cs                      — embedded pinyin→character mapping table
  PyHash.cs                      — hash bucket index for fast char→pinyin lookup
  Readme.md                      — packaged as NuGet readme
src/BlueBird.Pinyins.Tests/      — xUnit v3 test project
  PinyinTest.cs                  — tests for GetPinyin, GetInitials, GetChineseText
BlueBird.snk                     — strong-name key file
BlueBird.png                     — NuGet package icon
```

## Key Commands

```bash
# Build the solution
dotnet build BlueBird.slnx

# Run tests (use dotnet run, not dotnet test — see note below)
dotnet run --project src/BlueBird.Pinyins.Tests/BlueBird.Pinyins.Tests.csproj -f net10.0
dotnet run --project src/BlueBird.Pinyins.Tests/BlueBird.Pinyins.Tests.csproj -f net8.0

# Or run the built test executable directly
src/BlueBird.Pinyins.Tests/bin/Release/net10.0/BlueBird.Pinyins.Tests.exe

# Build in Release mode (generates XML docs, ready for NuGet pack)
dotnet build -c Release BlueBird.slnx
```

> **Note**: `dotnet test` fails with dotnet SDK 10.0.300 due to a `testhost.dll` path resolution bug. Use `dotnet run --project` or run the test executable directly instead.

## Architecture

### Public API (`Pinyin.cs`)

| Method | Input | Output | Notes |
|--------|-------|--------|-------|
| `GetPinyin(string?, string? separator)` | Chinese text | Pinyin string | null→null; non-Chinese chars returned as-is |
| `GetInitials(string?, string? separator)` | Chinese text | First-letter string | null→null; same fallback for non-Chinese |
| `GetChineseText(string?)` | Pinyin string | Chinese characters | null→null; no match→`string.Empty`; case-insensitive |

### Data Structure: Two-Layer Static Index

All pinyin data is embedded as C# source code — no external dependencies.

- **`PyCode.Codes`** — array of fixed-format strings: `6-char pinyin (space-padded) + 1-char separator + 汉字列表`. Separator is `:` for short pinyins, space for others.
- **`PyHash.Hashes`** — 500 hash buckets. Hash function: `(short)((uint)ch % 500)`. Each bucket contains indices into `PyCode.Codes`.

### Lookup Flow

- **Char → Pinyin**: `GetHashIndex(ch)` → bucket → scan entries for char position ≥ 7 → extract `Substring(0,6).TrimEnd()`. Falls back to `ch.ToString()` if not found.
- **Pinyin →汉字**: Sequential scan of `PyCode.Codes`, matching `StartsWith(key + " ")` or `StartsWith(key + ":")`. Returns first match's `Substring(7)`. Case-insensitive via `ToLower()` on input.

### Multi-Targeting

Library targets `net8.0` and `net10.0`. Tests target `net10.0` only. Nullable references enabled.

### Known Limitations

- No multi-pronunciation (多音字) support — returns the first match only.
- No traditional Chinese (繁体字) support.
- Non-Chinese characters are returned as-is (e.g., `'A'` → `"A"`).
- `GetChineseText` only returns the first matching entry; subsequent matches are ignored.
