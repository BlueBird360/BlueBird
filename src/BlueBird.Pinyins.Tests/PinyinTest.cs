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
        Assert.Equal("zhong", Pinyin.GetPinyin('中'));
        Assert.Equal("guo", Pinyin.GetPinyin('国'));
        Assert.Equal("ai", Pinyin.GetPinyin('爱'));
    }

    [Fact]
    public void MultipleChineseChars_WithoutSeparator()
    {
        Assert.Equal("zhongguo", Pinyin.GetPinyin("中国"));
        Assert.Equal("nihao", Pinyin.GetPinyin("你好"));
    }

    [Fact]
    public void MultipleChineseChars_WithSeparator()
    {
        Assert.Equal("zhong guo", Pinyin.GetPinyin("中国", " "));
        Assert.Equal("ni-hao", Pinyin.GetPinyin("你好", "-"));
        Assert.Equal("A-B-C", Pinyin.GetPinyin("ABC", "-"));
    }

    [Fact]
    public void MixedChineseAndNonChinese()
    {
        Assert.Equal("zhongguo123", Pinyin.GetPinyin("中国123"));
        Assert.Equal("zhongguoA", Pinyin.GetPinyin("中国A"));
    }

    [Fact]
    public void NonChineseChars_ReturnsOriginalChar()
    {
        Assert.Equal("A", Pinyin.GetPinyin('A'));
        Assert.Equal("1", Pinyin.GetPinyin('1'));
        Assert.Equal(" ", Pinyin.GetPinyin(' '));
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
    public void SingleChar_ReturnsFirstLetter()
    {
        Assert.Equal("z", Pinyin.GetInitials("中"));
        Assert.Equal("a", Pinyin.GetInitials("爱"));
    }

    [Fact]
    public void MultipleChars_WithoutSeparator()
    {
        Assert.Equal("zg", Pinyin.GetInitials("中国"));
        Assert.Equal("nh", Pinyin.GetInitials("你好"));
    }

    [Fact]
    public void MultipleChars_WithSeparator()
    {
        Assert.Equal("z-g", Pinyin.GetInitials("中国", "-"));
        Assert.Equal("n|h", Pinyin.GetInitials("你好", "|"));
    }

    [Fact]
    public void WithEmptySeparator()
    {
        Assert.Equal("zg", Pinyin.GetInitials("中国", string.Empty));
    }

    [Fact]
    public void NonChinese_ReturnsCharAsInitial()
    {
        Assert.Equal("A", Pinyin.GetInitials("A"));
        Assert.Equal("AB", Pinyin.GetInitials("AB"));
    }
}

public sealed class GetChineseTextTest
{
    [Fact]
    public void Null_ReturnsNull()
    {
        Assert.Null(Pinyin.GetChineseText(null));
    }

    [Fact]
    public void EmptyString_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, Pinyin.GetChineseText(string.Empty));
    }

    [Fact]
    public void LowercasePinyin_ReturnsChars()
    {
        string result = Pinyin.GetChineseText("ai");
        Assert.NotEmpty(result);
        Assert.Contains('爱', result);
        Assert.Contains('埃', result);
    }

    [Fact]
    public void UppercasePinyin_ReturnsChars()
    {
        string result = Pinyin.GetChineseText("AI")!;
        Assert.NotEmpty(result);
        Assert.Contains('爱', result);
        Assert.Contains('埃', result);
    }

    [Fact]
    public void PinyinWithWhitespace_ReturnsChars()
    {
        string result = Pinyin.GetChineseText("  zhong  ");
        Assert.NotEmpty(result);
        Assert.Contains('中', result);
    }

    [Fact]
    public void InvalidPinyin_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, Pinyin.GetChineseText("xyzxyz"));
    }

    [Fact]
    public void CommonPinyins_ReturnExpectedChars()
    {
        Assert.Contains('中', Pinyin.GetChineseText("zhong"));
        Assert.Contains('国', Pinyin.GetChineseText("guo"));
        Assert.Contains('你', Pinyin.GetChineseText("ni"));
        Assert.Contains('好', Pinyin.GetChineseText("hao"));
        Assert.Contains('阿', Pinyin.GetChineseText("a"));
    }
}
