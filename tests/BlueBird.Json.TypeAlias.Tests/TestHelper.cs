using BlueBird.Json.TypeAlias;
using Newtonsoft.Json;

namespace BlueBird.Json.TypeAlias.Tests;

internal static class TestHelper
{
    public static JsonSerializerSettings CreateSettings(TypeAliasSerializationBinder binder)
    {
        return new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Objects,
            SerializationBinder = binder,
        };
    }
}
