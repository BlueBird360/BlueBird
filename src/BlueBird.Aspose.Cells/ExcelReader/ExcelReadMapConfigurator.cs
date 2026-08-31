using System;
using Aspose.Cells;

namespace BlueBird.Aspose.Cells
{
    /// <summary>
    /// Configures reading and validation for a worksheet column.
    /// </summary>
    public sealed class ExcelReadMapConfigurator<T, TValue>
        where T : class, new()
    {
        private readonly ExcelReadMap<T> _map;

        internal ExcelReadMapConfigurator(ExcelReadMap<T> map)
        {
            this._map = map ?? throw new ArgumentNullException(nameof(map));
        }

        /// <summary>
        /// Sets a custom cell value reader.
        /// </summary>
        /// <param name="valueReader">The value reader; <see langword="null"/> restores default conversion.</param>
        public ExcelReadMapConfigurator<T, TValue> ValueReader(Func<Cell, TValue?>? valueReader)
        {
            if (valueReader == null)
            {
                this._map.ValueReader = null;
            }
            else
            {
                this._map.ValueReader = cell => valueReader(cell);
            }

            return this;
        }

        /// <summary>
        /// Sets whether leading and trailing whitespace is removed from strings. The default is <see langword="true"/>.
        /// </summary>
        /// <param name="enabled">Whether strings are trimmed.</param>
        public ExcelReadMapConfigurator<T, TValue> ValueAutoTrim(bool enabled = true)
        {
            this._map.ValueAutoTrim = enabled;
            return this;
        }

        /// <summary>
        /// Adds a value validation rule. Call this method multiple times to add rules.
        /// </summary>
        /// <param name="validator">The predicate that validates a value.</param>
        /// <param name="errorMessage">The optional message reported when validation fails.</param>
        public ExcelReadMapConfigurator<T, TValue> Validate(Func<TValue?, bool> validator, string? errorMessage = null)
        {
            ArgumentNullException.ThrowIfNull(validator);

            this._map.Validations.Add(new ValueValidation()
            {
                Validator = value => validator((TValue?)value),
                ErrorMessage = errorMessage,
            });

            return this;
        }
    }
}
