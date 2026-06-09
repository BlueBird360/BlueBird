using System;
using Newtonsoft.Json.Serialization;

namespace BlueBird.Json.TypeAlias.Tests;

public sealed class BinderFallbackTest
{
    [Fact]
    public void UnregisteredType_FallsBackToDefaultBinder()
    {
        var binder = new TypeAliasRegistry()
            .Register<Dog>()
            .BuildBinder();

        binder.BindToName(typeof(string), out string? assemblyName, out string? typeName);

        // Falls back to default: assemblyName is not null
        Assert.NotNull(assemblyName);
        Assert.Contains("String", typeName);
    }

    [Fact]
    public void UnregisteredAlias_FallsBackToDefaultBinder()
    {
        var binder = new TypeAliasRegistry()
            .Register<Dog>()
            .BuildBinder();

        // Unknown alias with null assemblyName — falls back to default binder
        // which searches loaded assemblies; returns null if not found
        Type result = binder.BindToType(null, "unknown-type");
        Assert.Null(result);
    }

    [Fact]
    public void BindToType_WithNonNullAssemblyName_FallsBackToDefault()
    {
        var binder = new TypeAliasRegistry()
            .Register<Dog>()
            .BuildBinder();

        // assemblyName != null → skip alias lookup, go to default
        Type type = binder.BindToType(typeof(Dog).Assembly.FullName, typeof(Dog).FullName!);

        Assert.Equal(typeof(Dog), type);
    }
}
