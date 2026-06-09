using Newtonsoft.Json;

namespace BlueBird.Json.TypeAlias.Tests;

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

[JsonTypeAlias]
public class NoAliasAnimal : Animal
{
    public int Age { get; set; }
}

[JsonTypeAlias("custom")]
public class ExplicitAliasClass
{
    public int Value { get; set; }
}

public class NoAttributeClass
{
    public int Value { get; set; }
}

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

[JsonTypeAlias("bird")]
[JsonDeserializationAlias("Bird")]
[JsonDeserializationAlias("old-bird")]
public class Bird : Animal
{
    public string Species { get; set; } = string.Empty;
}

[JsonTypeAlias("same-alias")]
[JsonDeserializationAlias("same-alias")]
public class SameAsPrimaryAliasAnimal : Animal
{
}

// Only has deserialization alias — no [JsonTypeAlias]
[JsonDeserializationAlias("old-fish")]
[JsonDeserializationAlias("legacy-fish")]
public class Fish : Animal
{
    public string WaterType { get; set; } = string.Empty;
}
