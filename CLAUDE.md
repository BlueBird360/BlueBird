# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**BlueBird.Pinyins** — a zero-dependency .NET library for converting between Chinese characters and Pinyin. Targets `net8.0` and `net10.0`. MIT licensed. Strong-name signed with `BlueBird.snk`. NuGet package includes `BlueBird.png` icon and Chinese readme. Namespace: `BlueBird.Pinyins`. Repository: <https://github.com/BlueBird360/BlueBird>.

**BlueBird.Json.TypeAlias** — Newtonsoft.Json extension library providing type alias support for JSON serialization. Targets `net8.0` and `net10.0`. Strong-name signed with `BlueBird.snk`. Assembly name: `BlueBird.Json.TypeAlias`. Namespace: `Newtonsoft.Json` / `Newtonsoft.Json.Serialization`. Depends on `Newtonsoft.Json` v13.0.1. **Included in the solution file**.

## Repository Structure

```
src/BlueBird.Pinyins/                   — library (strong-name signed, BlueBird.snk)
  Pinyin.cs                             — public API: GetPinyin, GetInitials, GetChineseText
  PinyinData.cs                         — PinyinEntry[] with ~400 pinyin→character mappings
  PinyinIndex.cs                        — 1000-bucket hash index for fast char→pinyin lookup
  README.md                             — packaged as NuGet readme (bilingual EN/ZH)
src/BlueBird.Pinyins.Tests/             — xUnit v3 test project (net8.0 + net10.0)
  PinyinTest.cs                         — tests for all three public methods
src/BlueBird.Json.TypeAlias/            — Newtonsoft.Json type alias extension (net8.0 + net10.0, BlueBird.snk)
  JsonTypeAliasAttribute.cs
  JsonDeserializationAliasAttribute.cs
  TypeAliasRegistry.cs
  TypeAliasSerializationBinder.cs
src/BlueBird.Json.TypeAlias.Tests/      — xUnit v3 test project (net8.0 + net10.0)
  Models/Animal.cs                      — test model classes (Animal, Dog, Cat, Bird, Fish, Shape, Circle...)
  TestHelper.cs                         — CreateSettings helper
  TypeAliasRegistryTest.cs              — registration, alias resolution, validation, null checks
  PolymorphicSerializationTest.cs       — polymorphic serialization/deserialization
  AbstractBaseTypeTest.cs               — abstract base type scenarios
  BinderFallbackTest.cs                 — fallback to default binder
  AliasResolutionTest.cs                — alias resolution priority
  RoundTripTest.cs                      — round-trip, compactness, reusability
  RegisterDeserializationAliasTest.cs        — backward-compatible alias migration, null checks
  JsonDeserializationAliasAttributeTest.cs — attribute-based deserialization aliases
BlueBird.slnx                           — solution file (all four projects)
BlueBird.snk                            — strong-name key file (shared by all projects)
BlueBird.png                            — NuGet package icon
```

## Key Commands

```bash
# Build the solution
dotnet build BlueBird.slnx

# Run tests (use dotnet run, not dotnet test — see note below)
dotnet run --project src/BlueBird.Pinyins.Tests/BlueBird.Pinyins.Tests.csproj -f net10.0
dotnet run --project src/BlueBird.Pinyins.Tests/BlueBird.Pinyins.Tests.csproj -f net8.0
dotnet run --project src/BlueBird.Json.TypeAlias.Tests/BlueBird.Json.TypeAlias.Tests.csproj -f net10.0
dotnet run --project src/BlueBird.Json.TypeAlias.Tests/BlueBird.Json.TypeAlias.Tests.csproj -f net8.0

# Or run the built test executable directly
src/BlueBird.Pinyins.Tests/bin/Release/net10.0/BlueBird.Pinyins.Tests.exe

# Build in Release mode (generates XML docs, ready for NuGet pack)
dotnet build -c Release BlueBird.slnx

# Run a single test by method name (xUnit v3 supports name filters as CLI args)
dotnet run --project src/BlueBird.Pinyins.Tests/BlueBird.Pinyins.Tests.csproj -f net10.0 -- Null_ReturnsNull
```

> **CRITICAL**: `dotnet test` fails with dotnet SDK 10.0.300 due to a `testhost.dll` path resolution bug. Always use `dotnet run --project` or run the test executable directly.

Tests use xUnit v3 with `<OutputType>Exe` — they run as self-hosted executables, not via the test runner.

## Architecture

### BlueBird.Pinyins — Public API (`Pinyin.cs`)

| Method | Input | Output | Notes |
|--------|-------|--------|-------|
| `GetPinyin(string?, string? separator)` | Chinese text | Pinyin string | null→null; non-Chinese chars returned as-is |
| `GetPinyin(char)` | Single character | Pinyin string | Falls back to `ch.ToString()` if not found |
| `GetInitials(string?, string? separator)` | Chinese text | First-letter string | null→null; same fallback for non-Chinese |
| `GetChineseText(string?)` | Pinyin string | Chinese characters | null→null; no match→`string.Empty`; case-insensitive |

### Data Structure: Two-Layer Static Index

All pinyin data is embedded as C# source code — no external dependencies.

- **`PinyinData.Entries`** — array of `PinyinEntry` structs (internal readonly struct with `Pinyin` and `Characters` string properties). ~400 entries, one per pinyin syllable.
- **`PinyinIndex.Buckets`** — 1000 hash buckets (`short[][]`). Hash function: `(short)((uint)ch % PinyinIndex.Buckets.Length)`. Each bucket contains indices into `PinyinData.Entries`.

### Lookup Flow

- **Char → Pinyin**: `GetBucketIndex(ch)` → bucket → scan entries: `PinyinData.Entries[index].Characters.Contains(ch)` → return `PinyinData.Entries[index].Pinyin`. Falls back to `ch.ToString()` if not found.
- **Pinyin → Characters**: `pinyin.Trim().ToLowerInvariant()` → sequential scan of `PinyinData.Entries`, matching `entry.Pinyin == key`. Returns first match's `entry.Characters`.

### BlueBird.Json.TypeAlias

Three main components (registration → build → bind):

- **`JsonTypeAliasAttribute`** — `[AttributeUsage(Class)]` that defines a short alias for a type. In namespace `Newtonsoft.Json`. When alias is null, the class name is used.
- **`JsonDeserializationAliasAttribute`** — `[AttributeUsage(Class, AllowMultiple = true)]` that defines additional deserialization-only aliases. In namespace `Newtonsoft.Json`. Automatically read by `Register()` and `RegisterAssembly()`.
- **`TypeAliasRegistry`** — mutable registry used during startup to collect type-alias mappings. Supports `Register<T>()`, `Register(Type)`, `Register(IEnumerable<Type>)`, `RegisterAssembly(Assembly)`, `RegisterDeserializationAlias<T>(alias)` / `RegisterDeserializationAlias<T>(IEnumerable<string>)` / `RegisterDeserializationAlias(Type, alias)` / `RegisterDeserializationAlias(Type, IEnumerable<string>)` for backward-compatible alias migration. All return `this` for fluent chaining. Call `BuildBinder()` to produce an immutable binder. In namespace `Newtonsoft.Json.Serialization`.
- **`TypeAliasSerializationBinder`** — immutable `ISerializationBinder` returned by `TypeAliasRegistry.BuildBinder()`. Uses `FrozenDictionary` for lock-free, high-performance lookups. Thread-safe for concurrent reads. Cannot be modified after construction.

## Package Publishing

- **Pinyins**: `dotnet build -c Release BlueBird.slnx` then `dotnet pack src/BlueBird.Pinyins/`. Icon: `BlueBird.png`, readme: `src/BlueBird.Pinyins/README.md`. Version is set in csproj (currently 1.2.0).
- **Json.TypeAlias**: `dotnet build -c Release BlueBird.slnx` then `dotnet pack src/BlueBird.Json.TypeAlias/`. Icon: `BlueBird.png`, readme: `src/BlueBird.Json.TypeAlias/README.md`. Version is set in csproj (currently 1.0.0).

## Known Limitations (design, not bugs)

- No multi-pronunciation (多音字) support — returns the first match only.
- Traditional Chinese (繁体字) is partially included — some entries contain both simplified and traditional variants, but coverage is not comprehensive.
- Non-Chinese characters are returned as-is (e.g., `'A'` → `"A"`).
- `GetChineseText` only returns the first matching entry; subsequent matches are ignored.
