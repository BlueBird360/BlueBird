using System;
using System.Collections.Generic;
using System.IO;
using Aspose.Cells;

namespace BlueBird.Aspose.Cells.Tests;

public sealed class ExcelWriterWritingTest
{
    [Fact]
    public void Write_Worksheet_WritesMappedValuesAtConfiguredPosition()
    {
        var writer = new ExcelWriter<WriterRow>
        {
            StartRowIndex = 2,
            StartColumnIndex = 1,
            AutoFitColumns = false
        };
        writer.AddColumn("Name", row => row.Name);
        writer.AddColumn("Number", (row, index) => index + 1);
        using var workbook = new Workbook();
        Worksheet worksheet = workbook.Worksheets[0];
        worksheet.Cells[0, 0].Value = "Existing";

        writer.Write(
            [
                new WriterRow("Alice", 10, "A"),
                new WriterRow("Bob", 20, "B")
            ],
            worksheet);

        Assert.Equal(2, writer.ColumnCount);
        Assert.Equal(1, writer.HeaderRowCount);
        Assert.Equal("Existing", worksheet.Cells[0, 0].StringValue);
        Assert.Equal("Name", worksheet.Cells[2, 1].StringValue);
        Assert.Equal("Number", worksheet.Cells[2, 2].StringValue);
        Assert.Equal("Alice", worksheet.Cells[3, 1].StringValue);
        Assert.Equal(1, worksheet.Cells[3, 2].IntValue);
        Assert.Equal("Bob", worksheet.Cells[4, 1].StringValue);
        Assert.Equal(2, worksheet.Cells[4, 2].IntValue);
    }

    [Fact]
    public void Write_AutoFilter_AppliesToHeaderAndOutputColumns()
    {
        var writer = new ExcelWriter<WriterRow>
        {
            AutoFitColumns = false,
            AutoFilter = true
        };
        writer.AddColumn("Name", row => row.Name);
        writer.AddColumn("Number", row => row.Quantity);
        using var workbook = new Workbook();
        Worksheet worksheet = workbook.Worksheets[0];

        writer.Write([new WriterRow("Alice", 10, "A")], worksheet);

        CellArea area = worksheet.AutoFilter.GetCellArea();
        Assert.Equal(0, area.StartRow);
        Assert.Equal(0, area.StartColumn);
        Assert.Equal(0, area.EndRow);
        Assert.Equal(1, area.EndColumn);
    }

    [Fact]
    public void Write_HideGridLines_HidesWorksheetGridLines()
    {
        var writer = new ExcelWriter<WriterRow>
        {
            AutoFitColumns = false,
            IsGridlinesVisible = false
        };
        writer.AddColumn("Name", row => row.Name);
        using var workbook = new Workbook();

        writer.Write([new WriterRow("Alice", 10, "A")], workbook.Worksheets[0]);

        Assert.False(workbook.Worksheets[0].IsGridlinesVisible);
    }

    [Fact]
    public void Write_HiddenColumn_HidesConfiguredOutputColumn()
    {
        var writer = new ExcelWriter<WriterRow>
        {
            StartColumnIndex = 2,
            AutoFitColumns = false
        };
        writer.AddColumn("Name", row => row.Name);
        writer.AddColumn("Number", row => row.Quantity).Hidden();
        using var workbook = new Workbook();

        writer.Write([new WriterRow("Alice", 10, "A")], workbook.Worksheets[0]);

        Assert.False(workbook.Worksheets[0].Cells.Columns[2].IsHidden);
        Assert.True(workbook.Worksheets[0].Cells.Columns[3].IsHidden);
    }

    [Fact]
    public void Write_Stream_RaisesEventsAndProducesReadableWorkbook()
    {
        var events = new List<string>();
        var writer = new ExcelWriter<WriterRow> { AutoFitColumns = false };
        writer.AddColumn("Name", row => row.Name);
        writer.WorksheetWriting += (_, e) =>
        {
            events.Add("Writing");
            e.Worksheet.Name = "Data";
            Assert.Null(e.Worksheet.Cells[0, 0].Value);
        };
        writer.WorksheetWritten += (_, e) =>
        {
            events.Add("Written");
            Assert.Equal("Name", e.Worksheet.Cells[0, 0].StringValue);
        };
        using var stream = new MemoryStream();

        writer.Write([new WriterRow("Alice", 10, "A")], stream, saveFormat: SaveFormat.Xlsx);

        Assert.Equal(["Writing", "Written"], events);
        Assert.True(stream.CanWrite);
        stream.Position = 0;
        using var workbook = new Workbook(stream);
        Assert.Equal("Data", workbook.Worksheets[0].Name);
        Assert.Equal("Alice", workbook.Worksheets[0].Cells[1, 0].StringValue);
    }

    [Fact]
    public void Write_Path_CreatesReadableWorkbook()
    {
        string path = Path.Combine(Path.GetTempPath(), $"BlueBird.Aspose.Cells.Tests-{Guid.NewGuid():N}.xlsx");
        try
        {
            var writer = new ExcelWriter<WriterRow> { AutoFitColumns = false };
            writer.AddColumn("Name", row => row.Name);

            writer.Write([new WriterRow("Alice", 10, "A")], path, saveFormat: SaveFormat.Xlsx);

            using var workbook = new Workbook(path);
            Assert.Equal("Alice", workbook.Worksheets[0].Cells[1, 0].StringValue);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Write_NullOutputTarget_ThrowsBeforeSelectingValues()
    {
        bool valueSelected = false;
        var writer = new ExcelWriter<WriterRow>();
        writer.AddColumn("Name", row =>
        {
            valueSelected = true;
            return row.Name;
        });
        WriterRow[] rows = [new WriterRow("Alice", 10, "A")];

        Assert.Throws<ArgumentNullException>(() => writer.Write(rows, (string)null!));
        Assert.Throws<ArgumentNullException>(() => writer.Write(rows, (Stream)null!));
        Assert.False(valueSelected);
    }

    [Fact]
    public void Write_NonZeroStart_FreezesActualHeaderAndColumns()
    {
        var writer = new ExcelWriter<WriterRow>
        {
            StartRowIndex = 2,
            StartColumnIndex = 3,
            AutoFitColumns = false,
            FreezeHeaderRows = true,
            FreezeColumnCount = 1
        };
        writer.AddColumn("Name", row => row.Name);
        using var workbook = new Workbook();
        Worksheet worksheet = workbook.Worksheets[0];

        writer.Write([new WriterRow("Alice", 10, "A")], worksheet);

        worksheet.GetFreezedPanes(
            out int freezeRowIndex,
            out int freezeColumnIndex,
            out int freezeRowCount,
            out int freezeColumnCount);
        Assert.Equal(3, freezeRowIndex);
        Assert.Equal(4, freezeColumnIndex);
        Assert.Equal(3, freezeRowCount);
        Assert.Equal(4, freezeColumnCount);
    }

    [Fact]
    public void Write_MixedHeaderlessAndHeaderColumns_Throws()
    {
        var writer = new ExcelWriter<WriterRow> { AutoFitColumns = false };
        writer.AddColumn(Array.Empty<string>(), row => row.Name);
        writer.AddColumn("Quantity", row => row.Quantity);
        using var workbook = new Workbook();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => writer.Write([new WriterRow("Alice", 10, "A")], workbook.Worksheets[0]));

        Assert.Contains("headerless and headed columns", exception.Message);
    }

    [Fact]
    public void Write_MultiLevelHeaders_MergesExpectedRanges()
    {
        var writer = new ExcelWriter<WriterRow> { AutoFitColumns = false };
        writer.AddColumn(["Identity"], row => row.Quantity);
        writer.AddColumn(["Contact", "Name"], row => row.Name);
        writer.AddColumn(["Contact", "Group"], row => row.Group);
        using var workbook = new Workbook();
        Worksheet worksheet = workbook.Worksheets[0];

        writer.Write([new WriterRow("Alice", 10, "A")], worksheet);

        Assert.Equal(2, writer.HeaderRowCount);
        global::Aspose.Cells.Range identityHeader = worksheet.Cells[0, 0].GetMergedRange();
        Assert.Equal(0, identityHeader.FirstRow);
        Assert.Equal(0, identityHeader.FirstColumn);
        Assert.Equal(2, identityHeader.RowCount);
        Assert.Equal(1, identityHeader.ColumnCount);
        global::Aspose.Cells.Range contactHeader = worksheet.Cells[0, 1].GetMergedRange();
        Assert.Equal(0, contactHeader.FirstRow);
        Assert.Equal(1, contactHeader.FirstColumn);
        Assert.Equal(1, contactHeader.RowCount);
        Assert.Equal(2, contactHeader.ColumnCount);
        Assert.Equal("Alice", worksheet.Cells[2, 1].StringValue);
    }

    [Fact]
    public void Write_MergeByCompositeKey_SeparatesGroups()
    {
        var writer = new ExcelWriter<MergeRow> { AutoFitColumns = false };
        writer.AddColumn("Department", row => row.Department)
            .MergeByValue();
        writer.AddColumn("Group", row => row.Group)
            .MergeBy(row => (row.Department, row.Group));
        writer.AddColumn("Value", row => row.Value)
            .MergeBy(row => (row.Department, row.Group));
        using var workbook = new Workbook();
        Worksheet worksheet = workbook.Worksheets[0];

        writer.Write(
            [
                new MergeRow("A", "X", "Same"),
                new MergeRow("A", "X", "Same"),
                new MergeRow("A", "Y", "Same"),
                new MergeRow("A", "Y", "Same")
            ],
            worksheet);

        global::Aspose.Cells.Range department = worksheet.Cells[1, 0].GetMergedRange();
        Assert.Equal(1, department.FirstRow);
        Assert.Equal(4, department.RowCount);
        global::Aspose.Cells.Range firstGroup = worksheet.Cells[1, 1].GetMergedRange();
        Assert.Equal(1, firstGroup.FirstRow);
        Assert.Equal(2, firstGroup.RowCount);
        global::Aspose.Cells.Range secondGroup = worksheet.Cells[3, 1].GetMergedRange();
        Assert.Equal(3, secondGroup.FirstRow);
        Assert.Equal(2, secondGroup.RowCount);
        global::Aspose.Cells.Range firstValueGroup = worksheet.Cells[1, 2].GetMergedRange();
        Assert.Equal(1, firstValueGroup.FirstRow);
        Assert.Equal(2, firstValueGroup.RowCount);
        global::Aspose.Cells.Range secondValueGroup = worksheet.Cells[3, 2].GetMergedRange();
        Assert.Equal(3, secondValueGroup.FirstRow);
        Assert.Equal(2, secondValueGroup.RowCount);
    }

    [Fact]
    public void Write_ResetMerge_DoesNotMergeEqualValues()
    {
        var writer = new ExcelWriter<MergeRow> { AutoFitColumns = false };
        writer.AddColumn("Value", row => row.Value)
            .MergeByValue()
            .ResetMerge();
        using var workbook = new Workbook();
        Worksheet worksheet = workbook.Worksheets[0];

        writer.Write(
            [
                new MergeRow("A", "X", "Same"),
                new MergeRow("A", "X", "Same")
            ],
            worksheet);

        Assert.False(worksheet.Cells[1, 0].IsMerged);
        Assert.False(worksheet.Cells[2, 0].IsMerged);
    }

    private sealed record WriterRow(string Name, int Quantity, string Group);

    private sealed record MergeRow(string Department, string Group, string Value);
}
