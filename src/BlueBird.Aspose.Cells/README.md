# BlueBird.Aspose.Cells

A lightweight semantic wrapper around [Aspose.Cells](https://products.aspose.com/cells/net/) for reading worksheet rows into .NET models and writing model sequences to Excel workbooks.

The package targets `net8.0` and `net10.0`.

## Installation

```bash
dotnet add package BlueBird.Aspose.Cells
```

`BlueBird.Aspose.Cells` depends on `Aspose.Cells`. Review Aspose's licensing terms before distributing or deploying applications that use this package.

## Reader

`ExcelReader<T>` maps worksheet columns to model properties and creates one model instance for each non-blank data row.

It supports:

- Mapping by header name or zero-based column index.
- Automatic mapping of public readable and writable properties.
- Custom assignment actions, value readers, and string trimming.
- Conversion to common .NET types, including nullable value types, `DateTime`, `DateOnly`, `TimeOnly`, `Guid`, and enums.
- Mapping-level validation and `System.ComponentModel.DataAnnotations` model validation.
- Hidden-row filtering, custom row filtering, and configurable header, data, and worksheet indexes.
- Reading from a file, stream, or existing Aspose `Worksheet`.

### Quick start

```csharp
using BlueBird.Aspose.Cells;

public sealed class OrderRow
{
    public int Id { get; set; }
    public string Customer { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public string Status { get; set; } = string.Empty;
}

var reader = new ExcelReader<OrderRow>();
reader.Map(row => row.Id, "Order ID");
reader.Map(row => row.Customer);
reader.Map(row => row.Total);

var orders = reader.Read("orders.xlsx");
```

The default header row and worksheet index are `0`. Data starts after the header, mapped strings are trimmed, and mapped values and models are validated while `ValidateOnRead` is enabled. Named mappings use exact header text matching; use indexed mappings when header names are unstable.

#### Common options

The following examples use separate readers. Use `AutoMap` when property names match the headers:

```csharp
var autoReader = new ExcelReader<OrderRow>
{
    SheetIndex = 1,
    HeaderRowIndex = 2,
    DataStartRowIndex = 3,
    IgnoreHiddenRows = true,
};

autoReader.AutoMap(columnRequired: false);
```

#### Advanced mapping

For a column that is identified by position, register an indexed mapping directly:

```csharp
var indexedReader = new ExcelReader<OrderRow>();
indexedReader.Map(row => row.Customer, columnIndex: 1)
             .ValueAutoTrim(false);
```

Use `MapCustom` for custom assignment and `ValueReader` for custom conversion:

```csharp
var customReader = new ExcelReader<OrderRow>();
customReader.MapCustom<string>(
    (row, value) => row.Status = value ?? string.Empty,
    "Status");
customReader.Map(row => row.Customer)
            .ValueReader(cell => cell.StringValue.Trim().ToUpperInvariant());
```

Add mapping-level validation when the source value must satisfy a rule:

```csharp
var validatingReader = new ExcelReader<OrderRow>();
validatingReader.Map(row => row.Total)
                .Validate(value => value >= 0, "The total must not be negative.");
```

Cell conversion and validation failures are collected and reported as an `AggregateException`. `Read(Stream)` and `Read(Worksheet)` leave their supplied resources open.

See [Reader documentation](https://github.com/BlueBird360/BlueBird/blob/main/src/BlueBird.Aspose.Cells/docs/reader.md) for custom readers, supported conversions, validation, filtering, and a key API reference.

## Writer

`ExcelWriter<T>` writes model sequences to an Aspose worksheet. Each configured column selects a value from the model and can define its own presentation and validation behavior.

It supports:

- Writing to a new workbook, stream, or existing `Worksheet`.
- Single-level and multi-level headers, with adjacent equal values merged at higher levels when enabled.
- Column, header, and body styles, number formats, and style callbacks.
- Merging consecutive body cells by value or by a custom key.
- Header comments and Excel data validation lists, ranges, and bounds.
- Auto-filters, automatic column fitting, explicit widths, hidden columns, frozen panes, and gridlines.
- Worksheet events before and after writing.

### Quick start

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
    new OrderRow { Id = 1, Customer = "Ada", Total = 125.50m, Status = "Paid" },
    new OrderRow { Id = 2, Customer = "Ada", Total = 80.00m, Status = "Paid" },
    new OrderRow { Id = 3, Customer = "Grace", Total = 240.00m, Status = "Paid" },
    new OrderRow { Id = 4, Customer = "Grace", Total = 60.00m, Status = "Paid" },
    new OrderRow { Id = 5, Customer = "Ada", Total = 310.00m, Status = "Paid" },
};

var writer = new ExcelWriter<OrderRow>();
writer.AddColumn("Order ID", row => row.Id);
writer.AddColumn("Customer", row => row.Customer);
writer.AddColumn("Total", row => row.Total);
writer.Write(orders, "orders.xlsx");
```

### Common options

Add presentation and worksheet options after the columns are configured:

```csharp
var commonWriter = new ExcelWriter<OrderRow>();
commonWriter.AddColumn("Order ID", row => row.Id)
      .HeaderFontBold();
commonWriter.AddColumn("Customer", row => row.Customer);
commonWriter.AddColumn("Total", row => row.Total)
      .BodyCustomFormat("#,##0.00")
      .WidthInCharacters(16);
commonWriter.AddColumn("Status", row => row.Status)
      .ValidationList(new[] { "Pending", "Paid", "Shipped" });

commonWriter.AutoFilter = true;
commonWriter.FreezeHeaderRows = true;
commonWriter.Write(orders, "orders.xlsx");
```

### Advanced features

Pass `Array.Empty<string?>()` to `AddColumn` when a column should not have a header. Headerless and headed columns cannot be mixed.

Use multiple header names for a multi-level header. Adjacent equal values at higher levels are merged automatically:

```csharp
var headerWriter = new ExcelWriter<OrderRow>();
headerWriter.AddColumn(new[] { "Order", "ID" }, row => row.Id)
      .HeaderFontBold();
headerWriter.AddColumn(new[] { "Order", "Customer" }, row => row.Customer);
headerWriter.AddColumn(new[] { "Amounts", "Total" }, row => row.Total)
      .BodyHorizontalAlignment(Aspose.Cells.TextAlignmentType.Right);
```

Merge consecutive body cells and add a validation rule or header comment with the fluent column configurator:

```csharp
var advancedWriter = new ExcelWriter<OrderRow>();
advancedWriter.AddColumn("Customer", row => row.Customer)
      .MergeByValue();

advancedWriter.AddColumn("Status", row => row.Status)
      .HeaderCommentNote("Allowed order states")
      .ValidationList(new[] { "Pending", "Paid", "Shipped" });
advancedWriter.Write(orders, "orders-advanced.xlsx");
```

The sample data contains adjacent rows for the same customer, so `MergeByValue` produces visible merged runs. The final `Ada` row is separated by `Grace` rows and remains a separate run.

`ExcelWriterTheme` initializes default header and body styles. `ValidationList` uses an inline Excel list and is limited to 255 characters; `ValidationListOrSkip` leaves the current validation unchanged when the list is oversized.

### Key options

| Option | Default | Description |
|--------|---------|-------------|
| `StartRowIndex` / `StartColumnIndex` | `0` | Starting cell for output. |
| `AutoMergeHeader` | `true` | Merges adjacent equal multi-level headers. |
| `AutoFilter` | `false` | Applies an auto-filter to the written range. |
| `AutoFitColumns` | `true` | Fits configured columns to their content. |
| `FreezeHeaderRows` | `false` | Freezes the written header rows. |
| `FreezeColumnCount` | `0` | Freezes leading columns. |
| `ValidationRowCount` | `null` | Body rows covered by validation; `null` uses the worksheet limit and `0` disables validation. |
| `IsGridlinesVisible` | `true` | Controls worksheet gridlines. |

See [Writer documentation](https://github.com/BlueBird360/BlueBird/blob/main/src/BlueBird.Aspose.Cells/docs/writer.md) for styles, widths, events, all validation helpers, and a key API reference.

## API overview

| Type | Purpose |
|------|---------|
| `ExcelReader<T>` | Reads worksheet rows and creates models. |
| `ExcelReadMapConfigurator<T, TValue>` | Configures conversion, trimming, and validation for one mapping. |
| `ExcelWriter<T>` | Writes models to an Aspose worksheet or workbook. |
| `ExcelWriteColumnConfigurator<T, TValue>` | Configures a column's styles, merges, comments, widths, visibility, and validation. |
| `ExcelWriterTheme` | Provides default header and body styles. |

## License

MIT
