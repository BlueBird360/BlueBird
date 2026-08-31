using System;
using System.Collections.Generic;
using System.Drawing;
using Aspose.Cells;

namespace BlueBird.Aspose.Cells
{
    /// <summary>
    /// Configures one Excel column.
    /// </summary>
    public sealed class ExcelWriteColumnConfigurator<T, TValue>
    {
        private readonly ExcelWriteColumn<T> _column;

        internal ExcelWriteColumnConfigurator(ExcelWriteColumn<T> column)
        {
            this._column = column ?? throw new ArgumentNullException(nameof(column));
        }

        #region Merge
        /// <summary>
        /// Merges consecutive cells with equal values.
        /// </summary>
        /// <returns>The current configurator.</returns>
        public ExcelWriteColumnConfigurator<T, TValue> MergeByValue()
        {
            this._column.MergeKeySelector = this._column.ValueSelector;
            return this;
        }

        /// <summary>
        /// Merges consecutive cells by a key selected from each item.
        /// </summary>
        /// <typeparam name="TKey">The merge key type.</typeparam>
        /// <param name="keySelector">The function that selects the merge key.</param>
        /// <returns>The current configurator.</returns>
        public ExcelWriteColumnConfigurator<T, TValue> MergeBy<TKey>(Func<T, TKey> keySelector)
        {
            ArgumentNullException.ThrowIfNull(keySelector);

            this._column.MergeKeySelector = (obj, rowIndex) => keySelector(obj);
            return this;
        }

        /// <summary>
        /// Merges consecutive cells by a key selected from each item and its zero-based index.
        /// </summary>
        /// <typeparam name="TKey">The merge key type.</typeparam>
        /// <param name="keySelector">The function that selects the merge key.</param>
        /// <returns>The current configurator.</returns>
        public ExcelWriteColumnConfigurator<T, TValue> MergeBy<TKey>(Func<T, int, TKey> keySelector)
        {
            ArgumentNullException.ThrowIfNull(keySelector);

            this._column.MergeKeySelector = (obj, rowIndex) => keySelector(obj, rowIndex);
            return this;
        }

        /// <summary>
        /// Removes the merge configuration.
        /// </summary>
        /// <returns>The current configurator.</returns>
        public ExcelWriteColumnConfigurator<T, TValue> ResetMerge()
        {
            this._column.MergeKeySelector = null;
            return this;
        }
        #endregion

        #region ColumnStyle
        /// <summary>
        /// Sets the style for the entire column.
        /// </summary>
        public ExcelWriteColumnConfigurator<T, TValue> ColumnStyle(Action<Style> columnStyleAction)
        {
            ArgumentNullException.ThrowIfNull(columnStyleAction);

            this._column.ColumnStyleAction += columnStyleAction;
            return this;
        }

        /// <summary>
        /// Removes the custom column style.
        /// </summary>
        public ExcelWriteColumnConfigurator<T, TValue> ResetColumnStyle()
        {
            this._column.ColumnStyleAction = null;
            return this;
        }

        /// <summary>
        /// Sets horizontal alignment for column values.
        /// </summary>
        public ExcelWriteColumnConfigurator<T, TValue> ColumnHorizontalAlignment(TextAlignmentType textAlignmentType)
        {
            this._column.ColumnStyleAction += style => style.HorizontalAlignment = textAlignmentType;
            return this;
        }

        /// <summary>
        /// Sets vertical alignment for column values.
        /// </summary>
        public ExcelWriteColumnConfigurator<T, TValue> ColumnVerticalAlignment(TextAlignmentType textAlignmentType)
        {
            this._column.ColumnStyleAction += style => style.VerticalAlignment = textAlignmentType;
            return this;
        }

        /// <summary>
        /// Sets the font name for column values.
        /// </summary>
        public ExcelWriteColumnConfigurator<T, TValue> ColumnFontName(string name)
        {
            ArgumentNullException.ThrowIfNull(name);

            this._column.ColumnStyleAction += style => style.Font.Name = name;
            return this;
        }

        /// <summary>
        /// Sets the font size for column values.
        /// </summary>
        public ExcelWriteColumnConfigurator<T, TValue> ColumnFontSize(int size)
        {
            this._column.ColumnStyleAction += style => style.Font.Size = size;
            return this;
        }

        /// <summary>
        /// Sets the font color for column values.
        /// </summary>
        public ExcelWriteColumnConfigurator<T, TValue> ColumnFontColor(Color color)
        {
            this._column.ColumnStyleAction += style => style.Font.Color = color;
            return this;
        }

        /// <summary>
        /// Sets whether column values are bold.
        /// </summary>
        public ExcelWriteColumnConfigurator<T, TValue> ColumnFontBold(bool isBold = true)
        {
            this._column.ColumnStyleAction += style => style.Font.IsBold = isBold;
            return this;
        }

        /// <summary>
        /// Sets whether column values are italic.
        /// </summary>
        public ExcelWriteColumnConfigurator<T, TValue> ColumnFontItalic(bool isItalic = true)
        {
            this._column.ColumnStyleAction += style => style.Font.IsItalic = isItalic;
            return this;
        }

        /// <summary>
        /// Sets whether column values wrap automatically.
        /// </summary>
        public ExcelWriteColumnConfigurator<T, TValue> ColumnTextWrapped(bool isTextWrapped = true)
        {
            this._column.ColumnStyleAction += style => style.IsTextWrapped = isTextWrapped;
            return this;
        }

        /// <summary>
        /// Sets the number format for column values.
        /// </summary>
        public ExcelWriteColumnConfigurator<T, TValue> ColumnCustomFormat(string? format)
        {
            this._column.ColumnStyleAction += style => style.Custom = format;
            return this;
        }
        #endregion

        #region HeaderStyle
        /// <summary>
        /// Sets the header style.
        /// </summary>
        public ExcelWriteColumnConfigurator<T, TValue> HeaderStyle(Action<Style> headerStyleAction)
        {
            ArgumentNullException.ThrowIfNull(headerStyleAction);

            this._column.HeaderStyleAction += (style, rowIndex, value) => headerStyleAction(style);
            return this;
        }

        /// <summary>
        /// Sets the header style using the header value.
        /// </summary>
        public ExcelWriteColumnConfigurator<T, TValue> HeaderStyle(Action<Style, string?> headerStyleAction)
        {
            ArgumentNullException.ThrowIfNull(headerStyleAction);

            this._column.HeaderStyleAction += (style, rowIndex, value) => headerStyleAction(style, value);
            return this;
        }

        /// <summary>
        /// Sets each header cell style using its value and zero-based level.
        /// </summary>
        public ExcelWriteColumnConfigurator<T, TValue> HeaderStyle(Action<Style, int, string?> headerStyleAction)
        {
            ArgumentNullException.ThrowIfNull(headerStyleAction);

            this._column.HeaderStyleAction += headerStyleAction;
            return this;
        }

        /// <summary>
        /// Removes the custom header style.
        /// </summary>
        public ExcelWriteColumnConfigurator<T, TValue> ResetHeaderStyle()
        {
            this._column.HeaderStyleAction = null;
            return this;
        }

        /// <summary>
        /// Sets horizontal alignment for header values.
        /// </summary>
        public ExcelWriteColumnConfigurator<T, TValue> HeaderHorizontalAlignment(TextAlignmentType textAlignmentType)
        {
            this._column.HeaderStyleAction += (style, rowIndex, value) => style.HorizontalAlignment = textAlignmentType;
            return this;
        }

        /// <summary>
        /// Sets vertical alignment for header values.
        /// </summary>
        public ExcelWriteColumnConfigurator<T, TValue> HeaderVerticalAlignment(TextAlignmentType textAlignmentType)
        {
            this._column.HeaderStyleAction += (style, rowIndex, value) => style.VerticalAlignment = textAlignmentType;
            return this;
        }

        /// <summary>
        /// Sets the header font name.
        /// </summary>
        public ExcelWriteColumnConfigurator<T, TValue> HeaderFontName(string name)
        {
            ArgumentNullException.ThrowIfNull(name);

            this._column.HeaderStyleAction += (style, rowIndex, value) => style.Font.Name = name;
            return this;
        }

        /// <summary>
        /// Sets the header font size.
        /// </summary>
        public ExcelWriteColumnConfigurator<T, TValue> HeaderFontSize(int size)
        {
            this._column.HeaderStyleAction += (style, rowIndex, value) => style.Font.Size = size;
            return this;
        }

        /// <summary>
        /// Sets the header font color.
        /// </summary>
        public ExcelWriteColumnConfigurator<T, TValue> HeaderFontColor(Color color)
        {
            this._column.HeaderStyleAction += (style, rowIndex, value) => style.Font.Color = color;
            return this;
        }

        /// <summary>
        /// Sets whether header values are bold.
        /// </summary>
        public ExcelWriteColumnConfigurator<T, TValue> HeaderFontBold(bool isBold = true)
        {
            this._column.HeaderStyleAction += (style, rowIndex, value) => style.Font.IsBold = isBold;
            return this;
        }

        /// <summary>
        /// Sets whether header values are italic.
        /// </summary>
        public ExcelWriteColumnConfigurator<T, TValue> HeaderFontItalic(bool isItalic = true)
        {
            this._column.HeaderStyleAction += (style, rowIndex, value) => style.Font.IsItalic = isItalic;
            return this;
        }

        /// <summary>
        /// Sets whether header values wrap automatically.
        /// </summary>
        public ExcelWriteColumnConfigurator<T, TValue> HeaderTextWrapped(bool isTextWrapped = true)
        {
            this._column.HeaderStyleAction += (style, rowIndex, value) => style.IsTextWrapped = isTextWrapped;
            return this;
        }
        #endregion

        #region BodyStyle
        /// <summary>
        /// Sets the body style.
        /// </summary>
        public ExcelWriteColumnConfigurator<T, TValue> BodyStyle(Action<Style> bodyStyleAction)
        {
            ArgumentNullException.ThrowIfNull(bodyStyleAction);

            this._column.BodyStyleAction += (style, obj, objIndex, value) => bodyStyleAction(style);
            return this;
        }

        /// <summary>
        /// Sets the body style using the item, index, and value.
        /// </summary>
        public ExcelWriteColumnConfigurator<T, TValue> BodyStyle(Action<Style, T, TValue?> bodyStyleAction)
        {
            ArgumentNullException.ThrowIfNull(bodyStyleAction);

            this._column.BodyStyleAction += (style, obj, objIndex, value) => bodyStyleAction(style, obj, (TValue?)value);
            return this;
        }

        /// <summary>
        /// Sets each body cell style using its item, zero-based index, and value.
        /// </summary>
        public ExcelWriteColumnConfigurator<T, TValue> BodyStyle(Action<Style, T, int, TValue?> bodyStyleAction)
        {
            ArgumentNullException.ThrowIfNull(bodyStyleAction);

            this._column.BodyStyleAction += (style, obj, objIndex, value) => bodyStyleAction(style, obj, objIndex, (TValue?)value);
            return this;
        }

        /// <summary>
        /// Removes the custom body style.
        /// </summary>
        public ExcelWriteColumnConfigurator<T, TValue> ResetBodyStyle()
        {
            this._column.BodyStyleAction = null;
            return this;
        }

        /// <summary>
        /// Sets horizontal alignment for body values.
        /// </summary>
        public ExcelWriteColumnConfigurator<T, TValue> BodyHorizontalAlignment(TextAlignmentType textAlignmentType)
        {
            this._column.BodyStyleAction += (style, obj, objIndex, value) => style.HorizontalAlignment = textAlignmentType;
            return this;
        }

        /// <summary>
        /// Sets vertical alignment for body values.
        /// </summary>
        public ExcelWriteColumnConfigurator<T, TValue> BodyVerticalAlignment(TextAlignmentType textAlignmentType)
        {
            this._column.BodyStyleAction += (style, obj, objIndex, value) => style.VerticalAlignment = textAlignmentType;
            return this;
        }

        /// <summary>
        /// Sets the body font name.
        /// </summary>
        public ExcelWriteColumnConfigurator<T, TValue> BodyFontName(string name)
        {
            ArgumentNullException.ThrowIfNull(name);

            this._column.BodyStyleAction += (style, obj, objIndex, value) => style.Font.Name = name;
            return this;
        }

        /// <summary>
        /// Sets the body font size.
        /// </summary>
        public ExcelWriteColumnConfigurator<T, TValue> BodyFontSize(int size)
        {
            this._column.BodyStyleAction += (style, obj, objIndex, value) => style.Font.Size = size;
            return this;
        }

        /// <summary>
        /// Sets the body font color.
        /// </summary>
        public ExcelWriteColumnConfigurator<T, TValue> BodyFontColor(Color color)
        {
            this._column.BodyStyleAction += (style, obj, objIndex, value) => style.Font.Color = color;
            return this;
        }

        /// <summary>
        /// Sets whether body values are bold.
        /// </summary>
        public ExcelWriteColumnConfigurator<T, TValue> BodyFontBold(bool isBold = true)
        {
            this._column.BodyStyleAction += (style, obj, objIndex, value) => style.Font.IsBold = isBold;
            return this;
        }

        /// <summary>
        /// Sets whether body values are italic.
        /// </summary>
        public ExcelWriteColumnConfigurator<T, TValue> BodyFontItalic(bool isItalic = true)
        {
            this._column.BodyStyleAction += (style, obj, objIndex, value) => style.Font.IsItalic = isItalic;
            return this;
        }

        /// <summary>
        /// Sets whether body values wrap automatically.
        /// </summary>
        public ExcelWriteColumnConfigurator<T, TValue> BodyTextWrapped(bool isTextWrapped = true)
        {
            this._column.BodyStyleAction += (style, obj, objIndex, value) => style.IsTextWrapped = isTextWrapped;
            return this;
        }

        /// <summary>
        /// Sets the number format for body values.
        /// </summary>
        public ExcelWriteColumnConfigurator<T, TValue> BodyCustomFormat(string? format)
        {
            this._column.BodyStyleAction += (style, obj, objIndex, value) => style.Custom = format;
            return this;
        }
        #endregion

        #region HeaderComment
        /// <summary>
        /// Sets a comment for the header cell.
        /// </summary>
        public ExcelWriteColumnConfigurator<T, TValue> HeaderComment(Action<Comment> headerCommentAction)
        {
            ArgumentNullException.ThrowIfNull(headerCommentAction);

            this._column.HeaderCommentAction += headerCommentAction;
            return this;
        }

        /// <summary>
        /// Removes the header comment configuration.
        /// </summary>
        public ExcelWriteColumnConfigurator<T, TValue> ResetHeaderComment()
        {
            this._column.HeaderCommentAction = null;
            return this;
        }

        /// <summary>
        /// Sets the header comment text.
        /// </summary>
        public ExcelWriteColumnConfigurator<T, TValue> HeaderCommentNote(string? note)
        {
            this._column.HeaderCommentAction += comment => comment.Note = note;
            return this;
        }
        #endregion

        #region Validation
        /// <summary>
        /// Sets data validation for the column, replacing the previous validation configuration.
        /// </summary>
        public ExcelWriteColumnConfigurator<T, TValue> Validation(Action<Validation> validationAction)
        {
            ArgumentNullException.ThrowIfNull(validationAction);

            this._column.ValidationAction = validationAction;
            return this;
        }

        /// <summary>
        /// Removes column data validation.
        /// </summary>
        public ExcelWriteColumnConfigurator<T, TValue> ResetValidation()
        {
            this._column.ValidationAction = null;
            return this;
        }

        /// <summary>
        /// Sets data validation with an inline list.
        /// The comma-separated list must not exceed 255 characters; otherwise, an <see cref="ArgumentException"/> is thrown.
        /// The candidates are passed through without filtering or normalization.
        /// </summary>
        /// <remarks>
        /// Inline validation lists are limited by Excel to 255 characters in total.
        /// </remarks>
        public ExcelWriteColumnConfigurator<T, TValue> ValidationList(IEnumerable<string?> candidates, string? errorMessage = null)
        {
            if (!TryCreateInlineValidationList(candidates, out string value))
                throw new ArgumentException("The inline validation list must not exceed 255 characters in total.", nameof(candidates));

            return this.SetValidationList(value, errorMessage);
        }

        /// <summary>
        /// Sets data validation with an inline list when it fits Excel’s 255-character limit.
        /// If the limit is exceeded, leaves the current validation unchanged instead of throwing.
        /// The candidates are passed through without filtering or normalization.
        /// </summary>
        /// <remarks>
        /// Inline validation lists are limited by Excel to 255 characters in total.
        /// </remarks>
        public ExcelWriteColumnConfigurator<T, TValue> ValidationListOrSkip(IEnumerable<string?> candidates, string? errorMessage = null)
        {
            if (TryCreateInlineValidationList(candidates, out string value))
                return this.SetValidationList(value, errorMessage);

            return this;
        }

        private static bool TryCreateInlineValidationList(IEnumerable<string?> candidates, out string value)
        {
            ArgumentNullException.ThrowIfNull(candidates);

            value = string.Join(",", candidates);
            return value.Length <= 255;
        }

        private ExcelWriteColumnConfigurator<T, TValue> SetValidationList(string value, string? errorMessage)
        {
            this._column.ValidationAction = validation =>
            {
                validation.Type = ValidationType.List;
                validation.Operator = OperatorType.None;
                validation.InCellDropDown = true;
                validation.Value1 = value;
                validation.ErrorMessage = errorMessage;
            };
            return this;
        }

        /// <summary>
        /// Sets an inclusive text-length range. At least one bound is required.
        /// </summary>
        public ExcelWriteColumnConfigurator<T, TValue> ValidationTextLength(int? minLength, int? maxLength, string? errorMessage = null)
        {
            if (minLength == null && maxLength == null)
                throw new ArgumentException("At least one text length boundary must be specified.");
            if (minLength != null)
                ArgumentOutOfRangeException.ThrowIfNegative(minLength.Value, nameof(minLength));
            if (maxLength != null)
                ArgumentOutOfRangeException.ThrowIfNegative(maxLength.Value, nameof(maxLength));
            if (minLength != null && maxLength != null)
                ArgumentOutOfRangeException.ThrowIfGreaterThan(minLength.Value, maxLength.Value, nameof(minLength));

            OperatorType operatorType;
            int value1;
            int? value2 = null;
            if (minLength == null)
            {
                operatorType = OperatorType.LessOrEqual;
                value1 = maxLength!.Value;
            }
            else if (maxLength == null)
            {
                operatorType = OperatorType.GreaterOrEqual;
                value1 = minLength!.Value;
            }
            else
            {
                operatorType = OperatorType.Between;
                value1 = minLength.Value;
                value2 = maxLength.Value;
            }

            this._column.ValidationAction = validation =>
            {
                validation.Type = ValidationType.TextLength;
                validation.Operator = operatorType;
                validation.Value1 = value1;
                validation.Value2 = value2;
                validation.ErrorMessage = errorMessage;
            };
            return this;
        }

        /// <summary>
        /// Sets inclusive minimum and maximum values for validation, replacing the previous validation configuration.
        /// </summary>
        public ExcelWriteColumnConfigurator<T, TValue> ValidationRange(TValue minimum, TValue maximum, string? errorMessage = null)
        {
            ArgumentNullException.ThrowIfNull(minimum);
            ArgumentNullException.ThrowIfNull(maximum);

            ValidationType validationType = GetRangeValidationType();
            if (Comparer<TValue>.Default.Compare(minimum, maximum) > 0)
                throw new ArgumentOutOfRangeException(nameof(minimum), minimum, "The minimum value must be less than or equal to the maximum value.");

            return this.SetRangeValidation(validationType, OperatorType.Between, minimum, maximum, errorMessage);
        }

        /// <summary>
        /// Sets an inclusive minimum value for validation, replacing the previous validation configuration.
        /// </summary>
        public ExcelWriteColumnConfigurator<T, TValue> ValidationMinimum(TValue minimum, string? errorMessage = null)
        {
            ArgumentNullException.ThrowIfNull(minimum);

            ValidationType validationType = GetRangeValidationType();
            return this.SetRangeValidation(validationType, OperatorType.GreaterOrEqual, minimum, null, errorMessage);
        }

        /// <summary>
        /// Sets an inclusive maximum value for validation, replacing the previous validation configuration.
        /// </summary>
        public ExcelWriteColumnConfigurator<T, TValue> ValidationMaximum(TValue maximum, string? errorMessage = null)
        {
            ArgumentNullException.ThrowIfNull(maximum);

            ValidationType validationType = GetRangeValidationType();
            return this.SetRangeValidation(validationType, OperatorType.LessOrEqual, maximum, null, errorMessage);
        }

        private static ValidationType GetRangeValidationType()
        {
            Type underlyingType = Nullable.GetUnderlyingType(typeof(TValue)) ?? typeof(TValue);
            if (underlyingType == typeof(byte) || underlyingType == typeof(sbyte) ||
                underlyingType == typeof(short) || underlyingType == typeof(ushort) ||
                underlyingType == typeof(int) || underlyingType == typeof(uint) ||
                underlyingType == typeof(long) || underlyingType == typeof(ulong))
                return ValidationType.WholeNumber;
            if (underlyingType == typeof(float) || underlyingType == typeof(double) || underlyingType == typeof(decimal))
                return ValidationType.Decimal;
            if (underlyingType == typeof(DateTime) || underlyingType == typeof(DateOnly))
                return ValidationType.Date;
            if (underlyingType == typeof(TimeSpan) || underlyingType == typeof(TimeOnly))
                return ValidationType.Time;

            throw new NotSupportedException($"Type '{underlyingType}' does not support range data validation.");
        }

        private ExcelWriteColumnConfigurator<T, TValue> SetRangeValidation(ValidationType validationType, OperatorType operatorType, object value1, object? value2, string? errorMessage)
        {
            value1 = ConvertRangeValidationValue(value1);
            if (value2 != null)
                value2 = ConvertRangeValidationValue(value2);

            this._column.ValidationAction = validation =>
            {
                validation.Type = validationType;
                validation.Operator = operatorType;
                validation.Value1 = value1;
                validation.Value2 = value2;
                validation.ErrorMessage = errorMessage;
            };
            return this;
        }

        private static object ConvertRangeValidationValue(object value)
        {
            if (value is DateOnly date)
                return date.ToDateTime(TimeOnly.MinValue);
            if (value is TimeOnly time)
                return time.ToTimeSpan();
            return value;
        }
        #endregion

        #region Width
        /// <summary>
        /// Sets the column width in characters. <see langword="null"/> leaves it unchanged.
        /// </summary>
        public ExcelWriteColumnConfigurator<T, TValue> WidthInCharacters(double? width)
        {
            this._column.Width = width;
            this._column.WidthUnit = WidthUnit.Character;
            return this;
        }

        /// <summary>
        /// Sets the column width in pixels. <see langword="null"/> leaves it unchanged.
        /// </summary>
        public ExcelWriteColumnConfigurator<T, TValue> WidthInPixels(int? width)
        {
            this._column.Width = width;
            this._column.WidthUnit = WidthUnit.Pixel;
            return this;
        }

        /// <summary>
        /// Sets the column width in inches. <see langword="null"/> leaves it unchanged.
        /// </summary>
        public ExcelWriteColumnConfigurator<T, TValue> WidthInInches(double? width)
        {
            this._column.Width = width;
            this._column.WidthUnit = WidthUnit.Inch;
            return this;
        }
        #endregion

        #region IsHidden
        /// <summary>
        /// Sets whether this column is hidden.
        /// </summary>
        public ExcelWriteColumnConfigurator<T, TValue> Hidden(bool isHidden = true)
        {
            this._column.IsHidden = isHidden;
            return this;
        }
        #endregion
    }
}
