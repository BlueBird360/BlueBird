using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace BlueBird.Json.TypeAlias.Tests;

public sealed class RegisterDeserializationAliasTest
{
    private readonly JsonSerializerSettings _settings;

    public RegisterDeserializationAliasTest()
    {
        var binder = new TypeAliasRegistry()
            .Register<Animal>()
            .Register<Dog>()
            .Register<Cat>()
            .RegisterDeserializationAlias<Dog>("Dog")     // old alias (capital D) for backward compatibility
            .BuildBinder();
        this._settings = TestHelper.CreateSettings(binder);
    }

    [Fact]
    public void Serialization_UsesPrimaryAlias()
    {
        var dog = new Dog { Name = "Rex", Breed = "Lab" };
        string json = JsonConvert.SerializeObject(dog, this._settings);

        // Primary alias from [JsonTypeAlias("dog")] — lowercase
        Assert.Contains("\"$type\":\"dog\"", json);
    }

    [Fact]
    public void Deserialization_PrimaryAlias_Works()
    {
        string json = """{"$type":"dog","Name":"Rex","Breed":"Lab"}""";

        Animal? result = JsonConvert.DeserializeObject<Animal>(json, this._settings);

        Assert.IsType<Dog>(result);
        Assert.Equal("Rex", ((Dog)result!).Name);
    }

    [Fact]
    public void Deserialization_AdditionalAlias_Works()
    {
        // Old JSON with capital "Dog" — should still deserialize correctly
        string json = """{"$type":"Dog","Name":"Rex","Breed":"Lab"}""";

        Animal? result = JsonConvert.DeserializeObject<Animal>(json, this._settings);

        Assert.IsType<Dog>(result);
        Assert.Equal("Rex", ((Dog)result!).Name);
    }

    [Fact]
    public void RegisterDeserializationAlias_WithoutPriorRegister_Works()
    {
        // RegisterDeserializationAlias no longer requires prior Register
        var binder = new TypeAliasRegistry()
            .RegisterDeserializationAlias<Dog>("old-dog")
            .BuildBinder();

        var settings = TestHelper.CreateSettings(binder);

        // Deserialization works with the alias
        string json = """{"$type":"old-dog","Name":"Rex","Breed":"Lab"}""";
        Animal? result = JsonConvert.DeserializeObject<Animal>(json, settings);
        Assert.IsType<Dog>(result);
        Assert.Equal("Rex", ((Dog)result!).Name);
    }

    [Fact]
    public void RegisterDeserializationAlias_OnlyNoPrimaryAlias_SerializationFallsBackToDefault()
    {
        // Only deserialization alias, no primary alias registered
        var binder = new TypeAliasRegistry()
            .Register<Animal>()
            .RegisterDeserializationAlias<Dog>("legacy-dog")
            .BuildBinder();

        var settings = TestHelper.CreateSettings(binder);
        var dog = new Dog { Name = "Rex", Breed = "Lab" };
        string json = JsonConvert.SerializeObject(dog, settings);

        // No primary alias → falls back to Newtonsoft.Json default (full type name)
        Assert.DoesNotContain("\"$type\":\"legacy-dog\"", json);
        Assert.Contains("Dog", json);
    }

    [Fact]
    public void RegisterDeserializationAlias_DuplicateSameType_NoError()
    {
        var binder = new TypeAliasRegistry()
            .Register<Dog>()
            .RegisterDeserializationAlias<Dog>("old-dog")
            .RegisterDeserializationAlias<Dog>("old-dog")  // duplicate — no error
            .BuildBinder();

        string json = """{"$type":"old-dog","Name":"Rex","Breed":"Lab"}""";
        Animal? result = JsonConvert.DeserializeObject<Animal>(json, TestHelper.CreateSettings(binder));

        Assert.IsType<Dog>(result);
    }

    [Fact]
    public void RegisterDeserializationAlias_AliasAlreadyUsedByOtherType_Throws()
    {
        var registry = new TypeAliasRegistry()
            .Register<Dog>()
            .Register<Cat>();

        var ex = Assert.Throws<ArgumentException>(() => registry.RegisterDeserializationAlias<Cat>("dog"));
        Assert.Contains("already registered by type", ex.Message);
    }

    [Fact]
    public void RegisterDeserializationAlias_EmptyAlias_Throws()
    {
        var registry = new TypeAliasRegistry().Register<Dog>();

        Assert.Throws<ArgumentException>(() => registry.RegisterDeserializationAlias<Dog>(""));
    }

    [Fact]
    public void RegisterDeserializationAlias_SameAsPrimaryAlias_NoError()
    {
        // Registering the same alias as primary should be idempotent
        var binder = new TypeAliasRegistry()
            .Register<Dog>()                          // primary alias: "dog"
            .RegisterDeserializationAlias<Dog>("dog")      // same as primary — no error
            .BuildBinder();

        binder.BindToName(typeof(Dog), out _, out string? typeName);
        Assert.Equal("dog", typeName);
    }

    [Fact]
    public void RegisterDeserializationAlias_NonGenericOverload_Works()
    {
        var binder = new TypeAliasRegistry()
            .Register<Dog>()
            .RegisterDeserializationAlias(typeof(Dog), "old-dog")
            .BuildBinder();

        var settings = TestHelper.CreateSettings(binder);
        string json = """{"$type":"old-dog","Name":"Rex","Breed":"Lab"}""";

        Animal? result = JsonConvert.DeserializeObject<Animal>(json, settings);
        Assert.IsType<Dog>(result);
        Assert.Equal("Rex", ((Dog)result!).Name);
    }

    [Fact]
    public void RegisterDeserializationAlias_FluentChaining()
    {
        TypeAliasRegistry registry = new TypeAliasRegistry();

        TypeAliasRegistry result = registry
            .Register<Dog>()
            .RegisterDeserializationAlias<Dog>("old-dog")
            .RegisterDeserializationAlias<Dog>("legacy-dog");

        Assert.Same(registry, result);
    }

    [Fact]
    public void MultipleAliases_AllDeserializeCorrectly()
    {
        var binder = new TypeAliasRegistry()
            .Register<Animal>()
            .Register<Dog>()
            .RegisterDeserializationAlias<Dog>("Dog")
            .RegisterDeserializationAlias<Dog>("old-dog")
            .RegisterDeserializationAlias<Dog>("legacy-dog")
            .BuildBinder();

        var settings = TestHelper.CreateSettings(binder);

        // All four aliases should resolve to Dog
        foreach (string alias in new[] { "dog", "Dog", "old-dog", "legacy-dog" })
        {
            string json = $$"""{"$type":"{{alias}}","Name":"Rex","Breed":"Lab"}""";
            Animal? result = JsonConvert.DeserializeObject<Animal>(json, settings);

            Assert.IsType<Dog>(result);
            Assert.Equal("Rex", ((Dog)result!).Name);
        }
    }

    [Fact]
    public void RegisterDeserializationAlias_BatchGeneric_Works()
    {
        var binder = new TypeAliasRegistry()
            .Register<Animal>()
            .Register<Dog>()
            .RegisterDeserializationAlias<Dog>(["Dog", "old-dog", "legacy-dog"])
            .BuildBinder();

        var settings = TestHelper.CreateSettings(binder);

        foreach (string alias in new[] { "dog", "Dog", "old-dog", "legacy-dog" })
        {
            string json = $$"""{"$type":"{{alias}}","Name":"Rex","Breed":"Lab"}""";
            Animal? result = JsonConvert.DeserializeObject<Animal>(json, settings);

            Assert.IsType<Dog>(result);
            Assert.Equal("Rex", ((Dog)result!).Name);
        }
    }

    [Fact]
    public void RegisterDeserializationAlias_BatchNonGeneric_Works()
    {
        var binder = new TypeAliasRegistry()
            .Register<Animal>()
            .Register<Dog>()
            .RegisterDeserializationAlias(typeof(Dog), ["Dog", "old-dog", "legacy-dog"])
            .BuildBinder();

        var settings = TestHelper.CreateSettings(binder);

        foreach (string alias in new[] { "dog", "Dog", "old-dog", "legacy-dog" })
        {
            string json = $$"""{"$type":"{{alias}}","Name":"Rex","Breed":"Lab"}""";
            Animal? result = JsonConvert.DeserializeObject<Animal>(json, settings);

            Assert.IsType<Dog>(result);
            Assert.Equal("Rex", ((Dog)result!).Name);
        }
    }

    [Fact]
    public void RegisterDeserializationAlias_BatchEmptyList_NoError()
    {
        var binder = new TypeAliasRegistry()
            .Register<Dog>()
            .RegisterDeserializationAlias<Dog>(Array.Empty<string>())
            .BuildBinder();

        binder.BindToName(typeof(Dog), out _, out string? typeName);
        Assert.Equal("dog", typeName);
    }

    [Fact]
    public void RegisterDeserializationAlias_BatchDuplicateSameType_NoError()
    {
        var binder = new TypeAliasRegistry()
            .Register<Dog>()
            .RegisterDeserializationAlias<Dog>(["old-dog", "old-dog"])
            .BuildBinder();

        string json = """{"$type":"old-dog","Name":"Rex","Breed":"Lab"}""";
        Animal? result = JsonConvert.DeserializeObject<Animal>(json, TestHelper.CreateSettings(binder));

        Assert.IsType<Dog>(result);
    }

    [Fact]
    public void RegisterDeserializationAlias_BatchAliasAlreadyUsedByOtherType_Throws()
    {
        var registry = new TypeAliasRegistry()
            .Register<Dog>()
            .Register<Cat>();

        var ex = Assert.Throws<ArgumentException>(() =>
            registry.RegisterDeserializationAlias<Cat>(["dog"]));
        Assert.Contains("already registered by type", ex.Message);
    }

    [Fact]
    public void RegisterDeserializationAlias_BatchEmptyAlias_Throws()
    {
        var registry = new TypeAliasRegistry().Register<Dog>();

        Assert.Throws<ArgumentException>(() =>
            registry.RegisterDeserializationAlias<Dog>([""]));
    }

    [Fact]
    public void RegisterDeserializationAlias_BatchFluentChaining()
    {
        TypeAliasRegistry registry = new TypeAliasRegistry();

        TypeAliasRegistry result = registry
            .Register<Dog>()
            .RegisterDeserializationAlias<Dog>(["old-dog", "legacy-dog"]);

        Assert.Same(registry, result);
    }

    [Fact]
    public void RegisterDeserializationAlias_NullType_ThrowsArgumentNullException()
    {
        var registry = new TypeAliasRegistry();

        Assert.Throws<ArgumentNullException>(() => registry.RegisterDeserializationAlias((Type)null!, "alias"));
    }

    [Fact]
    public void RegisterDeserializationAlias_NullAliasString_ThrowsArgumentNullException()
    {
        var registry = new TypeAliasRegistry();

        Assert.Throws<ArgumentNullException>(() => registry.RegisterDeserializationAlias(typeof(Dog), (string)null!));
    }

    [Fact]
    public void RegisterDeserializationAlias_NullAliasesEnumerable_ThrowsArgumentNullException()
    {
        var registry = new TypeAliasRegistry();

        Assert.Throws<ArgumentNullException>(() => registry.RegisterDeserializationAlias(typeof(Dog), (System.Collections.Generic.IEnumerable<string>)null!));
    }

    [Fact]
    public void RegisterDeserializationAlias_Generic_NullAliasString_ThrowsArgumentNullException()
    {
        var registry = new TypeAliasRegistry();

        Assert.Throws<ArgumentNullException>(() => registry.RegisterDeserializationAlias<Dog>((string)null!));
    }

    [Fact]
    public void RegisterDeserializationAlias_Generic_NullAliasesEnumerable_ThrowsArgumentNullException()
    {
        var registry = new TypeAliasRegistry();

        Assert.Throws<ArgumentNullException>(() => registry.RegisterDeserializationAlias<Dog>((System.Collections.Generic.IEnumerable<string>)null!));
    }

    [Fact]
    public void RegisterDeserializationAlias_ThenRegister_PrimaryAliasRegistered()
    {
        // Bug scenario: register deserialization alias first, then primary alias with same name
        var binder = new TypeAliasRegistry()
            .RegisterDeserializationAlias<Dog>("dog")
            .Register<Dog>()
            .BuildBinder();

        // Primary alias should be registered and used for serialization
        binder.BindToName(typeof(Dog), out string? assemblyName, out string? typeName);
        Assert.Null(assemblyName);
        Assert.Equal("dog", typeName);

        // Serialization should use the primary alias
        var dog = new Dog { Name = "Rex", Breed = "Lab" };
        var settings = TestHelper.CreateSettings(binder);
        string json = JsonConvert.SerializeObject(dog, settings);
        Assert.Contains("\"$type\":\"dog\"", json);
    }

    [Fact]
    public void RegisterDeserializationAlias_ThenRegister_DifferentAlias_PrimaryAliasRegistered()
    {
        // Register deserialization alias first, then primary alias with different name
        var binder = new TypeAliasRegistry()
            .RegisterDeserializationAlias<Dog>("old-dog")
            .Register<Dog>()  // primary alias = "dog" from attribute
            .BuildBinder();

        // Primary alias should be registered
        binder.BindToName(typeof(Dog), out _, out string? typeName);
        Assert.Equal("dog", typeName);

        // Both aliases should work for deserialization
        var settings = TestHelper.CreateSettings(binder);

        string json1 = """{"$type":"dog","Name":"Rex","Breed":"Lab"}""";
        Assert.IsType<Dog>(JsonConvert.DeserializeObject<Animal>(json1, settings));

        string json2 = """{"$type":"old-dog","Name":"Rex","Breed":"Lab"}""";
        Assert.IsType<Dog>(JsonConvert.DeserializeObject<Animal>(json2, settings));
    }

    [Fact]
    public void RegisterDeserializationAlias_MultipleThenRegister_PrimaryAliasRegistered()
    {
        // Register multiple deserialization aliases, then primary alias
        var binder = new TypeAliasRegistry()
            .RegisterDeserializationAlias<Dog>(["old-dog", "legacy-dog", "dog"])
            .Register<Dog>()
            .BuildBinder();

        // Primary alias should be registered
        binder.BindToName(typeof(Dog), out _, out string? typeName);
        Assert.Equal("dog", typeName);

        // All aliases should work for deserialization
        var settings = TestHelper.CreateSettings(binder);

        foreach (string alias in new[] { "dog", "old-dog", "legacy-dog" })
        {
            string json = $$"""{"$type":"{{alias}}","Name":"Rex","Breed":"Lab"}""";
            Assert.IsType<Dog>(JsonConvert.DeserializeObject<Animal>(json, settings));
        }
    }
}
