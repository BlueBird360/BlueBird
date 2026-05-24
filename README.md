# BlueBird

A collection of practical .NET NuGet packages. Each package is independently published, and is ready to use out of the box.

## Packages

### BlueBird.Pinyins

Chinese character ↔ Pinyin conversion library. Supports Chinese-to-Pinyin, Pinyin-to-Chinese, and initial letter extraction.

```csharp
Pinyin.GetPinyin("中国");           // "zhongguo"
Pinyin.GetInitials("你好", "-");    // "n-h"
Pinyin.GetChineseText("zhong");     // "中忠钟终盅..."
```

See [src/BlueBird.Pinyins/README.md](src/BlueBird.Pinyins/README.md) for details.

---

## 包列表

BlueBird 是一组实用 .NET NuGet 包的集合，每个包独立发布，开箱即用。

### BlueBird.Pinyins

汉字与拼音转换工具库。支持汉字转拼音、拼音转汉字、拼音首字母提取。

```csharp
Pinyin.GetPinyin("中国");           // "zhongguo"
Pinyin.GetInitials("你好", "-");    // "n-h"
Pinyin.GetChineseText("zhong");     // "中忠钟终盅..."
```

详细说明参见 [src/BlueBird.Pinyins/README.md](src/BlueBird.Pinyins/README.md)。
