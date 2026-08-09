using BlueBird.Json.TypeAlias;
using Newtonsoft.Json;

namespace BlueBird.Json.TypeAlias.Tests;

public sealed class PolymorphicSerializationTest
{
    [Fact]
    public void CollectionOfBaseTypes_RoundTripsConcreteTypesUsingAliases()
    {
        var binder = new TypeAliasRegistry()
            .Register<Animal>()
            .Register<Dog>()
            .Register<Cat>()
            .BuildBinder();
        var settings = TestHelper.CreateSettings(binder);
        Animal[] animals =
        [
            new Dog { Name = "Rex", Breed = "Labrador" },
            new Cat { Name = "Mimi", IsIndoor = true },
        ];

        string json = JsonConvert.SerializeObject(animals, settings);
        Animal[]? result = JsonConvert.DeserializeObject<Animal[]>(json, settings);

        Assert.Contains("\"$type\":\"dog\"", json);
        Assert.Contains("\"$type\":\"cat\"", json);
        Assert.DoesNotContain(typeof(Dog).Assembly.FullName!, json);
        Assert.NotNull(result);
        var dog = Assert.IsType<Dog>(result[0]);
        var cat = Assert.IsType<Cat>(result[1]);
        Assert.Equal("Rex", dog.Name);
        Assert.Equal("Labrador", dog.Breed);
        Assert.Equal("Mimi", cat.Name);
        Assert.True(cat.IsIndoor);
    }

    [Fact]
    public void AbstractBaseType_RoundTripsConcreteType()
    {
        var binder = new TypeAliasRegistry()
            .Register<Shape>()
            .Register<Circle>()
            .BuildBinder();
        var settings = TestHelper.CreateSettings(binder);
        Shape shape = new Circle { Color = "Red", Radius = 5.0 };

        string json = JsonConvert.SerializeObject(shape, settings);
        Shape? result = JsonConvert.DeserializeObject<Shape>(json, settings);

        Assert.Contains("\"$type\":\"circle\"", json);
        var circle = Assert.IsType<Circle>(result);
        Assert.Equal("Red", circle.Color);
        Assert.Equal(5.0, circle.Radius);
    }
}
