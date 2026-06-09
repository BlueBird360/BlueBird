# BlueBird.Json.TypeAlias

A [Newtonsoft.Json](https://www.newtonsoft.com/json) extension that replaces fully qualified type names with short, stable aliases in JSON output.

## Why?

### Problem 1: Polymorphic deserialization requires `TypeNameHandling`

Consider a class hierarchy:

```csharp
public class Animal
{
    public string Name { get; set; } = string.Empty;
}

public class Dog : Animal
{
    public string Breed { get; set; } = string.Empty;
}
```

With `TypeNameHandling.None` (the default), serializing a `Dog` through an `Animal` reference produces:

```csharp
Animal animal = new Dog { Name = "Rex", Breed = "Labrador" };
string json = JsonConvert.SerializeObject(animal);
// {"Name":"Rex","Breed":"Labrador"}
```

The `$type` metadata is absent, so deserialization has no way to know the concrete type:

```csharp
Animal? result = JsonConvert.DeserializeObject<Animal>(json);
// result is Animal, not Dog — Breed data is silently lost.
// If Animal were abstract, this would throw an exception.
```

To fix this, you must enable `TypeNameHandling.Objects`, `TypeNameHandling.Auto`, or `TypeNameHandling.All`. But that introduces two more problems.

### Problem 2: Bloated JSON

With `TypeNameHandling.Objects`, Newtonsoft.Json writes the full assembly name and type qualified name into the `$type` field:

```json
{
  "$type": "MyApp.Models.Dog, MyApp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
  "Name": "Rex",
  "Breed": "Labrador"
}
```

The `$type` value can easily be longer than the actual data.

### Problem 3: Fragile deserialization

The `$type` field pins the serialized JSON to the exact assembly name and type namespace. If you later move `Dog` to a different assembly or rename the namespace, old JSON becomes unreadable.

### How this library solves all three

`BlueBird.Json.TypeAlias` writes a short, stable alias instead of the full type name. This makes `TypeNameHandling` practical for polymorphic scenarios:

```json
{
  "$type": "dog",
  "Name": "Rex",
  "Breed": "Labrador"
}
```

The JSON stays compact, and types can be freely moved across assemblies and namespaces — as long as the alias doesn't change, old JSON still deserializes correctly.

## Quick Start

### 1. Define your types

```csharp
using Newtonsoft.Json;

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

[JsonTypeAlias("cat")]
public class Cat : Animal
{
    public bool IsIndoor { get; set; }
}
```

### 2. Register types and build the binder

```csharp
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

var binder = new TypeAliasRegistry()
    .Register<Animal>()
    .Register<Dog>()
    .Register<Cat>()
    .BuildBinder();

var settings = new JsonSerializerSettings
{
    TypeNameHandling = TypeNameHandling.Objects,
    SerializationBinder = binder,
};
```

### 3. Serialize and deserialize

```csharp
// Polymorphic serialization — concrete type is preserved
Animal animal = new Dog { Name = "Rex", Breed = "Labrador" };
string json = JsonConvert.SerializeObject(animal, settings);
// {"$type":"dog","Name":"Rex","Breed":"Labrador"}

// Polymorphic deserialization — returns the correct derived type
Animal? result = JsonConvert.DeserializeObject<Animal>(json, settings);
// result is Dog { Name = "Rex", Breed = "Labrador" }
```

## Polymorphic Serialization in Depth

The key scenario for `BlueBird.Json.TypeAlias` is **preserving concrete types across a class hierarchy** during serialization and deserialization.

As shown above, this library solves the three problems by replacing the full type name with a short, stable alias:

```csharp
var settings = new JsonSerializerSettings
{
    TypeNameHandling = TypeNameHandling.Objects,
    SerializationBinder = binder,
};

string json = JsonConvert.SerializeObject(animal, settings);
// {"$type":"dog","Name":"Rex","Breed":"Labrador"}
```

The `$type` field is a short, stable alias. Even if `Dog` is moved to `MyApp.Domain.Models`, the alias `"dog"` still resolves correctly.

### Collections of base types

This also works for collections containing mixed derived types:

```csharp
var animals = new Animal[]
{
    new Dog { Name = "Rex", Breed = "Labrador" },
    new Cat { Name = "Whiskers", IsIndoor = true },
};

string json = JsonConvert.SerializeObject(animals, settings);
// [
//   {"$type":"dog","Name":"Rex","Breed":"Labrador"},
//   {"$type":"cat","Name":"Whiskers","IsIndoor":true}
// ]

Animal[]? result = JsonConvert.DeserializeObject<Animal[]>(json, settings);
// result[0] is Dog, result[1] is Cat
```

### Properties with base type

In real-world scenarios, polymorphism typically appears as class properties declared with a base type:

```csharp
[JsonTypeAlias("zoo")]
public class Zoo
{
    public string Name { get; set; } = string.Empty;
    public Animal Star { get; set; } = null!;        // runtime type: Dog or Cat
    public Animal[] Residents { get; set; } = [];    // mixed Dog and Cat instances
}
```

```csharp
var zoo = new Zoo
{
    Name = "Central Park Zoo",
    Star = new Dog { Name = "Rex", Breed = "Labrador" },
    Residents = new Animal[]
    {
        new Dog { Name = "Rex", Breed = "Labrador" },
        new Cat { Name = "Whiskers", IsIndoor = true },
    },
};

string json = JsonConvert.SerializeObject(zoo, settings);
// {
//   "$type":"zoo",
//   "Name":"Central Park Zoo",
//   "Star":{"$type":"dog","Name":"Rex","Breed":"Labrador"},
//   "Residents":[
//     {"$type":"dog","Name":"Rex","Breed":"Labrador"},
//     {"$type":"cat","Name":"Whiskers","IsIndoor":true}
//   ]
// }

Zoo? result = JsonConvert.DeserializeObject<Zoo>(json, settings);
// result.Star is Dog
// result.Residents[0] is Dog, result.Residents[1] is Cat
```

### Abstract base types

When the base type is `abstract`, `TypeNameHandling` is not optional — without it, deserialization throws because the abstract type cannot be instantiated. `BlueBird.Json.TypeAlias` makes this practical:

```csharp
[JsonTypeAlias("shape")]
public abstract class Shape
{
    public string Color { get; set; } = string.Empty;
}

[JsonTypeAlias("circle")]
public class Circle : Shape
{
    public double Radius { get; set; }
}

[JsonTypeAlias("rect")]
public class Rectangle : Shape
{
    public double Width { get; set; }
    public double Height { get; set; }
}
```

```csharp
Shape shape = new Circle { Color = "Red", Radius = 5.0 };
string json = JsonConvert.SerializeObject(shape, settings);
// {"$type":"circle","Color":"Red","Radius":5.0}

Shape? result = JsonConvert.DeserializeObject<Shape>(json, settings);
// result is Circle { Color = "Red", Radius = 5.0 }
```

## Registering Types

Types are registered via `TypeAliasRegistry` during application startup. Call `BuildBinder()` to produce an immutable binder.

| Method | Behavior |
|--------|----------|
| `Register<T>()` | Register a single type. Alias is resolved from the explicit `alias` parameter, then `[JsonTypeAlias]` attribute, then falls back to `type.Name`. `[JsonDeserializationAlias]` attributes are also auto-registered. |
| `Register(Type)` | Same as above, accepts a `Type` parameter. |
| `Register(IEnumerable<Type>)` | Register multiple types at once. For each type, the alias is resolved from `[JsonTypeAlias]` or `type.Name`. |
| `RegisterAssembly(Assembly)` | Scan and register all types decorated with `[JsonTypeAlias]` or `[JsonDeserializationAlias]`. Types with only `[JsonDeserializationAlias]` have no primary alias registered; serialization falls back to the default Newtonsoft.Json behavior. |
| `RegisterDeserializationAlias<T>(alias)` | Register a deserialization-only alias. Does not require prior `Register()` call. Use this for types you cannot modify (e.g., third-party types). |
| `RegisterDeserializationAlias<T>(IEnumerable<string>)` | Register multiple deserialization-only aliases at once. |
| `RegisterDeserializationAlias(Type, alias)` | Register a deserialization-only alias. Accepts a `Type` parameter instead of generic. |
| `RegisterDeserializationAlias(Type, IEnumerable<string>)` | Register multiple deserialization-only aliases. Accepts a `Type` parameter instead of generic. |

All methods return `this` to support fluent chaining:

```csharp
var binder = new TypeAliasRegistry()
    .Register<Animal>()
    .Register<Dog>()
    .Register<Cat>()
    .RegisterDeserializationAlias<Dog>("Dog")  // old alias for backward compatibility
    .RegisterAssembly(typeof(Shape).Assembly)
    .BuildBinder();
```

## Alias Resolution

The registry resolves the alias in this order:

1. The explicit `alias` parameter passed to `Register(type, alias)`, if provided.
2. The value of `[JsonTypeAlias("...")]` on the type.
3. The type's simple name (`type.Name`).

## Alias Migration and Backward Compatibility

When you rename an alias (e.g., from `"Dog"` to `"dog"`), old JSON with the previous alias would normally become unreadable. There are two ways to solve this:

### Option 1: Using `[JsonDeserializationAlias]` attribute (recommended)

Add the attribute directly on the class. When you call `Register<T>()` or `RegisterAssembly()`, these aliases are automatically registered:

```csharp
[JsonTypeAlias("dog")]
[JsonDeserializationAlias("Dog")]      // v1 alias — still works for deserialization
[JsonDeserializationAlias("Canine")]   // even older alias
public class Dog : Animal
{
    public string Breed { get; set; } = string.Empty;
}

// Just register — attribute-based aliases are auto-registered
var binder = new TypeAliasRegistry()
    .Register<Dog>()
    .BuildBinder();
```

This is the preferred approach because all aliases are declared at the class definition, making them easy to see and maintain.

### Option 2: Using `RegisterDeserializationAlias` method

For types you cannot modify (e.g., third-party types), use the method-based approach:

```csharp
var binder = new TypeAliasRegistry()
    .Register<Dog>()                           // primary alias: "dog" (used for serialization)
    .RegisterDeserializationAlias<Dog>("Dog")       // old alias still works for deserialization
    .BuildBinder();
```

### How it works

Both approaches produce the same result:

```csharp
// Old JSON (v1)
string oldJson = """{"$type":"Dog","Name":"Rex","Breed":"Lab"}""";
Animal? result1 = JsonConvert.DeserializeObject<Animal>(oldJson, settings);
// result1 is Dog ✓

// New JSON (v2)
string newJson = """{"$type":"dog","Name":"Rex","Breed":"Lab"}""";
Animal? result2 = JsonConvert.DeserializeObject<Animal>(newJson, settings);
// result2 is Dog ✓

// Serialization always uses the primary alias
string json = JsonConvert.SerializeObject(new Dog { Name = "Rex", Breed = "Lab" }, settings);
// {"$type":"dog","Name":"Rex","Breed":"Lab"}
```

Multiple additional aliases can be registered for a single type, supporting gradual migration across several versions.

## Architecture

The library has four main components:

- **`JsonTypeAliasAttribute`** — defines the primary alias for a type (used for both serialization and deserialization).
- **`JsonDeserializationAliasAttribute`** — defines additional aliases for deserialization only (used for backward compatibility).
- **`TypeAliasRegistry`** — mutable registry used during startup to collect type-alias mappings. Automatically reads both attributes during `Register()` and `RegisterAssembly()`. Call `BuildBinder()` when registration is complete.
- **`TypeAliasSerializationBinder`** — immutable binder returned by `BuildBinder()`. Uses `FrozenDictionary` internally for lock-free, high-performance lookups. Thread-safe for concurrent reads.

This design ensures that type-alias mappings are configured once at startup and cannot be accidentally modified at runtime.

## Application-wide Singleton

For application-wide use, build the binder once and expose it as a static readonly field:

```csharp
public static class TypeAliasBinder
{
    public static readonly TypeAliasSerializationBinder Instance =
        new TypeAliasRegistry()
            .Register<Animal>()
            .Register<Dog>()
            .Register<Cat>()
            .RegisterAssembly(typeof(Shape).Assembly)
            .BuildBinder();
}
```

Then share it across all `JsonSerializerSettings`:

```csharp
var settings = new JsonSerializerSettings
{
    TypeNameHandling = TypeNameHandling.Objects,
    SerializationBinder = TypeAliasBinder.Instance,
};
```

Since the binder is immutable, it is inherently thread-safe — no locks needed.

## Thread Safety

`TypeAliasSerializationBinder` is immutable and thread-safe. Once built via `TypeAliasRegistry.BuildBinder()`, it can be shared across threads without any synchronization.

`TypeAliasRegistry` is **not** thread-safe. Register all types from a single thread during startup, then call `BuildBinder()`.

## License

MIT
