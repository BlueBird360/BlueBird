using System;
using Aspose.Cells;

namespace BlueBird.Aspose.Cells
{
    /// <summary>
    /// Stores the configuration for one Excel column.
    /// </summary>
    internal sealed class ExcelWriteColumn<T>
    {
        public required string?[] HeaderNames { get; init; }
        public required Func<T, int, object?> ValueSelector { get; init; }
        public Func<T, int, object?>? MergeKeySelector { get; set; }
        public Action<Style>? ColumnStyleAction { get; set; }
        public Action<Style, int, string?>? HeaderStyleAction { get; set; }
        public Action<Style, T, int, object?>? BodyStyleAction { get; set; }
        public Action<Comment>? HeaderCommentAction { get; set; }
        public Action<Validation>? ValidationAction { get; set; }
        public double? Width { get; set; }
        public WidthUnit WidthUnit { get; set; }
        public bool IsHidden { get; set; }
    }

    internal enum WidthUnit
    {
        Character = 0,
        Pixel = 1,
        Inch = 2
    }
}
