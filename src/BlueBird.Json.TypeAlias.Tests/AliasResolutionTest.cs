using Newtonsoft.Json.Serialization;

namespace BlueBird.Json.TypeAlias.Tests;

public sealed class AliasResolutionTest
{
    [Fact]
    public void ExplicitAlias_OverridesAttribute()
    {
        var binder = new TypeAliasRegistry()
            .Register<ExplicitAliasClass>("override")
            .BuildBinder();

        binder.BindToName(typeof(ExplicitAliasClass), out _, out string? typeName);

        Assert.Equal("override", typeName);
    }

    [Fact]
    public void AttributeAlias_UsedWhenNoExplicit()
    {
        var binder = new TypeAliasRegistry()
            .Register<ExplicitAliasClass>()
            .BuildBinder();

        binder.BindToName(typeof(ExplicitAliasClass), out _, out string? typeName);

        Assert.Equal("custom", typeName);
    }

    [Fact]
    public void TypeNameFallback_WhenNoAttributeAndNoExplicit()
    {
        var binder = new TypeAliasRegistry()
            .Register<NoAttributeClass>()
            .BuildBinder();

        binder.BindToName(typeof(NoAttributeClass), out _, out string? typeName);

        Assert.Equal("NoAttributeClass", typeName);
    }
}
