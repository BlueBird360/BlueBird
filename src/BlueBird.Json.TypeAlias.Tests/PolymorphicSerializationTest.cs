using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace BlueBird.Json.TypeAlias.Tests;

public sealed class PolymorphicSerializationTest
{
    private readonly TypeAliasSerializationBinder _binder;
    private readonly JsonSerializerSettings _settings;

    public PolymorphicSerializationTest()
    {
        this._binder = new TypeAliasRegistry()
            .Register<Animal>()
            .Register<Dog>()
            .Register<Cat>()
            .BuildBinder();
        this._settings = TestHelper.CreateSettings(this._binder);
    }

    [Fact]
    public void DerivedThroughBase_PreservesConcreteType()
    {
        Animal animal = new Dog { Name = "Rex", Breed = "Labrador" };
        string json = JsonConvert.SerializeObject(animal, this._settings);

        Assert.Contains("\"$type\":\"dog\"", json);
        Assert.Contains("\"Name\":\"Rex\"", json);
        Assert.Contains("\"Breed\":\"Labrador\"", json);
    }

    [Fact]
    public void DerivedThroughBase_DeserializesToConcreteType()
    {
        Animal animal = new Dog { Name = "Rex", Breed = "Labrador" };
        string json = JsonConvert.SerializeObject(animal, this._settings);

        Animal? result = JsonConvert.DeserializeObject<Animal>(json, this._settings);

        Assert.IsType<Dog>(result);
        var dog = (Dog)result!;
        Assert.Equal("Rex", dog.Name);
        Assert.Equal("Labrador", dog.Breed);
    }

    [Fact]
    public void BaseInstance_StillWritesTypeWhenRegistered()
    {
        var animal = new Animal { Name = "Generic" };
        string json = JsonConvert.SerializeObject(animal, this._settings);

        Assert.Contains("\"$type\":\"animal\"", json);

        Animal? result = JsonConvert.DeserializeObject<Animal>(json, this._settings);
        Assert.IsType<Animal>(result);
        Assert.Equal("Generic", result!.Name);
    }

    [Fact]
    public void CollectionOfBaseTypes_PreservesEachConcreteType()
    {
        var animals = new Animal[]
        {
            new Dog { Name = "Rex", Breed = "Labrador" },
            new Cat { Name = "Whiskers", IsIndoor = true },
        };

        string json = JsonConvert.SerializeObject(animals, this._settings);

        Animal[]? result = JsonConvert.DeserializeObject<Animal[]>(json, this._settings);

        Assert.NotNull(result);
        Assert.Equal(2, result!.Length);
        Assert.IsType<Dog>(result[0]);
        Assert.IsType<Cat>(result[1]);

        var dog = (Dog)result[0];
        Assert.Equal("Rex", dog.Name);
        Assert.Equal("Labrador", dog.Breed);

        var cat = (Cat)result[1];
        Assert.Equal("Whiskers", cat.Name);
        Assert.True(cat.IsIndoor);
    }
}
