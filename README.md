# BlueBird

A collection of practical .NET NuGet packages. Each package is independently published, and is ready to use out of the box.

## Packages

### BlueBird.Pinyins [![NuGet](https://img.shields.io/nuget/v/BlueBird.Pinyins)](https://www.nuget.org/packages/BlueBird.Pinyins)

Chinese character ↔ Pinyin conversion library. Supports Chinese-to-Pinyin, Pinyin-to-Chinese, and initial letter extraction.

#### Install

```bash
dotnet add package BlueBird.Pinyins
```

#### Usage

```csharp
Pinyin.GetPinyin("中国");           // "zhongguo"
Pinyin.GetInitials("你好", "-");    // "n-h"
Pinyin.GetChineseText("zhong");     // "中种重众钟..."
```

See [src/BlueBird.Pinyins/README.md](src/BlueBird.Pinyins/README.md) for details.

### BlueBird.Json.TypeAlias [![NuGet](https://img.shields.io/nuget/v/BlueBird.Json.TypeAlias)](https://www.nuget.org/packages/BlueBird.Json.TypeAlias)

Newtonsoft.Json extension that replaces bloated assembly-qualified type names with short, stable aliases in `$type` fields. Makes polymorphic JSON compact and resilient to refactoring.

#### Install

```bash
dotnet add package BlueBird.Json.TypeAlias
```

#### Usage

```csharp
[JsonTypeAlias("animal")]
public class Animal
{
    public string Name { get; set; } = string.Empty;
}

[JsonTypeAlias("dog")]
public class Dog : Animal
{
    public string Breed { get; set; } = string.Empty;
}

var binder = new TypeAliasRegistry()
    .Register<Animal>()
    .Register<Dog>()
    .BuildBinder();

var settings = new JsonSerializerSettings
{
    TypeNameHandling = TypeNameHandling.Objects,
    SerializationBinder = binder,
};

// Serialize — compact $type alias instead of full assembly-qualified name
Animal animal = new Dog { Name = "Rex", Breed = "Labrador" };
string json = JsonConvert.SerializeObject(animal, settings);
// {"$type":"dog","Name":"Rex","Breed":"Labrador"}

// Deserialize — correct concrete type is restored
Animal? result = JsonConvert.DeserializeObject<Animal>(json, settings);
// result is Dog { Name = "Rex", Breed = "Labrador" }
```

See [src/BlueBird.Json.TypeAlias/README.md](src/BlueBird.Json.TypeAlias/README.md) for details.

## License

[MIT](LICENSE.txt)
