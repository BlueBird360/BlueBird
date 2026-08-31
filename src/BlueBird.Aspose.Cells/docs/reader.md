# Reading Excel data

`ExcelReader<T>` reads rows from an Aspose.Cells worksheet and creates one model instance per non-blank row. It resolves mapped columns from the header row or uses explicit zero-based column indexes.

Examples are independent unless stated otherwise. Named mappings use exact header text matching.

## Basic mapping

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

IReadOnlyList<OrderRow> orders = reader.Read("orders.xlsx");
```

When `columnName` is omitted, `Map` uses the property name. Named mappings are matched against the configured header row using the cell's string value. A named mapping is required by default; pass `columnRequired: false` to ignore a missing column.

Map by zero-based index when the workbook has no stable header names:

```csharp
var indexedReader = new ExcelReader<OrderRow>();
indexedReader.Map(row => row.Id, columnIndex: 0);
indexedReader.Map(row => row.Customer, columnIndex: 1);
```

## Automatic mapping

`AutoMap` registers every non-indexed public instance property that has a public getter and setter. The property name is used as the column name:

```csharp
var reader = new ExcelReader<OrderRow>();
reader.AutoMap(columnRequired: false);
```

`AutoMap` appends mappings to the reader. Do not call it after explicitly mapping the same properties unless duplicate assignments are intended. Use `RemoveMaps` to remove property mappings for one property; it does not remove `MapCustom` mappings. Use `ClearMaps` to remove all mappings before registering a different set.

## Custom assignment and conversion

Use `MapCustom` when the value must be assigned by an action or when the target type is known only at runtime:

```csharp
var customReader = new ExcelReader<OrderRow>();
customReader.MapCustom<string>(
    (row, value) => row.Customer = value ?? string.Empty,
    "Customer");

customReader.MapCustom<decimal>(
    (row, value) => row.Total = value ?? 0m,
    "Total");
```

The runtime-type overload accepts a `Type` and an `Action<T, object?>`:

```csharp
var runtimeReader = new ExcelReader<OrderRow>();
runtimeReader.MapCustom(
    typeof(decimal),
    (row, value) => row.Total = (decimal?)value ?? 0m,
    "Total");
```

Without a custom `ValueReader`, the reader converts cell values to the mapped property or generic value type. It supports nullable types, `DateTime`, `DateOnly`, `TimeOnly`, `Guid`, enums, and other types supported by `Convert.ChangeType`. Excel date and numeric values can be converted to the date types.

## Value readers and string trimming

Configure a custom reader for a mapping when Aspose's cell value needs special interpretation:

```csharp
var valueReader = new ExcelReader<OrderRow>();
valueReader.Map(row => row.Customer)
      .ValueReader(cell => cell.StringValue.Trim().ToUpperInvariant());
```

Mapped strings are trimmed by default. Disable trimming for a mapping with `ValueAutoTrim(false)`.

## Validation

Add one or more mapping-level rules with `Validate`:

```csharp
var validationReader = new ExcelReader<OrderRow>();
validationReader.Map(row => row.Total)
               .Validate(value => value >= 0, "The total must not be negative.");
```

When `ValidateOnRead` is enabled (the default), the reader applies mapping rules and then validates each model with `System.ComponentModel.DataAnnotations`. Values supplied through `ValidationContextItems` are available to every model validation context:

```csharp
var reader = new ExcelReader<OrderRow>();
reader.ValidationContextItems["Today"] = DateOnly.FromDateTime(DateTime.Today);
reader.ValidateOnRead = true;
```

Set `ValidateOnRead = false` to skip both mapping-level and model-level validation.

## Rows, headers, and worksheets

The defaults are:

| Property | Default | Description |
|----------|---------|-------------|
| `HeaderRowIndex` | `0` | Zero-based row containing column headers. |
| `DataStartRowIndex` | `null` | Starts after the header row when `null`. |
| `SheetIndex` | `0` | Zero-based worksheet index when reading a workbook. |
| `IgnoreHiddenRows` | `false` | Whether hidden rows are skipped. |
| `ShouldIgnoreRow` | `null` | Optional predicate for skipping rows. |
| `ValidateOnRead` | `true` | Whether mapped values and models are validated. |

Configure a row predicate for application-specific filtering:

```csharp
var filteringReader = new ExcelReader<OrderRow>();
filteringReader.ShouldIgnoreRow = row =>
    string.Equals(row[0].StringValue, "subtotal", StringComparison.OrdinalIgnoreCase);
```

Blank rows are always skipped. A `Read(string)` call opens and disposes the file stream. A `Read(Stream)` call leaves the supplied stream open. A `Read(Worksheet)` call leaves both the worksheet and its workbook open.

## Reading sources

```csharp
var sourceReader = new ExcelReader<OrderRow>();
IReadOnlyList<OrderRow> fromFile = sourceReader.Read("orders.xlsx");

using FileStream stream = File.OpenRead("orders.xlsx");
IReadOnlyList<OrderRow> fromStream = sourceReader.Read(stream);

using var workbook = new Aspose.Cells.Workbook("orders.xlsx");
IReadOnlyList<OrderRow> fromWorksheet = sourceReader.Read(workbook.Worksheets[0]);
```

## Errors

Cell conversion failures and validation failures are collected while rows are processed and then reported as one `AggregateException`. Each inner exception identifies the cell or row involved. Missing worksheets, missing required columns, and horizontally merged mapped headers throw `InvalidOperationException` before row processing starts.

## Key API

| Type | API | Purpose |
|------|-----|---------|
| `ExcelReader<T>` | `Map(expression, columnName, columnRequired)` | Maps a model property to a named column. |
| `ExcelReader<T>` | `Map(expression, columnIndex)` | Maps a model property to an indexed column. |
| `ExcelReader<T>` | `MapCustom(action, columnName, columnRequired)` | Assigns a converted value with a custom action. |
| `ExcelReader<T>` | `MapCustom(action, columnIndex)` | Uses custom assignment with an indexed column. |
| `ExcelReader<T>` | `MapCustom(type, action, columnName, columnRequired)` | Uses a runtime target type and custom assignment. |
| `ExcelReader<T>` | `AutoMap(columnRequired)` | Maps public readable and writable properties by name. |
| `ExcelReader<T>` | `RemoveMaps(property)`, `ClearMaps()` | Removes property mappings for one property or all mappings. |
| `ExcelReader<T>` | `HeaderRowIndex`, `DataStartRowIndex`, `SheetIndex` | Selects the header, first data row, and worksheet. |
| `ExcelReader<T>` | `IgnoreHiddenRows`, `ShouldIgnoreRow` | Controls hidden-row and custom row filtering. |
| `ExcelReader<T>` | `ValidateOnRead`, `ValidationContextItems` | Controls validation and supplies model validation context data. |
| `ExcelReader<T>` | `Read(path)`, `Read(stream)`, `Read(worksheet)` | Reads from a file, stream, or worksheet. |
| `ExcelReadMapConfigurator<T, TValue>` | `ValueReader(reader)` | Replaces default conversion for one mapping. |
| `ExcelReadMapConfigurator<T, TValue>` | `ValueAutoTrim(enabled)` | Controls trimming of mapped strings. |
| `ExcelReadMapConfigurator<T, TValue>` | `Validate(validator, errorMessage)` | Adds a mapping-level validation rule. |
