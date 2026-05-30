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
Pinyin.GetChineseText("zhong");   // "中忠钟终盅..."
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
│           PyCode.Codes (400 entries)    │
│  ┌───────────────────────────────────┐  │
│  │ "a     :阿啊吖嗄腌..."            │  │  Fixed format:
│  │ "ai    :爱埃碍矮挨..."            │  │  [6-char pinyin + 1-char sep + N chars]
│  │ "zhong :中忠钟终盅..."            │  │
│  │ "zuo   :作做左座坐..."            │  │  Separator: ":" for short pinyins, space for others
│  └───────────────────────────────────┘  │
└─────────────────────────────────────────┘

┌─────────────────────────────────────────┐
│         PyHash.Hashes (1000 buckets)    │
│  ┌───────────────────────────────────┐  │
│  │ [0]  → {69, 83, 87, 108, ...}     │  │  Each bucket contains indices into PyCode
│  │ [1]  → {1, 7, 22, 139,  ...}      │  │
│  │ ...                               │  │  Hash function: (uint)ch % 1000
│  │ [999]→ {38, 68, 78, 79, ...}      │  │
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
  → Search for '中' in the character portion of each entry
  → Found → Extract first 6 chars "zhong ", TrimEnd → "zhong"
  → Not found → Return '中' as-is
```

Key design:
- **Hash bucketing**: Characters are evenly distributed into 1000 buckets by Unicode codepoint, avoiding full-table scans.
- **Non-Chinese fallback**: Unmatched characters are returned as-is.

### Pinyin → Chinese

```
Input: "zhong"
  ↓
Normalize: Trim() + ToLower()
  ↓
Sequential scan of PyCode.Codes (400 entries):
  → Match condition: StartsWith("zhong ") or StartsWith("zhong:")
  → Found → Extract character portion after position 7, return
  → Not found → Return empty string
```

Key design:
- **Fixed-width pinyin field**: Pinyin is always padded to 6 characters (right-padded with spaces).

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
Pinyin.GetChineseText("zhong");   // "中忠钟终盅..."
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
│           PyCode.Codes (400 条)         │
│  ┌───────────────────────────────────┐  │
│  │ "a     :阿啊吖嗄腌..."            │  │  每条固定格式：
│  │ "ai    :爱埃碍矮挨..."            │  │  [6位拼音 + 1位分隔符 + N个汉字]
│  │ "zhong :中忠钟终盅..."            │  │
│  │ "zuo   :作做左座坐..."            │  │  分隔符: 为短拼音用":"，其余用空格
│  └───────────────────────────────────┘  │
└─────────────────────────────────────────┘

┌─────────────────────────────────────────┐
│         PyHash.Hashes (1000 个桶)       │
│  ┌───────────────────────────────────┐  │
│  │ [0]  → {69, 83, 87, 108, ...}     │  │  每个桶包含指向 PyCode 的索引
│  │ [1]  → {1, 7, 22, 139,  ...}      │  │
│  │ ...                               │  │  哈希函数: (uint)ch % 1000
│  │ [999]→ {38, 68, 78, 79, ...}      │  │
│  └───────────────────────────────────┘  │
└─────────────────────────────────────────┘
```

#### 汉字转拼音

```
输入: '中' (Unicode 20013)
  ↓
计算哈希桶: 20013 % 1000 = 13
  ↓
扫描桶 [13] 中每个 PyCode 索引
  → 在索引对应条目的汉字部分查找 '中'
  → 找到 → 提取前6位 "zhong "，TrimEnd → "zhong"
  → 未找到 → 遍历完桶后原样返回 '中'
```

关键设计：
- **哈希分桶**：用字符的 Unicode 码点对 1000 取模，将常见汉字均匀分布到 1000 个桶中，避免全表扫描
- **非汉字回退**：未命中任何条目时，直接返回字符本身（适用于字母、数字、标点等）

#### 拼音转汉字

```
输入: "zhong"
  ↓
标准化: Trim() + ToLower()
  ↓
顺序扫描 PyCode.Codes (400条):
  → 匹配条件: StartsWith("zhong ") 或 StartsWith("zhong:")
  → 找到 → 提取第7位之后的汉字部分，返回
  → 未找到 → 返回空字符串
```

关键设计：
- **定长分隔符**：拼音字段固定6字符（不足右侧补空格）
