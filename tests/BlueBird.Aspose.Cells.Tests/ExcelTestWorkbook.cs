using System;
using System.IO;
using Aspose.Cells;

namespace BlueBird.Aspose.Cells.Tests;

internal static class ExcelTestWorkbook
{
    public static MemoryStream Create(params object?[][] rows)
    {
        return Create(workbook => SetRows(workbook.Worksheets[0], rows));
    }

    public static MemoryStream Create(Action<Workbook> configure)
    {
        using var workbook = new Workbook();
        configure(workbook);

        var stream = new MemoryStream();
        workbook.Save(stream, SaveFormat.Xlsx);
        stream.Position = 0;
        return stream;
    }

    public static string CreateFile(params object?[][] rows)
    {
        string path = Path.Combine(Path.GetTempPath(), $"BlueBird.Aspose.Cells.Tests-{Guid.NewGuid():N}.xlsx");
        using var workbook = new Workbook();
        SetRows(workbook.Worksheets[0], rows);
        workbook.Save(path, SaveFormat.Xlsx);
        return path;
    }

    public static void SetRows(Worksheet worksheet, params object?[][] rows)
    {
        for (int rowIndex = 0; rowIndex < rows.Length; rowIndex++)
        {
            for (int columnIndex = 0; columnIndex < rows[rowIndex].Length; columnIndex++)
            {
                worksheet.Cells[rowIndex, columnIndex].Value = rows[rowIndex][columnIndex];
            }
        }
    }
}
