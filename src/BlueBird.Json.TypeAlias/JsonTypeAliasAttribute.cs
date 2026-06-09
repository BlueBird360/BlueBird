using System;
using Newtonsoft.Json.Serialization;

namespace Newtonsoft.Json
{
    /// <summary>
    /// Defines an alias for a type when used with <see cref="TypeAliasSerializationBinder"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class JsonTypeAliasAttribute : Attribute
    {
        /// <summary>
        /// Gets the alias. When <c>null</c>, the type name is used as the alias.
        /// </summary>
        public string? Alias { get; }

        /// <summary>
        /// Initializes a new instance of this class. The type name is used as the alias.
        /// </summary>
        public JsonTypeAliasAttribute()
        {
        }

        /// <summary>
        /// Initializes a new instance of this class with the specified alias.
        /// </summary>
        /// <param name="alias">The alias to use, or <c>null</c> to use the type name.</param>
        public JsonTypeAliasAttribute(string? alias)
        {
            this.Alias = alias;
        }
    }
}
