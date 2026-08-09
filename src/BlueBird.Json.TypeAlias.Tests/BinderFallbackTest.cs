using System;
using BlueBird.Json.TypeAlias;
using Newtonsoft.Json.Serialization;

namespace BlueBird.Json.TypeAlias.Tests;

public sealed class BinderFallbackTest
{
    [Fact]
    public void UnregisteredType_DefaultFallbackPreservesTypeIdentity()
    {
        var binder = new TypeAliasRegistry()
            .Register<Dog>()
            .BuildBinder();

        binder.BindToName(typeof(NoAttributeClass), out string? assemblyName, out string? typeName);
        Type resolvedType = binder.BindToType(assemblyName, typeName!);

        Assert.NotNull(assemblyName);
        Assert.Equal(typeof(NoAttributeClass), resolvedType);
    }

    [Fact]
    public void RegisteredAlias_TakesPrecedenceOverCustomFallback()
    {
        var fallbackBinder = new RecordingSerializationBinder(typeof(NoAttributeClass));
        var binder = new TypeAliasRegistry()
            .Register<Dog>()
            .BuildBinder(fallbackBinder);

        binder.BindToName(typeof(Dog), out string? assemblyName, out string? typeName);
        Type resolvedType = binder.BindToType(null, "dog");

        Assert.Null(assemblyName);
        Assert.Equal("dog", typeName);
        Assert.Equal(typeof(Dog), resolvedType);
        Assert.Equal(0, fallbackBinder.BindToNameCallCount);
        Assert.Equal(0, fallbackBinder.BindToTypeCallCount);
    }

    [Fact]
    public void UnregisteredMapping_DelegatesBothDirectionsToCustomFallback()
    {
        var fallbackBinder = new RecordingSerializationBinder(typeof(NoAttributeClass));
        var binder = new TypeAliasRegistry()
            .Register<Dog>()
            .BuildBinder(fallbackBinder);

        binder.BindToName(typeof(string), out string? assemblyName, out string? typeName);
        Type resolvedType = binder.BindToType("input-assembly", "input-type");

        Assert.Equal("fallback-assembly", assemblyName);
        Assert.Equal("fallback-type", typeName);
        Assert.Equal(typeof(string), fallbackBinder.LastSerializedType);
        Assert.Equal(typeof(NoAttributeClass), resolvedType);
        Assert.Equal("input-assembly", fallbackBinder.LastAssemblyName);
        Assert.Equal("input-type", fallbackBinder.LastTypeName);
        Assert.Equal(1, fallbackBinder.BindToNameCallCount);
        Assert.Equal(1, fallbackBinder.BindToTypeCallCount);
    }

    private sealed class RecordingSerializationBinder : ISerializationBinder
    {
        private readonly Type _typeToReturn;

        public RecordingSerializationBinder(Type typeToReturn)
        {
            this._typeToReturn = typeToReturn;
        }

        public int BindToNameCallCount { get; private set; }

        public int BindToTypeCallCount { get; private set; }

        public Type? LastSerializedType { get; private set; }

        public string? LastAssemblyName { get; private set; }

        public string? LastTypeName { get; private set; }

        public void BindToName(Type serializedType, out string? assemblyName, out string? typeName)
        {
            this.BindToNameCallCount++;
            this.LastSerializedType = serializedType;
            assemblyName = "fallback-assembly";
            typeName = "fallback-type";
        }

        public Type BindToType(string? assemblyName, string typeName)
        {
            this.BindToTypeCallCount++;
            this.LastAssemblyName = assemblyName;
            this.LastTypeName = typeName;
            return this._typeToReturn;
        }
    }
}
