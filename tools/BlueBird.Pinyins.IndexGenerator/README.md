# BlueBird.Pinyins.IndexGenerator

Regenerates `PinyinIndex.cs` from the entries in `PinyinData.cs`.

```bash
dotnet run --project tools/BlueBird.Pinyins.IndexGenerator -c Release
```

The generator validates Pinyin ordering, the entry-index size limit, and duplicate character mappings.
