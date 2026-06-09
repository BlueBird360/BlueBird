using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Reflection;

namespace Newtonsoft.Json.Serialization
{
    /// <summary>
    /// A mutable registry for collecting type-to-alias mappings during application startup.
    /// Call <see cref="BuildBinder"/> to produce an immutable <see cref="TypeAliasSerializationBinder"/>.
    /// </summary>
    public sealed class TypeAliasRegistry
    {
        private readonly Dictionary<Type, string> _typeToAlias = new();
        private readonly Dictionary<string, Type> _aliasToType = new();

        /// <summary>
        /// Registers a type with an alias. The alias is resolved in this order:
        /// (1) the <paramref name="alias"/> parameter, if provided;
        /// (2) the <see cref="JsonTypeAliasAttribute"/> on the type;
        /// (3) <c>type.Name</c>.
        /// </summary>
        /// <remarks>
        /// If the type has <see cref="JsonDeserializationAliasAttribute"/> attributes,
        /// those aliases are also registered for deserialization.
        /// </remarks>
        /// <param name="alias">An explicit alias, or <c>null</c> to resolve automatically.</param>
        /// <returns>This registry, to allow method chaining.</returns>
        public TypeAliasRegistry Register<T>(string? alias = null)
            where T : class
        {
            return this.Register(typeof(T), alias);
        }

        /// <summary>
        /// Registers a type with an alias. The alias is resolved in this order:
        /// (1) the <paramref name="alias"/> parameter, if provided;
        /// (2) the <see cref="JsonTypeAliasAttribute"/> on the type;
        /// (3) <c>type.Name</c>.
        /// </summary>
        /// <remarks>
        /// If the type has <see cref="JsonDeserializationAliasAttribute"/> attributes,
        /// those aliases are also registered for deserialization.
        /// </remarks>
        /// <param name="type">The type to register.</param>
        /// <param name="alias">An explicit alias, or <c>null</c> to resolve automatically.</param>
        /// <returns>This registry, to allow method chaining.</returns>
        public TypeAliasRegistry Register(Type type, string? alias = null)
        {
            ArgumentNullException.ThrowIfNull(type);

            alias ??= (type.GetCustomAttribute<JsonTypeAliasAttribute>()?.Alias ?? type.Name);
            this.RegisterCore(type, alias);
            foreach (var attribute in type.GetCustomAttributes<JsonDeserializationAliasAttribute>())
            {
                this.RegisterDeserializationAliasCore(type, attribute.Alias);
            }
            return this;
        }

        /// <summary>
        /// Registers multiple types. For each type, the alias is resolved from
        /// <see cref="JsonTypeAliasAttribute"/> or <c>type.Name</c>.
        /// </summary>
        /// <remarks>
        /// If the type has <see cref="JsonDeserializationAliasAttribute"/> attributes,
        /// those aliases are also registered for deserialization.
        /// </remarks>
        /// <param name="types">The types to register.</param>
        /// <returns>This registry, to allow method chaining.</returns>
        public TypeAliasRegistry Register(IEnumerable<Type> types)
        {
            ArgumentNullException.ThrowIfNull(types);

            foreach (Type type in types)
            {
                this.Register(type);
            }
            return this;
        }

        /// <summary>
        /// Registers all types in the specified assembly that have
        /// <see cref="JsonTypeAliasAttribute"/> or <see cref="JsonDeserializationAliasAttribute"/>.
        /// The two attributes are independent:
        /// <list type="bullet">
        /// <item><description><see cref="JsonTypeAliasAttribute"/> registers the primary alias (used for both serialization and deserialization).</description></item>
        /// <item><description><see cref="JsonDeserializationAliasAttribute"/> registers additional aliases used only for deserialization.</description></item>
        /// </list>
        /// A type with only <see cref="JsonDeserializationAliasAttribute"/> does not have a primary alias registered;
        /// serialization falls back to the default Newtonsoft.Json behavior.
        /// Types without either attribute are silently skipped.
        /// </summary>
        /// <param name="assembly">The assembly to scan.</param>
        /// <returns>This registry, to allow method chaining.</returns>
        public TypeAliasRegistry RegisterAssembly(Assembly assembly)
        {
            ArgumentNullException.ThrowIfNull(assembly);

            foreach (Type type in assembly.GetTypes())
            {
                var aliasAttribute = type.GetCustomAttribute<JsonTypeAliasAttribute>();
                if (aliasAttribute != null)
                {
                    this.RegisterCore(type, aliasAttribute.Alias ?? type.Name);
                }

                var deserAttributes = type.GetCustomAttributes<JsonDeserializationAliasAttribute>();
                foreach (var attribute in deserAttributes)
                {
                    this.RegisterDeserializationAliasCore(type, attribute.Alias);
                }
            }
            return this;
        }

        /// <summary>
        /// Registers an alias for deserialization only. If the type also has a primary alias
        /// (from <see cref="Register{T}"/>), serialization still uses the primary alias.
        /// If no primary alias is registered, serialization falls back to the default
        /// Newtonsoft.Json behavior. This is useful for backward compatibility when
        /// renaming aliases.
        /// </summary>
        /// <param name="alias">The additional alias for deserialization.</param>
        /// <returns>This registry, to allow method chaining.</returns>
        public TypeAliasRegistry RegisterDeserializationAlias<T>(string alias)
            where T : class
        {
            return this.RegisterDeserializationAlias(typeof(T), alias);
        }

        /// <summary>
        /// Registers multiple aliases for deserialization only. If the type also has a primary alias
        /// (from <see cref="Register{T}"/>), serialization still uses the primary alias.
        /// If no primary alias is registered, serialization falls back to the default
        /// Newtonsoft.Json behavior. This is useful for backward compatibility when
        /// renaming aliases.
        /// </summary>
        /// <param name="aliases">The additional aliases for deserialization.</param>
        /// <returns>This registry, to allow method chaining.</returns>
        public TypeAliasRegistry RegisterDeserializationAlias<T>(IEnumerable<string> aliases)
            where T : class
        {
            return this.RegisterDeserializationAlias(typeof(T), aliases);
        }

        /// <summary>
        /// Registers an alias for deserialization only. If the type also has a primary alias
        /// (from <see cref="Register(Type, string?)"/>), serialization still uses the primary alias.
        /// If no primary alias is registered, serialization falls back to the default
        /// Newtonsoft.Json behavior. This is useful for backward compatibility when
        /// renaming aliases.
        /// </summary>
        /// <param name="type">The type to add an alias for.</param>
        /// <param name="alias">The additional alias for deserialization.</param>
        /// <returns>This registry, to allow method chaining.</returns>
        public TypeAliasRegistry RegisterDeserializationAlias(Type type, string alias)
        {
            ArgumentNullException.ThrowIfNull(type);
            ArgumentNullException.ThrowIfNull(alias);

            this.RegisterDeserializationAliasCore(type, alias);
            return this;
        }

        /// <summary>
        /// Registers multiple aliases for deserialization only. If the type also has a primary alias
        /// (from <see cref="Register(Type, string?)"/>), serialization still uses the primary alias.
        /// If no primary alias is registered, serialization falls back to the default
        /// Newtonsoft.Json behavior. This is useful for backward compatibility when
        /// renaming aliases.
        /// </summary>
        /// <param name="type">The type to add aliases for.</param>
        /// <param name="aliases">The additional aliases for deserialization.</param>
        /// <returns>This registry, to allow method chaining.</returns>
        public TypeAliasRegistry RegisterDeserializationAlias(Type type, IEnumerable<string> aliases)
        {
            ArgumentNullException.ThrowIfNull(type);
            ArgumentNullException.ThrowIfNull(aliases);

            foreach (string alias in aliases)
            {
                this.RegisterDeserializationAliasCore(type, alias);
            }
            return this;
        }

        /// <summary>
        /// Builds an immutable <see cref="TypeAliasSerializationBinder"/> from the
        /// registered type-alias mappings. After calling this method, the returned binder
        /// cannot be modified.
        /// </summary>
        /// <returns>A new <see cref="TypeAliasSerializationBinder"/> instance.</returns>
        public TypeAliasSerializationBinder BuildBinder()
        {
            return new TypeAliasSerializationBinder(
                this._typeToAlias.ToFrozenDictionary(),
                this._aliasToType.ToFrozenDictionary());
        }

        private void RegisterCore(Type type, string alias)
        {
            if (!type.IsClass)
                throw new ArgumentException($"Only class types can be registered. \"{type.FullName}\" is not a class.");

            if (string.IsNullOrWhiteSpace(alias))
                throw new ArgumentException($"The alias for type \"{type.FullName}\" cannot be null or whitespace.");

            if (this._typeToAlias.TryGetValue(type, out string? existAlias))
            {
                if (alias != existAlias)
                {
                    throw new ArgumentException(
                        $"Type \"{type.FullName}\" is already registered with alias \"{existAlias}\" and cannot be re-registered with alias \"{alias}\". Use RegisterDeserializationAlias to add additional deserialization aliases.");
                }
            }

            if (this._aliasToType.TryGetValue(alias, out Type? existType))
            {
                if (existType != type)
                {
                    throw new ArgumentException(
                        $"Alias \"{alias}\" is already registered by type \"{existType.FullName}\" and cannot be used for type \"{type.FullName}\".");
                }
            }

            this._typeToAlias.TryAdd(type, alias);
            this._aliasToType.TryAdd(alias, type);
        }

        private void RegisterDeserializationAliasCore(Type type, string alias)
        {
            if (!type.IsClass)
                throw new ArgumentException($"Only class types can be registered. \"{type.FullName}\" is not a class.");

            if (string.IsNullOrWhiteSpace(alias))
                throw new ArgumentException($"The alias for type \"{type.FullName}\" cannot be null or whitespace.");

            if (this._aliasToType.TryGetValue(alias, out Type? existType))
            {
                if (existType != type)
                {
                    throw new ArgumentException(
                        $"Alias \"{alias}\" is already registered by type \"{existType.FullName}\" and cannot be used for type \"{type.FullName}\".");
                }
                return;
            }

            this._aliasToType.Add(alias, type);
        }
    }
}
