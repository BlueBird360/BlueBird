using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace BlueBird.Json.TypeAlias.Tests;

public sealed class TypeAliasRegistryTest
{
    [Fact]
    public void Register_WithAttribute_UsesAttributeAlias()
    {
        var binder = new TypeAliasRegistry()
            .Register<Dog>()
            .BuildBinder();

        binder.BindToName(typeof(Dog), out string? assemblyName, out string? typeName);

        Assert.Null(assemblyName);
        Assert.Equal("dog", typeName);
    }

    [Fact]
    public void Register_WithExplicitAlias_UsesExplicitAlias()
    {
        var binder = new TypeAliasRegistry()
            .Register<Dog>("my-dog")
            .BuildBinder();

        binder.BindToName(typeof(Dog), out string? assemblyName, out string? typeName);

        Assert.Null(assemblyName);
        Assert.Equal("my-dog", typeName);
    }

    [Fact]
    public void Register_WithNullAliasOnAttributedType_UsesAttributeAlias()
    {
        var binder = new TypeAliasRegistry()
            .Register<Dog>(null)
            .BuildBinder();

        binder.BindToName(typeof(Dog), out _, out string? typeName);

        Assert.Equal("dog", typeName);
    }

    [Fact]
    public void Register_WithNullAliasOnNullAttributeType_UsesTypeName()
    {
        var binder = new TypeAliasRegistry()
            .Register<NoAttributeClass>()
            .BuildBinder();

        binder.BindToName(typeof(NoAttributeClass), out _, out string? typeName);

        Assert.Equal("NoAttributeClass", typeName);
    }

    [Fact]
    public void Register_WithNullAliasOnNullAliasAttribute_UsesTypeName()
    {
        var binder = new TypeAliasRegistry()
            .Register<NoAliasAnimal>()
            .BuildBinder();

        binder.BindToName(typeof(NoAliasAnimal), out _, out string? typeName);

        Assert.Equal("NoAliasAnimal", typeName);
    }

    [Fact]
    public void Register_MultipleTypes_AllRegistered()
    {
        var binder = new TypeAliasRegistry()
            .Register(new[] { typeof(Animal), typeof(Dog), typeof(Cat) })
            .BuildBinder();

        binder.BindToName(typeof(Animal), out _, out string? animalAlias);
        binder.BindToName(typeof(Dog), out _, out string? dogAlias);
        binder.BindToName(typeof(Cat), out _, out string? catAlias);

        Assert.Equal("animal", animalAlias);
        Assert.Equal("dog", dogAlias);
        Assert.Equal("cat", catAlias);
    }

    [Fact]
    public void Register_DuplicateTypeWithSameAlias_NoError()
    {
        var binder = new TypeAliasRegistry()
            .Register<Dog>()
            .Register<Dog>()
            .BuildBinder();

        binder.BindToName(typeof(Dog), out _, out string? typeName);
        Assert.Equal("dog", typeName);
    }

    [Fact]
    public void Register_DuplicateTypeWithDifferentAlias_Throws()
    {
        var registry = new TypeAliasRegistry().Register<Dog>();

        var ex = Assert.Throws<ArgumentException>(() => registry.Register<Dog>("other"));
        Assert.Contains("already registered", ex.Message);
    }

    [Fact]
    public void Register_DuplicateAliasForDifferentType_Throws()
    {
        var registry = new TypeAliasRegistry().Register<Dog>("shared");

        var ex = Assert.Throws<ArgumentException>(() => registry.Register<Cat>("shared"));
        Assert.Contains("already registered by type", ex.Message);
    }

    [Fact]
    public void Register_EmptyAlias_Throws()
    {
        var registry = new TypeAliasRegistry();

        Assert.Throws<ArgumentException>(() => registry.Register<Dog>(""));
    }

    [Fact]
    public void Register_WhitespaceAlias_Throws()
    {
        var registry = new TypeAliasRegistry();

        Assert.Throws<ArgumentException>(() => registry.Register<Dog>("   "));
    }

    [Fact]
    public void Register_NonClassType_Throws()
    {
        var registry = new TypeAliasRegistry();

        var ex = Assert.Throws<ArgumentException>(() => registry.Register(typeof(int)));
        Assert.Contains("not a class", ex.Message);
    }

    [Fact]
    public void Register_IEnumerable_Overload_RegistersAll()
    {
        var types = new[] { typeof(Animal), typeof(Dog), typeof(Cat) };
        var binder = new TypeAliasRegistry()
            .Register(types)
            .BuildBinder();

        binder.BindToName(typeof(Animal), out _, out string? animalAlias);
        binder.BindToName(typeof(Dog), out _, out string? dogAlias);
        binder.BindToName(typeof(Cat), out _, out string? catAlias);

        Assert.Equal("animal", animalAlias);
        Assert.Equal("dog", dogAlias);
        Assert.Equal("cat", catAlias);
    }

    [Fact]
    public void Register_FluentChaining_ReturnsRegistry()
    {
        TypeAliasRegistry registry = new TypeAliasRegistry();

        TypeAliasRegistry result = registry
            .Register<Animal>()
            .Register<Dog>()
            .Register<Cat>();

        Assert.Same(registry, result);
    }

    [Fact]
    public void RegisterAssembly_RegistersAttributedTypes()
    {
        var binder = new TypeAliasRegistry()
            .RegisterAssembly(typeof(Animal).Assembly)
            .BuildBinder();

        binder.BindToName(typeof(Dog), out _, out string? dogAlias);
        binder.BindToName(typeof(Cat), out _, out string? catAlias);
        binder.BindToName(typeof(Circle), out _, out string? circleAlias);

        Assert.Equal("dog", dogAlias);
        Assert.Equal("cat", catAlias);
        Assert.Equal("circle", circleAlias);
    }

    [Fact]
    public void RegisterAssembly_SkipsNonAttributedTypes()
    {
        var binder = new TypeAliasRegistry()
            .RegisterAssembly(typeof(Animal).Assembly)
            .BuildBinder();

        binder.BindToName(typeof(NoAttributeClass), out string? assemblyName, out string? typeName);

        // Not registered — falls back to default binder
        Assert.NotNull(assemblyName);
    }

    [Fact]
    public void Build_ProducesWorkingBinder()
    {
        var binder = new TypeAliasRegistry()
            .Register<Dog>()
            .BuildBinder();

        var dog = new Dog { Name = "Rex", Breed = "Labrador" };
        var settings = TestHelper.CreateSettings(binder);

        string json = JsonConvert.SerializeObject(dog, settings);
        Assert.Contains("\"$type\":\"dog\"", json);

        Dog? result = JsonConvert.DeserializeObject<Dog>(json, settings);
        Assert.NotNull(result);
        Assert.Equal("Rex", result!.Name);
        Assert.Equal("Labrador", result.Breed);
    }

    [Fact]
    public void Register_NullType_ThrowsArgumentNullException()
    {
        var registry = new TypeAliasRegistry();

        Assert.Throws<ArgumentNullException>(() => registry.Register((Type)null!));
    }

    [Fact]
    public void Register_NullTypesEnumerable_ThrowsArgumentNullException()
    {
        var registry = new TypeAliasRegistry();

        Assert.Throws<ArgumentNullException>(() => registry.Register((System.Collections.Generic.IEnumerable<Type>)null!));
    }

    [Fact]
    public void RegisterAssembly_NullAssembly_ThrowsArgumentNullException()
    {
        var registry = new TypeAliasRegistry();

        Assert.Throws<ArgumentNullException>(() => registry.RegisterAssembly((System.Reflection.Assembly)null!));
    }
}
