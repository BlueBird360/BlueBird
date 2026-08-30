using System;
using System.Reflection;
using BlueBird.Json.TypeAlias;
using Newtonsoft.Json;

namespace BlueBird.Json.TypeAlias.Tests;

public sealed class DeserializationAliasTest
{
    [Fact]
    public void AdditionalAlias_DeserializesWhileSerializationUsesPrimaryAlias()
    {
        var binder = new TypeAliasRegistry()
            .Register<Animal>()
            .Register<Dog>()
            .RegisterDeserializationAlias<Dog>("legacy-dog")
            .BuildBinder();
        var settings = TestHelper.CreateSettings(binder);
        var dog = new Dog { Name = "Rex", Breed = "Labrador" };

        string json = JsonConvert.SerializeObject(dog, settings);
        Animal? result = JsonConvert.DeserializeObject<Animal>(
            """{"$type":"legacy-dog","Name":"Rex","Breed":"Labrador"}""",
            settings);

        Assert.Contains("\"$type\":\"dog\"", json);
        var deserializedDog = Assert.IsType<Dog>(result);
        Assert.Equal("Rex", deserializedDog.Name);
        Assert.Equal("Labrador", deserializedDog.Breed);
    }

    [Fact]
    public void DeserializationOnlyRegistration_DoesNotCreateSerializationAlias()
    {
        var binder = new TypeAliasRegistry()
            .RegisterDeserializationAlias<Dog>("legacy-dog")
            .BuildBinder();

        binder.BindToName(typeof(Dog), out string? assemblyName, out string? typeName);

        Assert.NotNull(assemblyName);
        Assert.Contains(nameof(Dog), typeName);
        Assert.Equal(typeof(Dog), binder.BindToType(null, "legacy-dog"));
    }

    [Fact]
    public void MultipleDeserializationAliases_AllResolveToRegisteredType()
    {
        var binder = new TypeAliasRegistry()
            .Register<Dog>()
            .RegisterDeserializationAlias<Dog>(["Dog", "old-dog", "legacy-dog"])
            .BuildBinder();

        Assert.Equal(typeof(Dog), binder.BindToType(null, "dog"));
        Assert.Equal(typeof(Dog), binder.BindToType(null, "Dog"));
        Assert.Equal(typeof(Dog), binder.BindToType(null, "old-dog"));
        Assert.Equal(typeof(Dog), binder.BindToType(null, "legacy-dog"));
    }

    [Fact]
    public void DeserializationAlias_ConflictingWithAnotherType_Throws()
    {
        var registry = new TypeAliasRegistry()
            .Register<Dog>("shared");

        Assert.Throws<ArgumentException>(() =>
            registry.RegisterDeserializationAlias(typeof(Cat), "shared"));
    }

    [Fact]
    public void DeserializationAliasRegisteredBeforePrimaryAlias_PreservesBothAliases()
    {
        var binder = new TypeAliasRegistry()
            .RegisterDeserializationAlias<Dog>("legacy-dog")
            .Register<Dog>()
            .BuildBinder();

        binder.BindToName(typeof(Dog), out _, out string? primaryAlias);

        Assert.Equal("dog", primaryAlias);
        Assert.Equal(typeof(Dog), binder.BindToType(null, "dog"));
        Assert.Equal(typeof(Dog), binder.BindToType(null, "legacy-dog"));
    }

    [Fact]
    public void Register_ReadsPrimaryAndDeserializationAliasAttributes()
    {
        var binder = new TypeAliasRegistry()
            .Register<Bird>()
            .BuildBinder();

        binder.BindToName(typeof(Bird), out _, out string? primaryAlias);

        Assert.Equal("bird", primaryAlias);
        Assert.Equal(typeof(Bird), binder.BindToType(null, "Bird"));
        Assert.Equal(typeof(Bird), binder.BindToType(null, "old-bird"));
    }

    [Fact]
    public void RegisterAssembly_RegistersOnlyAliasesDeclaredByAttributes()
    {
        var binder = new TypeAliasRegistry()
            .RegisterAssembly(Assembly.GetExecutingAssembly())
            .BuildBinder();

        binder.BindToName(typeof(Bird), out string? birdAssemblyName, out string? birdAlias);
        binder.BindToName(typeof(Fish), out string? fishAssemblyName, out _);
        binder.BindToName(typeof(NoAttributeClass), out string? noAttributeAssemblyName, out _);

        Assert.Null(birdAssemblyName);
        Assert.Equal("bird", birdAlias);
        Assert.Equal(typeof(Bird), binder.BindToType(null, "old-bird"));
        Assert.Equal(typeof(Fish), binder.BindToType(null, "old-fish"));
        Assert.NotNull(fishAssemblyName);
        Assert.NotNull(noAttributeAssemblyName);
    }
}
