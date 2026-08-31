using System;
using System.Collections.Generic;
using System.Reflection;
using Aspose.Cells;

namespace BlueBird.Aspose.Cells
{
    /// <summary>
    /// Defines a mapping between a worksheet column and a model.
    /// </summary>
    internal abstract class ExcelReadMap<T>
    {
        public string? ColumnName { get; set; }
        public bool ColumnRequired { get; set; }
        public int? ColumnIndex { get; set; }
        public Func<Cell, object?>? ValueReader { get; set; }
        public bool ValueAutoTrim { get; set; } = true;
        public IList<ValueValidation> Validations { get; } = new List<ValueValidation>();

        public abstract Type ValueType { get; }
        public abstract void SetValue(T model, object? value);
    }

    internal sealed class ValueValidation
    {
        public required Func<object?, bool> Validator { get; set; }
        public string? ErrorMessage { get; set; }
    }

    internal sealed class ExcelReadMapByProperty<T> : ExcelReadMap<T>
    {
        public ExcelReadMapByProperty(PropertyInfo propertyInfo)
        {
            this.PropertyInfo = propertyInfo ?? throw new ArgumentNullException(nameof(propertyInfo));
        }

        public PropertyInfo PropertyInfo { get; }

        public override Type ValueType => this.PropertyInfo.PropertyType;

        public override void SetValue(T model, object? value) => this.PropertyInfo.SetValue(model, value);
    }

    internal sealed class ExcelReadMapByAction<T, TValue> : ExcelReadMap<T>
    {
        public ExcelReadMapByAction(Action<T, TValue?> setValueAction)
        {
            this.SetValueAction = setValueAction ?? throw new ArgumentNullException(nameof(setValueAction));
        }

        public Action<T, TValue?> SetValueAction { get; }

        public override Type ValueType => typeof(TValue);

        public override void SetValue(T model, object? value) => this.SetValueAction(model, (TValue?)value);
    }

    internal sealed class ExcelReadMapByAction<T> : ExcelReadMap<T>
    {
        public ExcelReadMapByAction(Type valueType, Action<T, object?> setValueAction)
        {
            this.ValueType = valueType ?? throw new ArgumentNullException(nameof(valueType));
            this.SetValueAction = setValueAction ?? throw new ArgumentNullException(nameof(setValueAction));
        }

        public Action<T, object?> SetValueAction { get; }

        public override Type ValueType { get; }

        public override void SetValue(T model, object? value) => this.SetValueAction(model, value);
    }
}
