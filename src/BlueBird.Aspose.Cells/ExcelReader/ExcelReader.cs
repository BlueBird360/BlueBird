using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Aspose.Cells;

namespace BlueBird.Aspose.Cells
{
    /// <summary>
    /// Reads worksheet rows into model instances.
    /// </summary>
    public sealed class ExcelReader<T>
        where T : class, new()
    {
        private readonly List<ExcelReadMap<T>> _maps = new List<ExcelReadMap<T>>();
        private int _headerRowIndex = 0;
        private int? _dataStartRowIndex;
        private int _sheetIndex = 0;
        private bool _ignoreHiddenRows = false;
        private Func<Row, bool>? _shouldIgnoreRow;
        private bool _validateOnRead = true;
        private readonly IDictionary<object, object?> _validationContextItems = new Dictionary<object, object?>();

        /// <summary>
        /// Maps a model property to a worksheet column by name.
        /// </summary>
        /// <param name="property">The model property to populate.</param>
        /// <param name="columnName">The column name; <see langword="null"/> uses the property name.</param>
        /// <param name="columnRequired">Whether the column is required.</param>
        /// <returns>A configurator for the mapping.</returns>
        public ExcelReadMapConfigurator<T, TValue> Map<TValue>(Expression<Func<T, TValue>> property, string? columnName = null, bool columnRequired = true)
        {
            ArgumentNullException.ThrowIfNull(property);
            if (columnName != null)
                ArgumentException.ThrowIfNullOrWhiteSpace(columnName);

            PropertyInfo propertyInfo = GetPropertyInfo(property, requirePublicSetter: true);
            ExcelReadMapByProperty<T> map = new ExcelReadMapByProperty<T>(propertyInfo)
            {
                ColumnName = columnName ?? propertyInfo.Name,
                ColumnRequired = columnRequired,
            };
            this._maps.Add(map);
            return new ExcelReadMapConfigurator<T, TValue>(map);
        }

        /// <summary>
        /// Maps a model property to a worksheet column by index.
        /// </summary>
        /// <param name="property">The model property to populate.</param>
        /// <param name="columnIndex">The zero-based column index.</param>
        /// <returns>A configurator for the mapping.</returns>
        public ExcelReadMapConfigurator<T, TValue> Map<TValue>(Expression<Func<T, TValue>> property, int columnIndex)
        {
            ArgumentNullException.ThrowIfNull(property);
            ArgumentOutOfRangeException.ThrowIfNegative(columnIndex);

            PropertyInfo propertyInfo = GetPropertyInfo(property, requirePublicSetter: true);
            ExcelReadMapByProperty<T> map = new ExcelReadMapByProperty<T>(propertyInfo)
            {
                ColumnIndex = columnIndex,
            };
            this._maps.Add(map);
            return new ExcelReadMapConfigurator<T, TValue>(map);
        }

        /// <summary>
        /// Maps a named worksheet column using a custom assignment action.
        /// </summary>
        /// <param name="setValueAction">The action that assigns the converted value to the model.</param>
        /// <param name="columnName">The column name.</param>
        /// <param name="columnRequired">Whether the column is required.</param>
        /// <returns>A configurator for the mapping.</returns>
        public ExcelReadMapConfigurator<T, TValue> MapCustom<TValue>(Action<T, TValue?> setValueAction, string columnName, bool columnRequired = true)
        {
            ArgumentNullException.ThrowIfNull(setValueAction);
            ArgumentException.ThrowIfNullOrWhiteSpace(columnName);

            ExcelReadMapByAction<T, TValue> map = new ExcelReadMapByAction<T, TValue>(setValueAction)
            {
                ColumnName = columnName,
                ColumnRequired = columnRequired,
            };
            this._maps.Add(map);
            return new ExcelReadMapConfigurator<T, TValue>(map);
        }

        /// <summary>
        /// Maps a worksheet column by index using a custom assignment action.
        /// </summary>
        /// <param name="setValueAction">The action that assigns the converted value to the model.</param>
        /// <param name="columnIndex">The zero-based column index.</param>
        /// <returns>A configurator for the mapping.</returns>
        public ExcelReadMapConfigurator<T, TValue> MapCustom<TValue>(Action<T, TValue?> setValueAction, int columnIndex)
        {
            ArgumentNullException.ThrowIfNull(setValueAction);
            ArgumentOutOfRangeException.ThrowIfNegative(columnIndex);

            ExcelReadMapByAction<T, TValue> map = new ExcelReadMapByAction<T, TValue>(setValueAction)
            {
                ColumnIndex = columnIndex,
            };
            this._maps.Add(map);
            return new ExcelReadMapConfigurator<T, TValue>(map);
        }

        /// <summary>
        /// Maps a named worksheet column using a runtime value type and a custom assignment action.
        /// </summary>
        /// <param name="valueType">The target type for cell values.</param>
        /// <param name="setValueAction">The action that assigns the converted value to the model.</param>
        /// <param name="columnName">The column name.</param>
        /// <param name="columnRequired">Whether the column is required.</param>
        /// <returns>A configurator for the mapping.</returns>
        public ExcelReadMapConfigurator<T, object> MapCustom(Type valueType, Action<T, object?> setValueAction, string columnName, bool columnRequired = true)
        {
            ArgumentNullException.ThrowIfNull(valueType);
            ArgumentNullException.ThrowIfNull(setValueAction);
            ArgumentException.ThrowIfNullOrWhiteSpace(columnName);

            ExcelReadMapByAction<T> map = new ExcelReadMapByAction<T>(valueType, setValueAction)
            {
                ColumnName = columnName,
                ColumnRequired = columnRequired,
            };
            this._maps.Add(map);
            return new ExcelReadMapConfigurator<T, object>(map);
        }

        /// <summary>
        /// Maps a worksheet column by index using a runtime value type and a custom assignment action.
        /// </summary>
        /// <param name="valueType">The target type for cell values.</param>
        /// <param name="setValueAction">The action that assigns the converted value to the model.</param>
        /// <param name="columnIndex">The zero-based column index.</param>
        /// <returns>A configurator for the mapping.</returns>
        public ExcelReadMapConfigurator<T, object> MapCustom(Type valueType, Action<T, object?> setValueAction, int columnIndex)
        {
            ArgumentNullException.ThrowIfNull(valueType);
            ArgumentNullException.ThrowIfNull(setValueAction);
            ArgumentOutOfRangeException.ThrowIfNegative(columnIndex);

            ExcelReadMapByAction<T> map = new ExcelReadMapByAction<T>(valueType, setValueAction)
            {
                ColumnIndex = columnIndex,
            };
            this._maps.Add(map);
            return new ExcelReadMapConfigurator<T, object>(map);
        }

        /// <summary>
        /// Maps all public readable and writable properties to columns with matching names.
        /// </summary>
        /// <param name="columnRequired">Whether all mapped columns are required.</param>
        public void AutoMap(bool columnRequired = true)
        {
            IEnumerable<PropertyInfo> propertyInfos =
                from propertyInfo in typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public)
                where propertyInfo.GetIndexParameters().Length == 0 && propertyInfo.GetMethod?.IsPublic == true && propertyInfo.SetMethod?.IsPublic == true
                select propertyInfo;

            foreach (PropertyInfo propertyInfo in propertyInfos)
            {
                this._maps.Add(new ExcelReadMapByProperty<T>(propertyInfo)
                {
                    ColumnName = propertyInfo.Name,
                    ColumnRequired = columnRequired,
                });
            }
        }

        /// <summary>
        /// Removes all property mappings for a model property.
        /// Custom mappings created with <c>MapCustom</c> are not affected.
        /// </summary>
        /// <param name="property">The property whose mappings are removed.</param>
        public void RemoveMaps<TValue>(Expression<Func<T, TValue>> property)
        {
            ArgumentNullException.ThrowIfNull(property);

            PropertyInfo propertyInfo = GetPropertyInfo(property);
            this._maps.RemoveAll(item => item is ExcelReadMapByProperty<T> map && map.PropertyInfo == propertyInfo);
        }

        /// <summary>
        /// Removes all registered mappings.
        /// </summary>
        public void ClearMaps()
        {
            this._maps.Clear();
        }

        /// <summary>
        /// Gets or sets the zero-based header row index. The default is 0.
        /// </summary>
        public int HeaderRowIndex
        {
            get { return this._headerRowIndex; }
            set
            {
                ArgumentOutOfRangeException.ThrowIfNegative(value);

                this._headerRowIndex = value;
            }
        }

        /// <summary>
        /// Gets or sets the zero-based first data row. <see langword="null"/> starts after the header row.
        /// </summary>
        public int? DataStartRowIndex
        {
            get { return this._dataStartRowIndex; }
            set
            {
                if (value.HasValue)
                    ArgumentOutOfRangeException.ThrowIfNegative(value.Value, nameof(value));

                this._dataStartRowIndex = value;
            }
        }

        /// <summary>
        /// Gets or sets the zero-based worksheet index. The default is 0.
        /// </summary>
        public int SheetIndex
        {
            get { return this._sheetIndex; }
            set
            {
                ArgumentOutOfRangeException.ThrowIfNegative(value);

                this._sheetIndex = value;
            }
        }

        /// <summary>
        /// Gets or sets whether hidden rows are ignored. The default is <see langword="false"/>.
        /// </summary>
        public bool IgnoreHiddenRows
        {
            get { return this._ignoreHiddenRows; }
            set { this._ignoreHiddenRows = value; }
        }

        /// <summary>
        /// Gets or sets the predicate used to ignore rows.
        /// </summary>
        public Func<Row, bool>? ShouldIgnoreRow
        {
            get { return this._shouldIgnoreRow; }
            set { this._shouldIgnoreRow = value; }
        }

        /// <summary>
        /// Gets or sets whether mapped values and models are validated while reading. The default is <see langword="true"/>.
        /// </summary>
        public bool ValidateOnRead
        {
            get { return this._validateOnRead; }
            set { this._validateOnRead = value; }
        }

        /// <summary>
        /// Gets the items passed to each model validation context.
        /// </summary>
        public IDictionary<object, object?> ValidationContextItems
        {
            get { return this._validationContextItems; }
        }

        /// <summary>
        /// Reads a workbook from a file.
        /// </summary>
        /// <param name="path">The workbook file path.</param>
        /// <returns>The models created from mapped rows.</returns>
        /// <exception cref="AggregateException">One or more cell or validation errors occurred.</exception>
        /// <exception cref="InvalidOperationException">The worksheet or a required column does not exist, or a mapped header spans multiple columns.</exception>
        public IReadOnlyList<T> Read(string path)
        {
            ArgumentNullException.ThrowIfNull(path);

            using FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read);
            return this.Read(stream);
        }

        /// <summary>
        /// Reads a workbook from a stream. The stream remains open.
        /// </summary>
        /// <param name="stream">A readable workbook stream.</param>
        /// <returns>The models created from mapped rows.</returns>
        /// <exception cref="AggregateException">One or more cell or validation errors occurred.</exception>
        /// <exception cref="InvalidOperationException">The worksheet or a required column does not exist, or a mapped header spans multiple columns.</exception>
        public IReadOnlyList<T> Read(Stream stream)
        {
            ArgumentNullException.ThrowIfNull(stream);
            if (!stream.CanRead)
                throw new ArgumentException("The stream must support reading.", nameof(stream));

            using Workbook workbook = new Workbook(stream);
            if (this._sheetIndex >= workbook.Worksheets.Count)
                throw new InvalidOperationException($"Worksheet at index '{this._sheetIndex}' does not exist. Worksheet count: {workbook.Worksheets.Count}.");

            return this.Read(workbook.Worksheets[this._sheetIndex]);
        }

        /// <summary>
        /// Reads a worksheet. The worksheet and workbook remain open.
        /// </summary>
        /// <param name="worksheet">The worksheet to read.</param>
        /// <returns>The models created from mapped rows.</returns>
        /// <exception cref="AggregateException">One or more cell or validation errors occurred.</exception>
        /// <exception cref="InvalidOperationException">A required column does not exist, or a mapped header spans multiple columns.</exception>
        public IReadOnlyList<T> Read(Worksheet worksheet)
        {
            ArgumentNullException.ThrowIfNull(worksheet);

            List<(ExcelReadMap<T> Map, int ColumnIndex)> resolvedMaps = ResolveMapColumnIndexes(worksheet, this._maps, this._headerRowIndex);

            List<Exception> exceptions = new List<Exception>();
            List<ValidationResult> validationResults = new List<ValidationResult>();
            List<T> models = new List<T>();
            int dataStartRowIndex = this._dataStartRowIndex ?? this._headerRowIndex + 1;
            foreach (Row row in worksheet.Cells.Rows)
            {
                if (row.Index < dataStartRowIndex)
                    continue;

                if (row.IsBlank)
                    continue;

                if (this._ignoreHiddenRows && row.IsHidden)
                    continue;

                if (this._shouldIgnoreRow != null && this._shouldIgnoreRow(row))
                    continue;

                T model = new T();
                foreach ((ExcelReadMap<T> map, int columnIndex) in resolvedMaps)
                {
                    Cell cell = row[columnIndex];

                    object? value;
                    try
                    {
                        value = GetCellValue(map, cell);
                        if (map.ValueAutoTrim && value is string str)
                        {
                            value = str.Trim();
                        }
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(new FormatException($"Cell '{cell.Name}' read failed, the content is '{cell.DisplayStringValue}'. {ex.Message}", ex));
                        continue;
                    }

                    if (this._validateOnRead)
                    {
                        foreach (ValueValidation validation in map.Validations)
                        {
                            if (!validation.Validator(value))
                            {
                                string message = $"Cell '{cell.Name}' validation failed, the content is '{cell.DisplayStringValue}'.";
                                if (!string.IsNullOrWhiteSpace(validation.ErrorMessage))
                                    message += $" {validation.ErrorMessage}";

                                exceptions.Add(new ValidationException(message));
                            }
                        }
                    }

                    map.SetValue(model, value);
                }

                if (this._validateOnRead)
                {
                    validationResults.Clear();
                    ValidationContext validationContext = new ValidationContext(model, this._validationContextItems);
                    Validator.TryValidateObject(model, validationContext, validationResults, true);

                    if (validationResults.Count > 0)
                    {
                        string validationMessage = string.Join(" ", validationResults.Where(item => !string.IsNullOrWhiteSpace(item.ErrorMessage)).Select(item => item.ErrorMessage));
                        string message = $"Row '{row.Index + 1}' validation failed.";
                        if (validationMessage.Length > 0)
                            message += $" {validationMessage}";

                        exceptions.Add(new ValidationException(message));
                    }
                }

                models.Add(model);
            }

            if (exceptions.Count > 0)
            {
                throw new AggregateException(exceptions);
            }
            return models;
        }

        private static List<(ExcelReadMap<T> Map, int ColumnIndex)> ResolveMapColumnIndexes(Worksheet worksheet, IEnumerable<ExcelReadMap<T>> maps, int headerRowIndex)
        {
            Dictionary<ExcelReadMap<T>, int> mapColumnIndexes = maps
                .Where(item => item.ColumnIndex != null)
                .ToDictionary(item => item, item => item.ColumnIndex!.Value);

            List<ExcelReadMap<T>> unresolvedMaps = maps.Where(item => item.ColumnIndex == null).ToList();
            if (unresolvedMaps.Count > 0)
            {
                int maxDataColumn = worksheet.Cells.MaxDataColumn;
                for (int columnIndex = 0; columnIndex <= maxDataColumn && unresolvedMaps.Count > 0; columnIndex++)
                {
                    Cell cell = worksheet.Cells[headerRowIndex, columnIndex];
                    global::Aspose.Cells.Range? mergedRange = null;
                    if (cell.IsMerged)
                    {
                        mergedRange = cell.GetMergedRange();
                        cell = mergedRange[0, 0];
                    }

                    List<ExcelReadMap<T>> matchedMaps = unresolvedMaps.Where(item => item.ColumnName == cell.StringValue).ToList();
                    if (matchedMaps.Count == 0)
                        continue;

                    if (mergedRange != null && mergedRange.ColumnCount > 1)
                        throw new InvalidOperationException($"Cell '{cell.Name}' cannot be merged across columns when used as a header column.");

                    foreach (ExcelReadMap<T> map in matchedMaps)
                    {
                        mapColumnIndexes.Add(map, columnIndex);
                        unresolvedMaps.Remove(map);
                    }
                }

                List<ExcelReadMap<T>> missingRequiredMaps = unresolvedMaps.Where(item => item.ColumnRequired).ToList();
                if (missingRequiredMaps.Count > 0)
                {
                    IEnumerable<string> columnNames = missingRequiredMaps.Select(item => item.ColumnName!).Distinct();
                    throw new InvalidOperationException($"The following required columns are missing: {string.Join(", ", columnNames)}.");
                }
            }

            List<(ExcelReadMap<T> Map, int ColumnIndex)> resolvedMaps = new List<(ExcelReadMap<T> Map, int ColumnIndex)>(mapColumnIndexes.Count);
            foreach (ExcelReadMap<T> map in maps)
            {
                if (mapColumnIndexes.TryGetValue(map, out int columnIndex))
                    resolvedMaps.Add((map, columnIndex));
            }
            return resolvedMaps;
        }

        private static object? GetCellValue(ExcelReadMap<T> map, Cell cell)
        {
            if (map.ValueReader != null)
            {
                return map.ValueReader(cell);
            }

            return ConvertCellValue(cell, map.ValueType);
        }

        private static object? ConvertCellValue(Cell cell, Type valueType)
        {
            if (cell.Type == CellValueType.IsError)
            {
                throw new InvalidCastException($"Cannot convert the Excel error value '{cell.StringValue}' to '{valueType}'.");
            }

            if (cell.Type == CellValueType.IsNull)
            {
                if (valueType.IsValueType && Nullable.GetUnderlyingType(valueType) == null)
                    return Activator.CreateInstance(valueType);
                else
                    return null;
            }

            object value = cell.Value;
            Type targetType = Nullable.GetUnderlyingType(valueType) ?? valueType;

            if (targetType.IsInstanceOfType(value))
                return value;

            if (targetType == typeof(DateTime))
            {
                if (cell.Type == CellValueType.IsNumeric || cell.Type == CellValueType.IsDateTime)
                    return cell.DateTimeValue;

                throw new InvalidCastException($"Cannot convert a cell value of type '{cell.Type}' to '{targetType}'.");
            }

            if (targetType == typeof(DateOnly))
            {
                if (cell.Type == CellValueType.IsNumeric || cell.Type == CellValueType.IsDateTime)
                    return DateOnly.FromDateTime(cell.DateTimeValue);

                throw new InvalidCastException($"Cannot convert a cell value of type '{cell.Type}' to '{targetType}'.");
            }

            if (targetType == typeof(TimeOnly))
            {
                if (cell.Type == CellValueType.IsNumeric || cell.Type == CellValueType.IsDateTime)
                    return TimeOnly.FromDateTime(cell.DateTimeValue);

                throw new InvalidCastException($"Cannot convert a cell value of type '{cell.Type}' to '{targetType}'.");
            }

            if (targetType == typeof(Guid))
            {
                if (value is string stringValue)
                    return Guid.Parse(stringValue);

                throw new InvalidCastException($"Cannot convert a value of type '{value.GetType()}' to '{targetType}'.");
            }

            if (targetType.IsEnum)
            {
                if (value is string stringValue)
                    return Enum.Parse(targetType, stringValue, ignoreCase: true);

                Type enumUnderlyingType = Enum.GetUnderlyingType(targetType);
                object enumValue = Convert.ChangeType(value, enumUnderlyingType, CultureInfo.InvariantCulture);
                return Enum.ToObject(targetType, enumValue);
            }

            return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
        }

        private static PropertyInfo GetPropertyInfo<TObject, TProperty>(
            Expression<Func<TObject, TProperty>> property,
            bool requirePublicSetter = false)
        {
            ArgumentNullException.ThrowIfNull(property);

            if (property.Body is not MemberExpression memberExpression ||
                memberExpression.Member is not PropertyInfo propertyInfo ||
                memberExpression.Expression != property.Parameters[0] ||
                propertyInfo.GetIndexParameters().Length != 0 ||
                (requirePublicSetter && propertyInfo.SetMethod?.IsPublic != true))
            {
                throw new ArgumentException("The specified expression is not a valid property expression.", nameof(property));
            }

            return propertyInfo;
        }
    }
}
