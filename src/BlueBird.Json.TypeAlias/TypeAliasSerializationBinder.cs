using System;
using System.Collections.Frozen;

namespace Newtonsoft.Json.Serialization
{
    /// <summary>
    /// An immutable <see cref="ISerializationBinder"/> implementation that replaces fully qualified
    /// type names with short, stable aliases during JSON serialization. This enables correct
    /// polymorphic deserialization of class hierarchies (preserving concrete derived types),
    /// reduces JSON payload size, and decouples serialized data from assembly names and type
    /// namespaces — allowing types to be moved or renamed without breaking deserialization,
    /// as long as the alias remains unchanged.
    /// </summary>
    /// <remarks>
    /// This binder is immutable and thread-safe. Create it via <see cref="TypeAliasRegistry.BuildBinder"/>.
    /// </remarks>
    public sealed class TypeAliasSerializationBinder : ISerializationBinder
    {
        private static readonly DefaultSerializationBinder s_fallbackBinder = new();
        private readonly FrozenDictionary<Type, string> _typeToAlias;
        private readonly FrozenDictionary<string, Type> _aliasToType;

        internal TypeAliasSerializationBinder(
            FrozenDictionary<Type, string> typeToAlias,
            FrozenDictionary<string, Type> aliasToType)
        {
            this._typeToAlias = typeToAlias;
            this._aliasToType = aliasToType;
        }

        /// <inheritdoc />
        public void BindToName(Type serializedType, out string? assemblyName, out string? typeName)
        {
            if (this._typeToAlias.TryGetValue(serializedType, out string? alias))
            {
                assemblyName = null;
                typeName = alias;
            }
            else
            {
                s_fallbackBinder.BindToName(serializedType, out assemblyName, out typeName);
            }
        }

        /// <inheritdoc />
        public Type BindToType(string? assemblyName, string typeName)
        {
            if (assemblyName == null && this._aliasToType.TryGetValue(typeName, out Type? type))
            {
                return type;
            }
            return s_fallbackBinder.BindToType(assemblyName, typeName);
        }
    }
}
