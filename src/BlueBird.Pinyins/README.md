# BlueBird.Pinyins

A zero-dependency .NET library for converting between Chinese characters and Pinyin. Supports Chinese-to-Pinyin, Pinyin-to-Chinese, and initial letter extraction.

## Quick Start

```csharp
using BlueBird.Pinyins;

// Chinese to Pinyin
Pinyin.GetPinyin("中国");         // "zhongguo"
Pinyin.GetPinyin("中国", " ");    // "zhong guo"

// Extract initial letters
Pinyin.GetInitials("你好");       // "nh"
Pinyin.GetInitials("你好", "-");  // "n-h"

// Pinyin to Chinese characters
Pinyin.GetChineseText("zhong");   // "中种重众钟..."
```

## API Reference

| Method | Parameters | Returns | Description |
|--------|------------|---------|-------------|
| `GetPinyin(string?, string?)` | text, separator (optional) | `string?` | Returns pinyin string, null input returns null |
| `GetInitials(string?, string?)` | text, separator (optional) | `string?` | Returns initial letters string, null input returns null |
| `GetPinyin(char)` | character | `string` | Returns pinyin for a single character |
| `GetChineseText(string?)` | pinyin | `string?` | Returns matching Chinese characters, null input returns null |

## Known Limitations

- No multi-pronunciation (多音字) support.
- Characters not in the pinyin data table (letters, digits, punctuation, etc.) are returned as-is.

## Architecture

### Data Storage

All pinyin data is embedded as C# source code — zero external dependencies. The library uses a **two-layer static index** structure:

```
┌─────────────────────────────────────────┐
│       PinyinData.Entries (400 entries)  │
│  ┌───────────────────────────────────┐  │
│  │ Pinyin: "a"                       │  │  PinyinEntry structs:
│  │ Chars:  "阿啊吖嗄腌..."           │  │
│  │───────────────────────────────────│  │  Each entry holds a pinyin syllable
│  │ Pinyin: "ai"                      │  │  and its matching characters
│  │ Chars:  "爱埃碍矮挨..."           │  │  as separate string fields
│  │───────────────────────────────────│  │
│  │ Pinyin: "zhong"                   │  │
│  │ Chars:  "中种重众钟..."           │  │
│  │───────────────────────────────────│  │
│  │ Pinyin: "zuo"                     │  │
│  │ Chars:  "作做左座坐..."           │  │
│  └───────────────────────────────────┘  │
└─────────────────────────────────────────┘

┌─────────────────────────────────────────┐
│      PinyinIndex.Buckets (1000 buckets)  │
│  ┌───────────────────────────────────┐  │
│  │ [0]  → {69, 83, 87, 108, ...}     │  │  Each bucket contains indices
│  │ [1]  → {1, 7, 22, 139,  ...}      │  │  into PinyinData.Entries
│  │ ...                               │  │
│  │ [999]→ {38, 68, 78, 79, ...}      │  │  Hash function: (uint)ch % 1000
│  └───────────────────────────────────┘  │
└─────────────────────────────────────────┘
```

### Chinese → Pinyin

```
Input: '中' (Unicode 20013)
  ↓
Hash bucket: 20013 % 1000 = 13
  ↓
Scan entries in bucket [13]:
  → Check if PinyinData.Entries[index].Characters contains '中'
  → Found → Return PinyinData.Entries[index].Pinyin ("zhong")
  → Not found → Return '中' as-is
```

Key design:
- **Hash bucketing**: Characters are distributed into 1000 buckets by Unicode codepoint, avoiding full-table scans.
- **Non-Chinese fallback**: Unmatched characters are returned as-is.

### Pinyin → Chinese

```
Input: "zhong"
  ↓
Normalize: Trim() + ToLowerInvariant()
  ↓
Sequential scan of PinyinData.Entries (400 entries):
  → Match condition: entry.Pinyin == "zhong"
  → Found → Return entry.Characters
  → Not found → Return empty string
```

Key design:
- **Exact match**: Pinyin comparison uses case-insensitive exact equality after normalization.

---

## 汉字与拼音转换工具库

### 快速开始

```csharp
using BlueBird.Pinyins;

// 汉字转拼音
Pinyin.GetPinyin("中国");         // "zhongguo"
Pinyin.GetPinyin("中国", " ");    // "zhong guo"

// 提取拼音首字母
Pinyin.GetInitials("你好");       // "nh"
Pinyin.GetInitials("你好", "-");  // "n-h"

// 拼音转汉字
Pinyin.GetChineseText("zhong");   // "中种重众钟..."
```

### API 说明

| 方法 | 参数 | 返回值 | 说明 |
|------|------|--------|------|
| `GetPinyin(string?, string?)` | 文本, 分隔符(可选) | `string?` | 返回拼音串，null 输入返回 null |
| `GetInitials(string?, string?)` | 文本, 分隔符(可选) | `string?` | 返回拼音首字母串，null 输入返回 null |
| `GetPinyin(char)` | 字符 | `string` | 返回单个字符的拼音 |
| `GetChineseText(string?)` | 拼音 | `string?` | 返回对应汉字列表，null 输入返回 null |

### 已知限制

- 不支持多音字。
- 不在拼音数据表中的字符（字母、数字、标点等）按原样返回。

### 技术原理

#### 数据存储结构

本库采用 **双层静态索引表** 结构，零外部依赖，所有数据内嵌为 C# 源代码。

```
┌─────────────────────────────────────────┐
│     PinyinData.Entries (400 条)         │
│  ┌───────────────────────────────────┐  │
│  │ Pinyin: "a"                       │  │  PinyinEntry 结构体：
│  │ Chars:  "阿啊吖嗄腌..."           │  │
│  │───────────────────────────────────│  │  每条包含独立的拼音字符串
│  │ Pinyin: "ai"                      │  │  和对应的汉字字符串
│  │ Chars:  "爱埃碍矮挨..."           │  │
│  │───────────────────────────────────│  │
│  │ Pinyin: "zhong"                   │  │
│  │ Chars:  "中种重众钟..."           │  │
│  │───────────────────────────────────│  │
│  │ Pinyin: "zuo"                     │  │
│  │ Chars:  "作做左座坐..."           │  │
│  └───────────────────────────────────┘  │
└─────────────────────────────────────────┘

┌─────────────────────────────────────────┐
│    PinyinIndex.Buckets (1000 个桶)       │
│  ┌───────────────────────────────────┐  │
│  │ [0]  → {69, 83, 87, 108, ...}     │  │  每个桶包含指向 PinyinData.Entries
│  │ [1]  → {1, 7, 22, 139,  ...}      │  │  的索引
│  │ ...                               │  │
│  │ [999]→ {38, 68, 78, 79, ...}      │  │  哈希函数: (uint)ch % 1000
│  └───────────────────────────────────┘  │
└─────────────────────────────────────────┘
```

#### 汉字转拼音

```
输入: '中' (Unicode 20013)
  ↓
计算哈希桶: 20013 % 1000 = 13
  ↓
扫描桶 [13] 中每个索引:
  → 检查 PinyinData.Entries[index].Characters 是否包含 '中'
  → 找到 → 返回 PinyinData.Entries[index].Pinyin ("zhong")
  → 未找到 → 遍历完桶后原样返回 '中'
```

关键设计：
- **哈希分桶**：用字符的 Unicode 码点对 1000 取模，将常见汉字分布到 1000 个桶中，避免全表扫描
- **非汉字回退**：未命中任何条目时，直接返回字符本身（适用于字母、数字、标点等）

#### 拼音转汉字

```
输入: "zhong"
  ↓
标准化: Trim() + ToLowerInvariant()
  ↓
顺序扫描 PinyinData.Entries (400 条):
  → 匹配条件: entry.Pinyin == "zhong"
  → 找到 → 返回 entry.Characters
  → 未找到 → 返回空字符串
```

关键设计：
- **精确匹配**：拼音标准化后使用大小写不敏感的精确等值比较。
