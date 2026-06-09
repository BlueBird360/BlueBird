using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace BlueBird.Json.TypeAlias.Tests;

public sealed class RoundTripTest
{
    [Fact]
    public void Dog_RoundTrip()
    {
        var binder = new TypeAliasRegistry()
            .Register<Animal>()
            .Register<Dog>()
            .BuildBinder();
        var settings = TestHelper.CreateSettings(binder);

        var original = new Dog { Name = "Rex", Breed = "Labrador" };
        string json = JsonConvert.SerializeObject(original, settings);
        Animal? result = JsonConvert.DeserializeObject<Animal>(json, settings);

        Assert.IsType<Dog>(result);
        var dog = (Dog)result!;
        Assert.Equal(original.Name, dog.Name);
        Assert.Equal(original.Breed, dog.Breed);
    }

    [Fact]
    public void JsonPayload_IsCompact()
    {
        var binder = new TypeAliasRegistry()
            .Register<Animal>()
            .Register<Dog>()
            .BuildBinder();
        var settings = TestHelper.CreateSettings(binder);

        var dog = new Dog { Name = "Rex", Breed = "Labrador" };
        string json = JsonConvert.SerializeObject(dog, settings);

        // Verify alias is short, not full type name
        Assert.Contains("\"$type\":\"dog\"", json);
        Assert.DoesNotContain("BlueBird.Json.TypeAlias.Tests", json);
    }

    [Fact]
    public void BinderIsReusable_AcrossMultipleSerializations()
    {
        var binder = new TypeAliasRegistry()
            .Register<Animal>()
            .Register<Dog>()
            .Register<Cat>()
            .BuildBinder();
        var settings = TestHelper.CreateSettings(binder);

        for (int i = 0; i < 10; i++)
        {
            Animal animal = i % 2 == 0
                ? new Dog { Name = $"Dog{i}", Breed = "Lab" }
                : new Cat { Name = $"Cat{i}", IsIndoor = true };

            string json = JsonConvert.SerializeObject(animal, settings);
            Animal? result = JsonConvert.DeserializeObject<Animal>(json, settings);

            if (i % 2 == 0)
            {
                Assert.IsType<Dog>(result);
                Assert.Equal($"Dog{i}", ((Dog)result!).Name);
            }
            else
            {
                Assert.IsType<Cat>(result);
                Assert.Equal($"Cat{i}", ((Cat)result!).Name);
            }
        }
    }
}
