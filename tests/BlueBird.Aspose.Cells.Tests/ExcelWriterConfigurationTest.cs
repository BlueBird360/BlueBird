using System;
using System.Collections.Generic;
using System.Drawing;
using Aspose.Cells;

namespace BlueBird.Aspose.Cells.Tests;

public sealed class ExcelWriterConfigurationTest
{
    [Fact]
    public void Constructor_ExplicitTheme_CopiesStyles()
    {
        var theme = new ExcelWriterTheme();
        theme.HeaderStyle.Font.IsBold = true;
        theme.BodyStyle.Font.IsItalic = true;

        var firstWriter = new ExcelWriter<WriterRow>(theme);
        theme.HeaderStyle.Font.IsItalic = true;
        var secondWriter = new ExcelWriter<WriterRow>(theme);
        firstWriter.DefaultHeaderStyle.Font.IsBold = false;

        Assert.False(firstWriter.DefaultHeaderStyle.Font.IsBold);
        Assert.False(firstWriter.DefaultHeaderStyle.Font.IsItalic);
        Assert.True(secondWriter.DefaultHeaderStyle.Font.IsBold);
        Assert.True(secondWriter.DefaultHeaderStyle.Font.IsItalic);
        Assert.True(secondWriter.DefaultBodyStyle.Font.IsItalic);
    }

    [Fact]
    public void Write_StylesWidthAndComment_AppliesConfiguredValues()
    {
        var writer = new ExcelWriter<WriterRow> { AutoFitColumns = false };
        writer.DefaultHeaderStyle.Font.IsBold = true;
        writer.AddColumn("Quantity", row => row.Quantity)
            .ColumnHorizontalAlignment(TextAlignmentType.Center)
            .HeaderFontColor(Color.Red)
            .BodyFontItalic()
            .HeaderCommentNote("Whole number")
            .WidthInCharacters(18);
        using var workbook = new Workbook();
        Worksheet worksheet = workbook.Worksheets[0];

        writer.Write([new WriterRow("Alice", 10, 12.5m, new DateTime(2026, 8, 31))], worksheet);

        Style headerStyle = worksheet.Cells[0, 0].GetStyle();
        Style bodyStyle = worksheet.Cells[1, 0].GetStyle();
        Assert.True(headerStyle.Font.IsBold);
        Assert.Equal(Color.Red.ToArgb(), headerStyle.Font.Color.ToArgb());
        Assert.Equal(TextAlignmentType.Center, headerStyle.HorizontalAlignment);
        Assert.True(bodyStyle.Font.IsItalic);
        Assert.Equal(TextAlignmentType.Center, bodyStyle.HorizontalAlignment);
        Assert.Equal(18, worksheet.Cells.Columns[0].Width, 3);
        Assert.Equal("Whole number", worksheet.Cells[0, 0].Comment.Note);
    }

    [Fact]
    public void Configuration_NullRequiredArguments_ThrowArgumentNullException()
    {
        var writer = new ExcelWriter<WriterRow>();
        ExcelWriteColumnConfigurator<WriterRow, string> column = writer.AddColumn("Name", row => row.Name);

        Assert.Throws<ArgumentNullException>(() => column.ColumnStyle(null!));
        Assert.Throws<ArgumentNullException>(() => column.ColumnFontName(null!));
        Assert.Throws<ArgumentNullException>(() => column.HeaderStyle((Action<Style>)null!));
        Assert.Throws<ArgumentNullException>(() => column.HeaderStyle((Action<Style, string?>)null!));
        Assert.Throws<ArgumentNullException>(() => column.HeaderStyle((Action<Style, int, string?>)null!));
        Assert.Throws<ArgumentNullException>(() => column.HeaderFontName(null!));
        Assert.Throws<ArgumentNullException>(() => column.BodyStyle((Action<Style>)null!));
        Assert.Throws<ArgumentNullException>(() => column.BodyStyle((Action<Style, WriterRow, string?>)null!));
        Assert.Throws<ArgumentNullException>(() => column.BodyStyle((Action<Style, WriterRow, int, string?>)null!));
        Assert.Throws<ArgumentNullException>(() => column.BodyFontName(null!));
        Assert.Throws<ArgumentNullException>(() => column.HeaderComment(null!));
        Assert.Throws<ArgumentNullException>(() => column.Validation(null!));
        Assert.Throws<ArgumentNullException>(() => column.ValidationList(null!));
        Assert.Throws<ArgumentNullException>(() => column.ValidationListOrSkip(null!));
    }

    [Fact]
    public void Configuration_NegativeNumericValues_ThrowArgumentOutOfRangeException()
    {
        var writer = new ExcelWriter<WriterRow>();

        Assert.Equal("value", Assert.Throws<ArgumentOutOfRangeException>(
            () => writer.StartRowIndex = -1).ParamName);
        Assert.Equal("value", Assert.Throws<ArgumentOutOfRangeException>(
            () => writer.StartColumnIndex = -1).ParamName);
        Assert.Equal("value", Assert.Throws<ArgumentOutOfRangeException>(
            () => writer.ValidationRowCount = -1).ParamName);
        Assert.Equal("value", Assert.Throws<ArgumentOutOfRangeException>(
            () => writer.FreezeColumnCount = -1).ParamName);

        writer.ValidationRowCount = null;
        Assert.Null(writer.ValidationRowCount);
    }

    [Fact]
    public void Write_ValidationConfigurations_AppliesExpectedValidationAreas()
    {
        var writer = new ExcelWriter<WriterRow>
        {
            AutoFitColumns = false,
            ValidationRowCount = 5
        };
        writer.AddColumn("Quantity", row => row.Quantity)
            .ValidationRange(1, 100, "Quantity is out of range.");
        writer.AddColumn("Name", row => row.Name)
            .ValidationList(["Alice", "Bob"], "Name is invalid.");
        writer.AddColumn("Description", row => row.Name)
            .ValidationTextLength(1, 20, "Description is too long.");
        using var workbook = new Workbook();
        Worksheet worksheet = workbook.Worksheets[0];

        writer.Write([new WriterRow("Alice", 10, 12.5m, new DateTime(2026, 8, 31))], worksheet);

        Assert.Equal(3, worksheet.Validations.Count);
        Validation rangeValidation = worksheet.Validations[0];
        Assert.Equal(ValidationType.WholeNumber, rangeValidation.Type);
        Assert.Equal(OperatorType.Between, rangeValidation.Operator);
        Assert.Equal(1, Convert.ToInt32(rangeValidation.Value1));
        Assert.Equal(100, Convert.ToInt32(rangeValidation.Value2));
        Assert.Equal("Quantity is out of range.", rangeValidation.ErrorMessage);
        AssertValidationArea(rangeValidation, 1, 5, 0);

        Validation listValidation = worksheet.Validations[1];
        Assert.Equal(ValidationType.List, listValidation.Type);
        Assert.Equal(["Alice", "Bob"], Assert.IsType<object[]>(listValidation.Value1));
        Assert.True(listValidation.InCellDropDown);
        AssertValidationArea(listValidation, 1, 5, 1);

        Validation textLengthValidation = worksheet.Validations[2];
        Assert.Equal(ValidationType.TextLength, textLengthValidation.Type);
        Assert.Equal(OperatorType.Between, textLengthValidation.Operator);
        Assert.Equal(1, Convert.ToInt32(textLengthValidation.Value1));
        Assert.Equal(20, Convert.ToInt32(textLengthValidation.Value2));
        AssertValidationArea(textLengthValidation, 1, 5, 2);
    }

    [Fact]
    public void Write_ValidationListOrSkip_AppliesValidListAndPreservesValidationForInvalidList()
    {
        var writer = new ExcelWriter<WriterRow>
        {
            AutoFitColumns = false,
            ValidationRowCount = 1
        };
        writer.AddColumn("Valid", row => row.Name)
            .ValidationListOrSkip(["Alice", "Bob"]);
        writer.AddColumn("Invalid", row => row.Name)
            .ValidationTextLength(1, 20)
            .ValidationListOrSkip([new string('A', 256)]);
        using var workbook = new Workbook();

        writer.Write([], workbook.Worksheets[0]);

        Assert.Equal(2, workbook.Worksheets[0].Validations.Count);
        Assert.Equal(ValidationType.List, workbook.Worksheets[0].Validations[0].Type);
        Assert.Equal(ValidationType.TextLength, workbook.Worksheets[0].Validations[1].Type);
    }

    [Fact]
    public void ValidationRange_InvalidArguments_ThrowExpectedExceptions()
    {
        var writer = new ExcelWriter<WriterRow>();
        ExcelWriteColumnConfigurator<WriterRow, int> numberColumn = writer.AddColumn("Quantity", row => row.Quantity);
        ExcelWriteColumnConfigurator<WriterRow, int?> nullableNumberColumn = writer.AddColumn("Nullable", row => (int?)row.Quantity);
        ExcelWriteColumnConfigurator<WriterRow, string> textColumn = writer.AddColumn("Name", row => row.Name);

        Assert.Equal("minimum", Assert.Throws<ArgumentNullException>(
            () => nullableNumberColumn.ValidationMinimum(null)).ParamName);
        Assert.Equal("maximum", Assert.Throws<ArgumentNullException>(
            () => nullableNumberColumn.ValidationMaximum(null)).ParamName);
        Assert.Equal("minimum", Assert.Throws<ArgumentOutOfRangeException>(
            () => numberColumn.ValidationRange(2, 1)).ParamName);
        Assert.Throws<NotSupportedException>(() => textColumn.ValidationRange("A", "Z"));
    }

    private static void AssertValidationArea(Validation validation, int startRow, int endRow, int column)
    {
        CellArea area = Assert.Single(validation.Areas);
        Assert.Equal(startRow, area.StartRow);
        Assert.Equal(endRow, area.EndRow);
        Assert.Equal(column, area.StartColumn);
        Assert.Equal(column, area.EndColumn);
    }

    private sealed record WriterRow(string Name, int Quantity, decimal Amount, DateTime Created);
}
