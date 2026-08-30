using System.Text;

namespace BlueBird.Pinyins.Tests;

public sealed class GetPinyinTest
{
    [Fact]
    public void Null_ReturnsNull()
    {
        Assert.Null(Pinyin.GetPinyin(null));
    }

    [Fact]
    public void EmptyString_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, Pinyin.GetPinyin(string.Empty));
    }

    [Fact]
    public void SingleChineseChar_ReturnsPinyin()
    {
        Assert.Equal("zhong", Pinyin.GetPinyin(new Rune('中')));
        Assert.Equal("guo", Pinyin.GetPinyin(new Rune('国')));
    }

    [Fact]
    public void MultipleChineseChars_WithoutSeparator_ReturnsPinyin()
    {
        Assert.Equal("zhongguo", Pinyin.GetPinyin("中国"));
        Assert.Equal("nihao", Pinyin.GetPinyin("你好"));
    }

    [Fact]
    public void MultipleChars_WithSeparator_InsertsSeparatorBetweenRunes()
    {
        Assert.Equal("zhong guo", Pinyin.GetPinyin("中国", " "));
        Assert.Equal("A-B-C", Pinyin.GetPinyin("ABC", "-"));
    }

    [Fact]
    public void MixedChineseAndNonChinese_PreservesNonChineseChars()
    {
        Assert.Equal("zhongguo123A", Pinyin.GetPinyin("中国123A"));
        Assert.Equal("ni，hao!", Pinyin.GetPinyin("你，好!"));
    }

    [Fact]
    public void NonChineseRune_ReturnsOriginalRune()
    {
        Assert.Equal("A", Pinyin.GetPinyin(new Rune('A')));
        Assert.Equal("😀", Pinyin.GetPinyin(new Rune(0x1F600)));
    }

    [Fact]
    public void SupplementaryUnicodeCharacters_WithSeparator_RemainIntact()
    {
        Assert.Equal("😀-zhong", Pinyin.GetPinyin("😀中", "-"));
        Assert.Equal("𠀀-A", Pinyin.GetPinyin("𠀀A", "-"));
    }
}

public sealed class TryGetPinyinTest
{
    [Fact]
    public void KnownCharacter_ReturnsTrueAndPinyin()
    {
        bool found = Pinyin.TryGetPinyin(new Rune('中'), out string? pinyin);

        Assert.True(found);
        Assert.Equal("zhong", pinyin);
    }

    [Fact]
    public void UnknownCharacters_ReturnFalseAndNull()
    {
        Assert.False(Pinyin.TryGetPinyin(new Rune('A'), out string? bmpPinyin));
        Assert.Null(bmpPinyin);

        Assert.False(Pinyin.TryGetPinyin(new Rune(0x20000), out string? supplementaryPinyin));
        Assert.Null(supplementaryPinyin);
    }

    [Fact]
    public void FirstAndLastMappedCodePoints_ReturnExpectedPinyin()
    {
        Assert.True(Pinyin.TryGetPinyin(new Rune(0x4E00), out string? firstPinyin));
        Assert.Equal("yi", firstPinyin);

        Assert.True(Pinyin.TryGetPinyin(new Rune(0x9FA0), out string? lastPinyin));
        Assert.Equal("yue", lastPinyin);
    }
}

public sealed class GetInitialsTest
{
    [Fact]
    public void Null_ReturnsNull()
    {
        Assert.Null(Pinyin.GetInitials(null));
    }

    [Fact]
    public void EmptyString_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, Pinyin.GetInitials(string.Empty));
    }

    [Fact]
    public void SingleChineseChar_ReturnsInitial()
    {
        Assert.Equal("z", Pinyin.GetInitials("中"));
        Assert.Equal("a", Pinyin.GetInitials("爱"));
    }

    [Fact]
    public void MultipleChineseChars_ReturnInitials()
    {
        Assert.Equal("zg", Pinyin.GetInitials("中国"));
        Assert.Equal("nh", Pinyin.GetInitials("你好"));
    }

    [Fact]
    public void Separator_InsertsSeparatorBetweenRunes()
    {
        Assert.Equal("z-g", Pinyin.GetInitials("中国", "-"));
        Assert.Equal("zg", Pinyin.GetInitials("中国", string.Empty));
    }

    [Fact]
    public void MixedChineseAndNonChinese_PreservesNonChineseChars()
    {
        Assert.Equal("n，h!2", Pinyin.GetInitials("你，好!2"));
    }

    [Fact]
    public void SupplementaryUnicodeCharacters_WithSeparator_RemainIntact()
    {
        Assert.Equal("😀-z", Pinyin.GetInitials("😀中", "-"));
        Assert.Equal("𠀀-A", Pinyin.GetInitials("𠀀A", "-"));
    }
}

public sealed class GetCharactersTest
{
    [Fact]
    public void Null_ReturnsNull()
    {
        Assert.Null(Pinyin.GetCharacters(null));
    }

    [Fact]
    public void EmptyOrWhitespace_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, Pinyin.GetCharacters(string.Empty));
        Assert.Equal(string.Empty, Pinyin.GetCharacters(" \t\r\n"));
    }

    [Fact]
    public void LowercasePinyin_ReturnsCharacters()
    {
        string characters = Pinyin.GetCharacters("ai")!;

        Assert.Contains('爱', characters);
        Assert.Contains('埃', characters);
    }

    [Fact]
    public void PinyinComparison_IgnoresCase()
    {
        Assert.Contains('中', Pinyin.GetCharacters("ZhOnG"));
    }

    [Fact]
    public void PinyinWithLeadingAndTrailingWhitespace_ReturnsCharacters()
    {
        Assert.Contains('中', Pinyin.GetCharacters("\t zhong \r\n"));
    }

    [Fact]
    public void InvalidOrInexactPinyin_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, Pinyin.GetCharacters("xyzxyz"));
        Assert.Equal(string.Empty, Pinyin.GetCharacters("zho"));
        Assert.Equal(string.Empty, Pinyin.GetCharacters("zho ng"));
        Assert.Equal(string.Empty, Pinyin.GetCharacters("zhōng"));
    }

    [Fact]
    public void CommonPinyins_ReturnExpectedSimplifiedAndTraditionalCharacters()
    {
        Assert.Contains('中', Pinyin.GetCharacters("zhong"));
        Assert.Contains('国', Pinyin.GetCharacters("guo"));
        Assert.Contains('爱', Pinyin.GetCharacters("ai"));
        Assert.Contains('愛', Pinyin.GetCharacters("ai"));
    }

    [Fact]
    public void UmlautVConvention_ReturnsExpectedCharacters()
    {
        Assert.Contains('绿', Pinyin.GetCharacters("lv"));
        Assert.Contains('女', Pinyin.GetCharacters("nv"));
        Assert.Contains('略', Pinyin.GetCharacters("lve"));
        Assert.Contains('虐', Pinyin.GetCharacters("nve"));
    }

    [Fact]
    public void ReturnedCharacters_MapBackToRequestedPinyin()
    {
        string characters = Pinyin.GetCharacters("zhong")!;

        foreach (Rune character in characters.EnumerateRunes())
        {
            Assert.Equal("zhong", Pinyin.GetPinyin(character));
        }
    }
}
