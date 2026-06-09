using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

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
