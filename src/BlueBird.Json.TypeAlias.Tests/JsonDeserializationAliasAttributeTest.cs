using System;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace BlueBird.Json.TypeAlias.Tests;

public sealed class JsonDeserializationAliasAttributeTest
{
    [Fact]
    public void Register_ReadsAttribute_AutoRegistersDeserializeAliases()
    {
        var binder = new TypeAliasRegistry()
            .Register<Animal>()
            .Register<Bird>()   // [JsonTypeAlias("bird")] + [JsonDeserializationAlias("Bird")] + [JsonDeserializationAlias("old-bird")]
            .BuildBinder();

        var settings = TestHelper.CreateSettings(binder);

        // Primary alias used for serialization
        binder.BindToName(typeof(Bird), out _, out string? typeName);
        Assert.Equal("bird", typeName);

        // All aliases work for deserialization
        foreach (string alias in new[] { "bird", "Bird", "old-bird" })
        {
            string json = $$"""{"$type":"{{alias}}","Name":"Tweety","Species":"Canary"}""";
            Animal? result = JsonConvert.DeserializeObject<Animal>(json, settings);

            Assert.IsType<Bird>(result);
            Assert.Equal("Tweety", ((Bird)result!).Name);
        }
    }

    [Fact]
    public void RegisterAssembly_ReadsAttribute_AutoRegistersDeserializeAliases()
    {
        var binder = new TypeAliasRegistry()
            .RegisterAssembly(Assembly.GetExecutingAssembly())
            .BuildBinder();

        var settings = TestHelper.CreateSettings(binder);

        // Bird has [JsonDeserializationAlias] attributes — should auto-register
        string json = """{"$type":"Bird","Name":"Tweety","Species":"Canary"}""";
        Animal? result = JsonConvert.DeserializeObject<Animal>(json, settings);

        Assert.IsType<Bird>(result);
        Assert.Equal("Tweety", ((Bird)result!).Name);
    }

    [Fact]
    public void Serialization_UsesPrimaryAlias_NotDeserializeAlias()
    {
        var binder = new TypeAliasRegistry()
            .Register<Bird>()
            .BuildBinder();

        var settings = TestHelper.CreateSettings(binder);
        var bird = new Bird { Name = "Tweety", Species = "Canary" };
        string json = JsonConvert.SerializeObject(bird, settings);

        Assert.Contains("\"$type\":\"bird\"", json);
        Assert.DoesNotContain("Bird", json);
        Assert.DoesNotContain("old-bird", json);
    }

    [Fact]
    public void AttributeAndMethodCall_BothWork()
    {
        var binder = new TypeAliasRegistry()
            .Register<Bird>()
            .RegisterDeserializationAlias<Bird>("legacy-bird")  // added via method call
            .BuildBinder();

        var settings = TestHelper.CreateSettings(binder);

        // Attribute-based aliases
        string json1 = """{"$type":"Bird","Name":"A","Species":"B"}""";
        Assert.IsType<Bird>(JsonConvert.DeserializeObject<Animal>(json1, settings));

        // Method-based alias
        string json2 = """{"$type":"legacy-bird","Name":"A","Species":"B"}""";
        Assert.IsType<Bird>(JsonConvert.DeserializeObject<Animal>(json2, settings));
    }

    [Fact]
    public void Attribute_DeserializeAliasSameAsPrimary_NoError()
    {
        // If a [JsonDeserializationAlias] matches the primary alias, should be idempotent
        var binder = new TypeAliasRegistry()
            .Register<Animal>()
            .Register<SameAsPrimaryAliasAnimal>()
            .BuildBinder();

        binder.BindToName(typeof(SameAsPrimaryAliasAnimal), out _, out string? typeName);
        Assert.Equal("same-alias", typeName);
    }

    [Fact]
    public void Attribute_DeserializeAliasConflictWithOtherType_Throws()
    {
        // Bird's [JsonDeserializationAlias("Bird")] conflicts with ExplicitAliasClass's primary alias "Bird"
        var registry = new TypeAliasRegistry();
        registry.Register<ExplicitAliasClass>("Bird");  // register "Bird" as primary for another type

        var ex = Assert.Throws<ArgumentException>(() => registry.Register<Bird>());
        Assert.Contains("already registered by type", ex.Message);
    }

    [Fact]
    public void Register_OnlyDeserializeAlias_UsesTypeNameAsPrimary()
    {
        // Fish has only [JsonDeserializationAlias], no [JsonTypeAlias]
        // Primary alias falls back to type.Name = "Fish"
        var binder = new TypeAliasRegistry()
            .Register<Animal>()
            .Register<Fish>()
            .BuildBinder();

        var settings = TestHelper.CreateSettings(binder);

        // Serialization uses type.Name as primary alias
        var fish = new Fish { Name = "Nemo", WaterType = "Salt" };
        string json = JsonConvert.SerializeObject(fish, settings);
        Assert.Contains("\"$type\":\"Fish\"", json);

        // Deserialization works with type.Name and all attribute aliases
        foreach (string alias in new[] { "Fish", "old-fish", "legacy-fish" })
        {
            string deserJson = $$"""{"$type":"{{alias}}","Name":"Nemo","WaterType":"Salt"}""";
            Animal? result = JsonConvert.DeserializeObject<Animal>(deserJson, settings);

            Assert.IsType<Fish>(result);
            Assert.Equal("Nemo", ((Fish)result!).Name);
        }
    }

    [Fact]
    public void RegisterAssembly_OnlyDeserializeAlias_RegistersType()
    {
        // Fish has only [JsonDeserializationAlias], no [JsonTypeAlias]
        // RegisterAssembly registers deserialization aliases but NOT a primary alias
        var binder = new TypeAliasRegistry()
            .RegisterAssembly(Assembly.GetExecutingAssembly())
            .BuildBinder();

        var settings = TestHelper.CreateSettings(binder);

        // Deserialization aliases from attribute work
        foreach (string alias in new[] { "old-fish", "legacy-fish" })
        {
            string json = $$"""{"$type":"{{alias}}","Name":"Nemo","WaterType":"Salt"}""";
            Animal? result = JsonConvert.DeserializeObject<Animal>(json, settings);

            Assert.IsType<Fish>(result);
            Assert.Equal("Nemo", ((Fish)result!).Name);
        }

        // No primary alias — serialization falls back to default Newtonsoft.Json behavior
        binder.BindToName(typeof(Fish), out _, out string? typeName);
        Assert.NotEqual("Fish", typeName);
        Assert.Contains("Fish", typeName);  // full type name from default binder
    }
}
