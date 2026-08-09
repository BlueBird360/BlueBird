using System;
using System.Collections.Frozen;
using Newtonsoft.Json.Serialization;

namespace BlueBird.Json.TypeAlias
{
    /// <summary>
    /// Resolves registered types through immutable aliases and delegates unregistered mappings
    /// to a fallback <see cref="ISerializationBinder"/>.
    /// </summary>
    /// <remarks>
    /// The type-alias mappings are immutable. This binder is thread-safe when its fallback binder is thread-safe.
    /// Create it via <see cref="TypeAliasRegistry.BuildBinder()"/>.
    /// </remarks>
    public sealed class TypeAliasSerializationBinder : ISerializationBinder
    {
        private readonly FrozenDictionary<Type, string> _typeToAlias;
        private readonly FrozenDictionary<string, Type> _aliasToType;
        private readonly ISerializationBinder _fallbackBinder;

        internal TypeAliasSerializationBinder(
            FrozenDictionary<Type, string> typeToAlias,
            FrozenDictionary<string, Type> aliasToType,
            ISerializationBinder fallbackBinder)
        {
            this._typeToAlias = typeToAlias ?? throw new ArgumentNullException(nameof(typeToAlias));
            this._aliasToType = aliasToType ?? throw new ArgumentNullException(nameof(aliasToType));
            this._fallbackBinder = fallbackBinder ?? throw new ArgumentNullException(nameof(fallbackBinder));
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
                this._fallbackBinder.BindToName(serializedType, out assemblyName, out typeName);
            }
        }

        /// <inheritdoc />
        public Type BindToType(string? assemblyName, string typeName)
        {
            if (assemblyName == null && this._aliasToType.TryGetValue(typeName, out Type? type))
            {
                return type;
            }
            return this._fallbackBinder.BindToType(assemblyName, typeName);
        }
    }
}
