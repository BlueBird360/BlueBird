using System;
using System.Collections.Generic;
using BlueBird.Json.TypeAlias;

namespace BlueBird.Json.TypeAlias.Tests;

public sealed class TypeAliasRegistryTest
{
    [Fact]
    public void Register_ResolvesAliasByExplicitAttributeAndTypeNamePrecedence()
    {
        var binder = new TypeAliasRegistry()
            .Register<Dog>("canine")
            .Register<Cat>()
            .Register<NoAttributeClass>()
            .BuildBinder();

        binder.BindToName(typeof(Dog), out _, out string? explicitAlias);
        binder.BindToName(typeof(Cat), out _, out string? attributeAlias);
        binder.BindToName(typeof(NoAttributeClass), out _, out string? typeNameAlias);

        Assert.Equal("canine", explicitAlias);
        Assert.Equal("cat", attributeAlias);
        Assert.Equal("NoAttributeClass", typeNameAlias);
    }

    [Fact]
    public void Register_MultipleTypes_RegistersEveryType()
    {
        var binder = new TypeAliasRegistry()
            .Register([typeof(Animal), typeof(Dog), typeof(Cat)])
            .BuildBinder();

        Assert.Equal(typeof(Animal), binder.BindToType(null, "animal"));
        Assert.Equal(typeof(Dog), binder.BindToType(null, "dog"));
        Assert.Equal(typeof(Cat), binder.BindToType(null, "cat"));
    }

    [Fact]
    public void Register_DuplicateMappings_EnforcesOneToOneRelationship()
    {
        var registry = new TypeAliasRegistry()
            .Register<Dog>()
            .Register<Dog>();

        Assert.Throws<ArgumentException>(() => registry.Register<Dog>("other"));
        Assert.Throws<ArgumentException>(() => registry.Register<Cat>("dog"));
    }

    [Fact]
    public void Register_InvalidAlias_Throws()
    {
        var registry = new TypeAliasRegistry();

        Assert.Throws<ArgumentException>(() => registry.Register<Dog>(string.Empty));
        Assert.Throws<ArgumentException>(() => registry.Register<Dog>("   "));
        Assert.Throws<ArgumentException>(() => registry.Register<Dog>("dog,legacy"));
    }

    [Fact]
    public void Register_AliasWithWhitespace_PreservesItExactly()
    {
        const string alias = " dog alias ";
        var binder = new TypeAliasRegistry()
            .Register<Dog>(alias)
            .BuildBinder();

        binder.BindToName(typeof(Dog), out string? assemblyName, out string? typeName);

        Assert.Null(assemblyName);
        Assert.Equal(alias, typeName);
        Assert.Equal(typeof(Dog), binder.BindToType(null, alias));
    }

    [Fact]
    public void Register_TypeRestrictions_RejectNonClassAndOpenGenericTypes()
    {
        var registry = new TypeAliasRegistry();

        Assert.Throws<ArgumentException>(() => registry.Register(typeof(int), "number"));
        Assert.Throws<ArgumentException>(() => registry.Register(typeof(List<>), "list"));

        var binder = registry
            .Register(typeof(List<Dog>), "dog-list")
            .BuildBinder();

        Assert.Equal(typeof(List<Dog>), binder.BindToType(null, "dog-list"));
    }

    [Fact]
    public void BuildBinder_CreatesImmutableSnapshot()
    {
        var registry = new TypeAliasRegistry().Register<Dog>();
        var firstBinder = registry.BuildBinder();

        registry.Register<Cat>();
        var secondBinder = registry.BuildBinder();

        firstBinder.BindToName(typeof(Cat), out string? firstAssemblyName, out _);
        secondBinder.BindToName(typeof(Cat), out string? secondAssemblyName, out string? secondTypeName);

        Assert.NotNull(firstAssemblyName);
        Assert.Null(secondAssemblyName);
        Assert.Equal("cat", secondTypeName);
    }
}
