# Writing Excel workbooks

`ExcelWriter<T>` writes a sequence of models to an Aspose.Cells worksheet. Columns are configured with value selectors and optional header, body, style, merge, comment, width, and validation settings.

The examples use the `OrderRow` model and `orders` sequence defined in the Basic writing section. Configuration snippets create a new writer where needed.

## Basic writing

```csharp
using BlueBird.Aspose.Cells;

public sealed class OrderRow
{
    public int Id { get; set; }
    public string Customer { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public string Status { get; set; } = string.Empty;
}

var orders = new[]
{
    new OrderRow
    {
        Id = 1,
        Customer = "Ada",
        Total = 125.50m,
        Status = "Paid",
    },
    new OrderRow
    {
        Id = 2,
        Customer = "Ada",
        Total = 80.00m,
        Status = "Paid",
    },
    new OrderRow
    {
        Id = 3,
        Customer = "Grace",
        Total = 240.00m,
        Status = "Paid",
    },
    new OrderRow
    {
        Id = 4,
        Customer = "Grace",
        Total = 60.00m,
        Status = "Paid",
    },
    new OrderRow
    {
        Id = 5,
        Customer = "Ada",
        Total = 310.00m,
        Status = "Paid",
    },
};

var writer = new ExcelWriter<OrderRow>();
writer.AddColumn("Order ID", row => row.Id);
writer.AddColumn("Customer", row => row.Customer);
writer.AddColumn("Total", row => row.Total)
      .BodyCustomFormat("#,##0.00")
      .WidthInCharacters(16);

writer.Write(orders, "orders.xlsx");
```

The indexed overload receives the model's zero-based item index:

```csharp
var writer = new ExcelWriter<OrderRow>();
writer.AddColumn("No.", (row, index) => index + 1);
```

`AddColumn` also accepts a list of header names for multi-level headers. Pass `Array.Empty<string?>()` when a column should not have a header. Headerless and headed columns cannot be mixed; otherwise, all columns must use either one header level or the same number of levels.

## Output targets and options

```csharp
var writer = new ExcelWriter<OrderRow>();
writer.Write(orders, "orders.xlsx", sheetName: "Orders");

using var stream = File.Create("orders.xlsx");
writer.Write(orders, stream, saveFormat: Aspose.Cells.SaveFormat.Xlsx);

var workbook = new Aspose.Cells.Workbook();
writer.Write(orders, workbook.Worksheets[0]);
```

The path and stream overloads create a new workbook. The worksheet overload writes into the supplied worksheet and leaves its workbook open.

| Option | Default | Description |
|--------|---------|-------------|
| `StartRowIndex` | `0` | Zero-based first output row. |
| `StartColumnIndex` | `0` | Zero-based first output column. |
| `AutoMergeHeader` | `true` | Merges adjacent equal values in multi-level headers. |
| `AutoFilter` | `false` | Applies an auto-filter to the header and body range. |
| `AutoFitColumns` | `true` | Fits configured columns to their content. |
| `FreezeHeaderRows` | `false` | Freezes the written header rows. |
| `FreezeColumnCount` | `0` | Freezes leading columns. |
| `ValidationRowCount` | `null` | Number of body rows covered by validation; `null` uses the worksheet limit and `0` disables validation. |
| `IsGridlinesVisible` | `true` | Controls worksheet gridlines. |

Call `ClearColumns` to reuse a writer with a new column definition.

## Headers and styles

```csharp
var writer = new ExcelWriter<OrderRow>();
writer.AddColumn(new[] { "Order", "ID" }, row => row.Id)
      .HeaderFontBold()
      .HeaderHorizontalAlignment(Aspose.Cells.TextAlignmentType.Center);
writer.AddColumn(new[] { "Order", "Customer" }, row => row.Customer);
writer.AddColumn(new[] { "Amounts", "Total" }, row => row.Total)
      .BodyHorizontalAlignment(Aspose.Cells.TextAlignmentType.Right);
```

With `AutoMergeHeader` enabled, adjacent equal values at higher header levels are merged. A single-level header spans all configured header rows and is merged vertically. The lowest header level is never merged horizontally.

Every column can define a column style, header style, and body style. Convenience methods set alignment, font name, font size, font color, bold, italic, wrapping, and number formats. Style callbacks provide full access to the Aspose `Style` object:

```csharp
var writer = new ExcelWriter<OrderRow>();
writer.AddColumn("Total", row => row.Total)
      .HeaderStyle(style => style.Font.IsBold = true)
      .BodyStyle((style, row, value) =>
      {
          style.Number = 4;
          style.Custom = "#,##0.00";
      });
```

Use `ExcelWriterTheme` to initialize the writer's default header and body styles:

```csharp
var theme = new ExcelWriterTheme();
theme.HeaderStyle.Font.IsBold = true;
theme.BodyStyle.Font.Name = "Aptos";

var writer = new ExcelWriter<OrderRow>(theme);
```

Style callbacks are applied when the worksheet is written. The callbacks should not retain or mutate the supplied style after they return.

## Merging body cells

Merge consecutive cells in one column when their selected values are equal:

```csharp
var writer = new ExcelWriter<OrderRow>();
writer.AddColumn("Customer", row => row.Customer)
      .MergeByValue();
writer.Write(orders, "orders-merged.xlsx");
```

Use a composite key when merging depends on the displayed value and another property:

```csharp
var keyedWriter = new ExcelWriter<OrderRow>();
keyedWriter.AddColumn("Status", row => row.Status)
           .MergeBy(row => (row.Customer, row.Status));
keyedWriter.Write(orders, "orders-keyed-merge.xlsx");
```

`MergeBy` compares only the selected key. Include the displayed value and any grouping properties in a composite key when all of them must match, as in `(row.Customer, row.Status)` above. The writer merges only contiguous runs within the items supplied to one `Write` call. Use `ResetMerge` to remove the merge configuration.

## Comments and data validation

Configure a header comment with a callback or a note:

```csharp
var writer = new ExcelWriter<OrderRow>();
writer.AddColumn("Status", row => row.Status)
      .HeaderCommentNote("Allowed order states");
```

Validation helpers configure Excel's data-validation rules for the body range:

```csharp
var writer = new ExcelWriter<OrderRow>();
writer.AddColumn("Status", row => row.Status)
      .ValidationList(new[] { "Pending", "Paid", "Shipped" });

writer.AddColumn("Total", row => row.Total)
      .ValidationRange(1m, 1000m);

writer.AddColumn("Customer", row => row.Customer)
      .ValidationTextLength(1, 20);
```

Each column has one validation configuration; calling another validation method replaces the previous configuration for that column. Use `ValidationRange` for an inclusive minimum and maximum. `ValidationList` uses an inline list and throws when the resulting comma-separated list exceeds Excel's 255-character limit. `ValidationListOrSkip` leaves the current validation unchanged when the limit is exceeded. Use `ResetValidation` to remove a column's validation configuration.

## Column width and visibility

```csharp
var writer = new ExcelWriter<OrderRow>();
writer.AddColumn("Customer", row => row.Customer)
      .WidthInCharacters(24);

writer.AddColumn("Internal ID", row => row.Id)
      .WidthInPixels(120)
      .Hidden();
```

Column widths can be specified in characters, pixels, or inches. Passing `null` leaves the width unchanged. `AutoFitColumns` runs before explicit widths are applied, so an explicit width takes precedence.

## Worksheet events

Use the events to inspect or modify the worksheet immediately before or after the writer applies its output:

```csharp
var writer = new ExcelWriter<OrderRow>();
writer.WorksheetWriting += (_, eventArgs) =>
{
    eventArgs.Worksheet.TabColor = Aspose.Cells.Color.LightBlue;
};

writer.WorksheetWritten += (_, eventArgs) =>
{
    eventArgs.Worksheet.Protect(
        Aspose.Cells.ProtectionType.All,
        "password",
        "Generated by BlueBird.Aspose.Cells");
};
```

`WorksheetWriting` runs before headers and body cells are written. `WorksheetWritten` runs after the writer has applied headers, body cells, styles, merges, filters, widths, hidden columns, and freeze panes.

## Key API

| Type | API | Purpose |
|------|-----|---------|
| `ExcelWriter<T>` | `AddColumn(header, selector)` | Adds a value column. |
| `ExcelWriter<T>` | `AddColumn(headers, selector)` | Adds a multi-level column. |
| `ExcelWriter<T>` | `ClearColumns()` | Removes all configured columns. |
| `ExcelWriter<T>` | `Write(items, path)`, `Write(items, stream)`, `Write(items, worksheet)` | Writes to a file, stream, or worksheet. |
| `ExcelWriter<T>` | `StartRowIndex`, `StartColumnIndex` | Sets the first output cell. |
| `ExcelWriter<T>` | `AutoMergeHeader`, `AutoFilter`, `AutoFitColumns` | Controls header merging, filters, and column fitting. |
| `ExcelWriter<T>` | `FreezeHeaderRows`, `FreezeColumnCount` | Controls frozen rows and leading columns. |
| `ExcelWriter<T>` | `ValidationRowCount`, `IsGridlinesVisible` | Controls validation coverage and worksheet gridlines. |
| `ExcelWriter<T>` | `WorksheetWriting`, `WorksheetWritten` | Handles events before and after worksheet output. |
| `ExcelWriteColumnConfigurator<T, TValue>` | `MergeByValue()`, `MergeBy(keySelector)` | Merges contiguous body cells. |
| `ExcelWriteColumnConfigurator<T, TValue>` | `ColumnStyle`, `HeaderStyle`, `BodyStyle` | Configures Aspose styles. |
| `ExcelWriteColumnConfigurator<T, TValue>` | `HeaderComment`, `HeaderCommentNote` | Adds a header comment. |
| `ExcelWriteColumnConfigurator<T, TValue>` | `Validation`, `ValidationList`, `ValidationTextLength` | Sets the column's data validation configuration. |
| `ExcelWriteColumnConfigurator<T, TValue>` | `ValidationRange`, `ValidationMinimum`, `ValidationMaximum` | Sets the column's numeric or comparable bounds; later validation calls replace earlier ones. |
| `ExcelWriteColumnConfigurator<T, TValue>` | `WidthInCharacters`, `WidthInPixels`, `WidthInInches` | Sets column width. |
| `ExcelWriteColumnConfigurator<T, TValue>` | `Hidden(bool)` | Hides or shows a column. |
| `ExcelWriterTheme` | `HeaderStyle`, `BodyStyle` | Provides default header and body styles. |
