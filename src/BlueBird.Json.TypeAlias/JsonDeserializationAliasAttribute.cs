using System;
using System.Reflection;

namespace BlueBird.Json.TypeAlias
{
    /// <summary>
    /// Defines an additional alias for deserialization only. The deserialization alias is automatically registered during
    /// <see cref="TypeAliasRegistry.Register{T}(string?)"/> or <see cref="TypeAliasRegistry.RegisterAssembly(Assembly)"/>.
    /// If <see cref="JsonTypeAliasAttribute"/> is also present, its alias is used for serialization.
    /// </summary>
    /// <remarks>
    /// This attribute is useful for backward compatibility when renaming aliases: old JSON using
    /// the previous alias continues to deserialize correctly while new JSON is serialized with
    /// the updated primary alias.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class JsonDeserializationAliasAttribute : Attribute
    {
        /// <summary>
        /// Gets the deserialization alias.
        /// </summary>
        public string Alias { get; }

        /// <summary>
        /// Initializes a new instance of this class with the specified alias.
        /// </summary>
        /// <param name="alias">The alias to use for deserialization.</param>
        public JsonDeserializationAliasAttribute(string alias)
        {
            this.Alias = alias;
        }
    }
}
