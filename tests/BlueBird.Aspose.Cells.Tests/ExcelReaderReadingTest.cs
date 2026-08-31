using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Aspose.Cells;

namespace BlueBird.Aspose.Cells.Tests;

public sealed class ExcelReaderReadingTest
{
    [Fact]
    public void Read_HeaderRowIndex_StartsReadingAfterConfiguredHeader()
    {
        var reader = new ExcelReader<Person> { HeaderRowIndex = 1 };
        reader.Map(person => person.Name);
        using MemoryStream workbook = ExcelTestWorkbook.Create(
            ["Report title"],
            ["Name"],
            ["Alice"]);

        Person person = Assert.Single(reader.Read(workbook));

        Assert.Equal("Alice", person.Name);
    }

    [Fact]
    public void Read_DataStartRowIndexZero_ReadsFirstRowWithoutHeader()
    {
        var reader = new ExcelReader<Person> { DataStartRowIndex = 0 };
        reader.Map(person => person.Name, 0);
        reader.Map(person => person.Age, 1);
        using MemoryStream workbook = ExcelTestWorkbook.Create(
            ["Alice", 30],
            ["Bob", 40]);

        Person[] people = reader.Read(workbook).ToArray();

        Assert.Collection(
            people,
            person =>
            {
                Assert.Equal("Alice", person.Name);
                Assert.Equal(30, person.Age);
            },
            person =>
            {
                Assert.Equal("Bob", person.Name);
                Assert.Equal(40, person.Age);
            });
    }

    [Fact]
    public void Read_SheetIndex_ReadsConfiguredWorksheet()
    {
        var reader = new ExcelReader<Person> { SheetIndex = 1 };
        reader.Map(person => person.Name);
        using MemoryStream workbook = ExcelTestWorkbook.Create(book =>
        {
            ExcelTestWorkbook.SetRows(book.Worksheets[0], ["Name"], ["Wrong"]);
            Worksheet second = book.Worksheets.Add("Second");
            ExcelTestWorkbook.SetRows(second, ["Name"], ["Alice"]);
        });

        Person person = Assert.Single(reader.Read(workbook));

        Assert.Equal("Alice", person.Name);
    }

    [Fact]
    public void Read_Worksheet_ReadsSpecifiedWorksheet()
    {
        using var workbook = new Workbook();
        Worksheet worksheet = workbook.Worksheets.Add("Orders");
        ExcelTestWorkbook.SetRows(worksheet, ["Name"], ["Alice"]);

        var reader = new ExcelReader<Person> { SheetIndex = int.MaxValue };
        reader.Map(person => person.Name);

        Person person = Assert.Single(reader.Read(worksheet));

        Assert.Equal("Alice", person.Name);
    }

    [Fact]
    public void Read_BlankRows_SkipsThem()
    {
        var reader = new ExcelReader<Person>();
        reader.Map(person => person.Name);
        using MemoryStream workbook = ExcelTestWorkbook.Create(
            ["Name"],
            [null],
            ["Alice"],
            [null]);

        Person person = Assert.Single(reader.Read(workbook));

        Assert.Equal("Alice", person.Name);
    }

    [Fact]
    public void Read_HiddenRows_IncludedByDefaultAndExcludedWhenConfigured()
    {
        var defaultReader = new ExcelReader<Person>();
        defaultReader.Map(person => person.Name);
        var ignoringReader = new ExcelReader<Person> { IgnoreHiddenRows = true };
        ignoringReader.Map(person => person.Name);

        using MemoryStream firstWorkbook = CreateWorkbookWithHiddenRow();
        using MemoryStream secondWorkbook = CreateWorkbookWithHiddenRow();

        Assert.Equal(2, defaultReader.Read(firstWorkbook).Count());
        Person visible = Assert.Single(ignoringReader.Read(secondWorkbook));
        Assert.Equal("Visible", visible.Name);
    }

    [Fact]
    public void Read_ShouldIgnoreRowPredicate_ExcludesMatchingRows()
    {
        var reader = new ExcelReader<Person>
        {
            ShouldIgnoreRow = row => row[0].StringValue == "Ignore",
        };
        reader.Map(person => person.Name);
        using MemoryStream workbook = ExcelTestWorkbook.Create(
            ["Name"],
            ["Ignore"],
            ["Alice"]);

        Person person = Assert.Single(reader.Read(workbook));

        Assert.Equal("Alice", person.Name);
    }

    [Fact]
    public void Read_StringValue_TrimsByDefault()
    {
        var reader = new ExcelReader<Person>();
        reader.Map(person => person.Name);
        using MemoryStream workbook = ExcelTestWorkbook.Create(
            ["Name"],
            ["  Alice  "]);

        Person person = Assert.Single(reader.Read(workbook));

        Assert.Equal("Alice", person.Name);
    }

    [Fact]
    public void Read_ValueAutoTrimDisabled_PreservesWhitespace()
    {
        var reader = new ExcelReader<Person>();
        reader.Map(person => person.Name).ValueAutoTrim(false);
        using MemoryStream workbook = ExcelTestWorkbook.Create(
            ["Name"],
            ["  Alice  "]);

        Person person = Assert.Single(reader.Read(workbook));

        Assert.Equal("  Alice  ", person.Name);
    }

    [Fact]
    public void Read_CustomValueReader_UsesConfiguredValue()
    {
        var reader = new ExcelReader<Person>();
        reader.Map(person => person.Name).ValueReader(cell => $"[{cell.StringValue}]");
        using MemoryStream workbook = ExcelTestWorkbook.Create(
            ["Name"],
            ["Alice"]);

        Person person = Assert.Single(reader.Read(workbook));

        Assert.Equal("[Alice]", person.Name);
    }

    [Fact]
    public void Read_CommonCellTypes_UsesDefaultConversion()
    {
        DateTime date = new DateTime(2026, 8, 30, 12, 30, 0);
        var reader = new ExcelReader<ConversionModel>();
        reader.AutoMap();
        using MemoryStream workbook = ExcelTestWorkbook.Create(
            ["Number", "Amount", "Enabled", "Date", "Text"],
            [42, 12.5m, true, date, 123]);

        ConversionModel model = Assert.Single(reader.Read(workbook));

        Assert.Equal(42, model.Number);
        Assert.Equal(12.5m, model.Amount);
        Assert.True(model.Enabled);
        Assert.Equal(date, model.Date);
        Assert.Equal("123", model.Text);
    }

    [Fact]
    public void Read_GuidAndEnumValues_UsesDefaultConversion()
    {
        Guid identifier = Guid.NewGuid();
        var reader = new ExcelReader<AdditionalConversionModel>();
        reader.AutoMap();
        using MemoryStream workbook = ExcelTestWorkbook.Create(
            ["Identifier", "StatusFromName", "StatusFromNumber"],
            [identifier.ToString("D"), "active", 1]);

        AdditionalConversionModel model = Assert.Single(reader.Read(workbook));

        Assert.Equal(identifier, model.Identifier);
        Assert.Equal(ConversionStatus.Active, model.StatusFromName);
        Assert.Equal(ConversionStatus.Active, model.StatusFromNumber);
    }

    [Fact]
    public void Read_ExcelErrorCell_AggregatesFailureInsteadOfReadingErrorText()
    {
        var reader = new ExcelReader<Person>();
        reader.Map(person => person.Name);
        using MemoryStream workbook = ExcelTestWorkbook.Create(book =>
        {
            Worksheet worksheet = book.Worksheets[0];
            worksheet.Cells[0, 0].Value = "Name";
            worksheet.Cells[1, 0].Formula = "=1/0";
            book.CalculateFormula();
        });

        AggregateException exception = Assert.Throws<AggregateException>(() => reader.Read(workbook));

        FormatException formatException = Assert.IsType<FormatException>(Assert.Single(exception.InnerExceptions));
        Assert.IsType<InvalidCastException>(formatException.InnerException);
    }

    [Fact]
    public void Read_StringNumber_UsesInvariantCulture()
    {
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            var reader = new ExcelReader<InvariantCultureModel>();
            reader.Map(model => model.Amount);
            using MemoryStream workbook = ExcelTestWorkbook.Create(
                ["Amount"],
                ["12.5"]);

            InvariantCultureModel model = Assert.Single(reader.Read(workbook));

            Assert.Equal(12.5m, model.Amount);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void Read_BlankMappedCells_AssignsNullOrDefaultValue()
    {
        var reader = new ExcelReader<NullableModel>();
        reader.Map(model => model.Number);
        reader.Map(model => model.OptionalNumber);
        using MemoryStream workbook = ExcelTestWorkbook.Create(
            ["Number", "OptionalNumber", "Marker"],
            [null, null, "Nonblank row"]);

        NullableModel model = Assert.Single(reader.Read(workbook));

        Assert.Equal(0, model.Number);
        Assert.Null(model.OptionalNumber);
    }

    [Fact]
    public void Read_MultipleConversionFailures_AggregatesEveryFailure()
    {
        var reader = new ExcelReader<Person>();
        reader.Map(person => person.Age);
        using MemoryStream workbook = ExcelTestWorkbook.Create(
            ["Age"],
            ["First invalid value"],
            ["Second invalid value"]);

        AggregateException exception = Assert.Throws<AggregateException>(() => reader.Read(workbook));

        Assert.Equal(2, exception.InnerExceptions.Count);
        Assert.All(exception.InnerExceptions, inner => Assert.IsType<FormatException>(inner));
    }

    [Fact]
    public void Read_CustomValueReaderThrows_WrapsFailureWithCellContext()
    {
        var reader = new ExcelReader<Person>();
        reader.Map(person => person.Name).ValueReader(_ => throw new InvalidOperationException("Conversion failed."));
        using MemoryStream workbook = ExcelTestWorkbook.Create(
            ["Name"],
            ["Alice"]);

        AggregateException exception = Assert.Throws<AggregateException>(() => reader.Read(workbook));

        FormatException formatException = Assert.IsType<FormatException>(Assert.Single(exception.InnerExceptions));
        Assert.Contains("A2", formatException.Message);
        Assert.IsType<InvalidOperationException>(formatException.InnerException);
    }

    [Fact]
    public void Read_Path_ReadsWorkbookAndReleasesFile()
    {
        string path = ExcelTestWorkbook.CreateFile(
            ["Name"],
            ["Alice"]);

        try
        {
            var reader = new ExcelReader<Person>();
            reader.Map(person => person.Name);

            Person person = Assert.Single(reader.Read(path));

            Assert.Equal("Alice", person.Name);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Read_Stream_LeavesCallerOwnedStreamOpen()
    {
        var reader = new ExcelReader<Person>();
        reader.Map(person => person.Name);
        using MemoryStream workbook = ExcelTestWorkbook.Create(
            ["Name"],
            ["Alice"]);

        Assert.Single(reader.Read(workbook));

        Assert.True(workbook.CanRead);
    }

    [Fact]
    public void Read_NullSource_Throws()
    {
        var reader = new ExcelReader<Person>();

        Assert.Throws<ArgumentNullException>(() => reader.Read((string)null!));
        Assert.Throws<ArgumentNullException>(() => reader.Read((Stream)null!));
        Assert.Throws<ArgumentNullException>(() => reader.Read((Worksheet)null!));
    }

    [Fact]
    public void Read_UnreadableStream_ThrowsArgumentException()
    {
        var reader = new ExcelReader<Person>();
        var stream = new MemoryStream();
        stream.Dispose();

        Assert.Throws<ArgumentException>(() => reader.Read(stream));
    }

    [Fact]
    public void Read_VerticallyMergedHeader_UsesTopCellValue()
    {
        var reader = new ExcelReader<Person> { HeaderRowIndex = 1 };
        reader.Map(person => person.Name);
        using MemoryStream workbook = ExcelTestWorkbook.Create(book =>
        {
            Worksheet worksheet = book.Worksheets[0];
            worksheet.Cells[0, 0].Value = "Name";
            worksheet.Cells.Merge(0, 0, 2, 1);
            worksheet.Cells[2, 0].Value = "Alice";
        });

        Person person = Assert.Single(reader.Read(workbook));

        Assert.Equal("Alice", person.Name);
    }

    [Fact]
    public void Read_HorizontallyMergedHeader_Throws()
    {
        var reader = new ExcelReader<Person>();
        reader.Map(person => person.Name);
        using MemoryStream workbook = ExcelTestWorkbook.Create(book =>
        {
            Worksheet worksheet = book.Worksheets[0];
            worksheet.Cells[0, 0].Value = "Name";
            worksheet.Cells.Merge(0, 0, 1, 2);
            worksheet.Cells[1, 0].Value = "Alice";
        });

        Assert.Throws<InvalidOperationException>(() => reader.Read(workbook));
    }

    private static MemoryStream CreateWorkbookWithHiddenRow()
    {
        return ExcelTestWorkbook.Create(book =>
        {
            Worksheet worksheet = book.Worksheets[0];
            ExcelTestWorkbook.SetRows(worksheet,
                ["Name"],
                ["Hidden"],
                ["Visible"]);
            worksheet.Cells.Rows[1].IsHidden = true;
        });
    }

    private sealed class Person
    {
        public string? Name { get; set; }

        public int Age { get; set; }
    }

    private sealed class ConversionModel
    {
        public int Number { get; set; }

        public decimal Amount { get; set; }

        public bool Enabled { get; set; }

        public DateTime Date { get; set; }

        public string? Text { get; set; }
    }

    private sealed class NullableModel
    {
        public int Number { get; set; }

        public int? OptionalNumber { get; set; }
    }

    private sealed class AdditionalConversionModel
    {
        public Guid Identifier { get; set; }

        public ConversionStatus StatusFromName { get; set; }

        public ConversionStatus StatusFromNumber { get; set; }
    }

    private sealed class InvariantCultureModel
    {
        public decimal Amount { get; set; }
    }

    private enum ConversionStatus
    {
        Unknown,
        Active,
    }
}
