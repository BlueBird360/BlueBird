using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Aspose.Cells;

namespace BlueBird.Aspose.Cells
{
    /// <summary>
    /// Writes configured columns and data to an Aspose worksheet.
    /// </summary>
    public sealed class ExcelWriter<T>
    {
        private readonly CellsFactory _cellsFactory = new CellsFactory();
        private readonly List<ExcelWriteColumn<T>> _columns = new List<ExcelWriteColumn<T>>();
        private int _headerRowCount;

        /// <summary>
        /// Gets the default header style initialized by the writer's theme.
        /// </summary>
        public Style DefaultHeaderStyle { get; }

        /// <summary>
        /// Gets the default body style initialized by the writer's theme.
        /// </summary>
        public Style DefaultBodyStyle { get; }

        /// <summary>
        /// Gets or sets the zero-based row at which output starts. Defaults to 0.
        /// </summary>
        public int StartRowIndex
        {
            get;
            set
            {
                ArgumentOutOfRangeException.ThrowIfNegative(value);
                field = value;
            }
        }

        /// <summary>
        /// Gets or sets the zero-based column at which output starts. Defaults to 0.
        /// </summary>
        public int StartColumnIndex
        {
            get;
            set
            {
                ArgumentOutOfRangeException.ThrowIfNegative(value);
                field = value;
            }
        }

        /// <summary>
        /// Gets or sets the number of rows covered by data validation. Null extends validation to the worksheet limit; zero disables it. Defaults to null.
        /// </summary>
        public int? ValidationRowCount
        {
            get;
            set
            {
                if (value != null)
                    ArgumentOutOfRangeException.ThrowIfNegative(value.Value, nameof(value));

                field = value;
            }
        }

        /// <summary>
        /// Gets or sets whether adjacent multi-level headers are merged automatically. Defaults to true.
        /// </summary>
        public bool AutoMergeHeader { get; set; } = true;

        /// <summary>
        /// Gets or sets whether an auto-filter is applied to the header and body range. Defaults to false.
        /// </summary>
        public bool AutoFilter { get; set; }

        /// <summary>
        /// Gets or sets whether configured columns are autofit. Defaults to true.
        /// </summary>
        public bool AutoFitColumns { get; set; } = true;

        /// <summary>
        /// Gets or sets whether header rows are frozen. Defaults to false.
        /// </summary>
        public bool FreezeHeaderRows { get; set; }

        /// <summary>
        /// Gets or sets the number of leading columns to freeze. Zero disables column freezing.
        /// </summary>
        public int FreezeColumnCount
        {
            get;
            set
            {
                ArgumentOutOfRangeException.ThrowIfNegative(value);
                field = value;
            }
        }

        /// <summary>
        /// Gets or sets whether worksheet gridlines are visible. Defaults to true.
        /// </summary>
        public bool IsGridlinesVisible { get; set; } = true;

        /// <summary>
        /// Initializes a writer with neutral default styles.
        /// </summary>
        public ExcelWriter()
        {
            this.DefaultHeaderStyle = this._cellsFactory.CreateStyle();
            this.DefaultBodyStyle = this._cellsFactory.CreateStyle();
        }

        /// <summary>
        /// Initializes a writer with the specified theme.
        /// </summary>
        /// <param name="theme">The theme used to initialize the writer's default styles.</param>
        public ExcelWriter(ExcelWriterTheme theme)
            : this()
        {
            ArgumentNullException.ThrowIfNull(theme);

            this.DefaultHeaderStyle.Copy(theme.HeaderStyle);
            this.DefaultBodyStyle.Copy(theme.BodyStyle);
        }

        #region Metadata
        /// <summary>
        /// Gets the number of configured columns.
        /// </summary>
        public int ColumnCount
        {
            get { return this._columns.Count; }
        }

        /// <summary>
        /// Gets the number of header rows required by the configured columns.
        /// </summary>
        public int HeaderRowCount
        {
            get { return this._headerRowCount; }
        }
        #endregion

        #region Column
        /// <summary>
        /// Adds a column and returns its fluent configurator.
        /// </summary>
        /// <typeparam name="TValue">The selected value type.</typeparam>
        /// <param name="headerName">The header text; <see langword="null"/> writes a blank header.</param>
        /// <param name="valueSelector">The function that selects the cell value.</param>
        public ExcelWriteColumnConfigurator<T, TValue> AddColumn<TValue>(string? headerName, Func<T, TValue> valueSelector)
        {
            return this.AddColumn([headerName], valueSelector);
        }

        /// <summary>
        /// Adds an indexed value column and returns its fluent configurator.
        /// </summary>
        /// <typeparam name="TValue">The selected value type.</typeparam>
        /// <param name="headerName">The header text; <see langword="null"/> writes a blank header.</param>
        /// <param name="valueSelector">The function that selects the cell value and receives its zero-based item index.</param>
        public ExcelWriteColumnConfigurator<T, TValue> AddColumn<TValue>(string? headerName, Func<T, int, TValue> valueSelector)
        {
            return this.AddColumn([headerName], valueSelector);
        }

        /// <summary>
        /// Adds a column with one or more header levels and returns its fluent configurator.
        /// Pass an empty list to create a headerless column.
        /// </summary>
        /// <typeparam name="TValue">The selected value type.</typeparam>
        /// <param name="headerNames">The header levels, or an empty list when no header is required.</param>
        /// <param name="valueSelector">The function that selects the cell value.</param>
        public ExcelWriteColumnConfigurator<T, TValue> AddColumn<TValue>(IReadOnlyList<string?> headerNames, Func<T, TValue> valueSelector)
        {
            ArgumentNullException.ThrowIfNull(headerNames);
            ArgumentNullException.ThrowIfNull(valueSelector);

            string?[] copiedHeaderNames = headerNames.ToArray();
            ExcelWriteColumn<T> column = new ExcelWriteColumn<T>()
            {
                HeaderNames = copiedHeaderNames,
                ValueSelector = (item, index) => valueSelector(item)
            };
            this._columns.Add(column);
            this._headerRowCount = Math.Max(this._headerRowCount, copiedHeaderNames.Length);
            return new ExcelWriteColumnConfigurator<T, TValue>(column);
        }

        /// <summary>
        /// Adds an indexed column with one or more header levels and returns its fluent configurator.
        /// Pass an empty list to create a headerless column.
        /// </summary>
        /// <typeparam name="TValue">The selected value type.</typeparam>
        /// <param name="headerNames">The header levels, or an empty list when no header is required.</param>
        /// <param name="valueSelector">The function that selects the cell value and receives its zero-based item index.</param>
        public ExcelWriteColumnConfigurator<T, TValue> AddColumn<TValue>(IReadOnlyList<string?> headerNames, Func<T, int, TValue> valueSelector)
        {
            ArgumentNullException.ThrowIfNull(headerNames);
            ArgumentNullException.ThrowIfNull(valueSelector);

            string?[] copiedHeaderNames = headerNames.ToArray();
            ExcelWriteColumn<T> column = new ExcelWriteColumn<T>()
            {
                HeaderNames = copiedHeaderNames,
                ValueSelector = (item, index) => valueSelector(item, index)
            };
            this._columns.Add(column);
            this._headerRowCount = Math.Max(this._headerRowCount, copiedHeaderNames.Length);
            return new ExcelWriteColumnConfigurator<T, TValue>(column);
        }

        /// <summary>
        /// Removes all columns.
        /// </summary>
        public void ClearColumns()
        {
            this._columns.Clear();
            this._headerRowCount = 0;
        }
        #endregion

        #region Write
        /// <summary>
        /// Writes the supplied items to a new workbook and saves it to the specified path.
        /// </summary>
        /// <param name="items">The items to write.</param>
        /// <param name="path">The output workbook path.</param>
        /// <param name="sheetName">The optional worksheet name.</param>
        /// <param name="saveFormat">The output format.</param>
        public void Write(IEnumerable<T> items, string path, string? sheetName = null, SaveFormat saveFormat = SaveFormat.Auto)
        {
            ArgumentNullException.ThrowIfNull(items);
            ArgumentNullException.ThrowIfNull(path);

            using Workbook workbook = new Workbook();
            Worksheet worksheet = workbook.Worksheets[0];
            if (sheetName != null)
            {
                worksheet.Name = sheetName;
            }

            this.Write(items, worksheet);
            workbook.Save(path, saveFormat);
        }

        /// <summary>
        /// Writes the supplied items to a new workbook and saves it to the specified stream.
        /// </summary>
        /// <param name="items">The items to write.</param>
        /// <param name="stream">The writable output stream.</param>
        /// <param name="sheetName">The optional worksheet name.</param>
        /// <param name="saveFormat">The output format.</param>
        public void Write(IEnumerable<T> items, Stream stream, string? sheetName = null, SaveFormat saveFormat = SaveFormat.Auto)
        {
            ArgumentNullException.ThrowIfNull(items);
            ArgumentNullException.ThrowIfNull(stream);
            if (!stream.CanWrite)
                throw new ArgumentException("The stream must support writing.", nameof(stream));

            using Workbook workbook = new Workbook();
            Worksheet worksheet = workbook.Worksheets[0];
            if (sheetName != null)
            {
                worksheet.Name = sheetName;
            }

            this.Write(items, worksheet);
            workbook.Save(stream, saveFormat);
        }

        /// <summary>
        /// Writes the supplied items to the specified worksheet.
        /// </summary>
        /// <param name="items">The items to write.</param>
        /// <param name="worksheet">The worksheet to populate.</param>
        public void Write(IEnumerable<T> items, Worksheet worksheet)
        {
            ArgumentNullException.ThrowIfNull(items);
            ArgumentNullException.ThrowIfNull(worksheet);

            this.CheckHeader();

            worksheet.IsGridlinesVisible = this.IsGridlinesVisible;

            this.OnWorksheetWriting(new WorksheetWritingEventArgs(worksheet));

            this.ApplyColumnStyles(worksheet);

            Style[] headerBaseStyles = this.CreateColumnBaseStyles(this.DefaultHeaderStyle);
            Style[] bodyBaseStyles = this.CreateColumnBaseStyles(this.DefaultBodyStyle);

            this.WriteHeader(worksheet, headerBaseStyles);
            this.WriteBody(worksheet, items, bodyBaseStyles);

            this.ApplyAutoFilter(worksheet);
            this.ApplyAutoFitColumns(worksheet);
            this.ApplyColumnWidths(worksheet);
            this.ApplyHiddenColumns(worksheet);
            this.ApplyFreezePanes(worksheet);

            this.OnWorksheetWritten(new WorksheetWrittenEventArgs(worksheet));
        }
        #endregion

        #region CheckHeader
        private void CheckHeader()
        {
            if (this._columns.Count > 0)
            {
                if (!this._columns.All(item => item.HeaderNames.Length == 1 || item.HeaderNames.Length == this._headerRowCount))
                {
                    throw new InvalidOperationException("Header definitions are inconsistent: headerless and headed columns cannot be mixed; columns with multiple header levels must use the same number of levels.");
                }
            }
        }
        #endregion

        #region ApplyColumnStyles
        private void ApplyColumnStyles(Worksheet worksheet)
        {
            for (int i = 0; i < this._columns.Count; i++)
            {
                ExcelWriteColumn<T> column = this._columns[i];
                if (column.ColumnStyleAction != null)
                {
                    Style style = this._cellsFactory.CreateStyle();
                    column.ColumnStyleAction(style);
                    worksheet.Cells.Columns[this.StartColumnIndex + i].ApplyStyle(style, new StyleFlag() { All = true });
                }
            }
        }
        #endregion

        #region CreateColumnBaseStyles
        private Style[] CreateColumnBaseStyles(Style defaultStyle)
        {
            Style[] baseStyles = new Style[this._columns.Count];
            for (int i = 0; i < this._columns.Count; i++)
            {
                Style style = this._cellsFactory.CreateStyle();
                style.Copy(defaultStyle);
                this._columns[i].ColumnStyleAction?.Invoke(style);
                baseStyles[i] = style;
            }
            return baseStyles;
        }
        #endregion

        #region WriteHeader
        private void WriteHeader(Worksheet worksheet, Style[] headerBaseStyles)
        {
            if (this._headerRowCount > 0)
            {
                this.WriteHeaderCells(worksheet, headerBaseStyles);
                this.MergeHeaderCells(worksheet);
                this.WriteHeaderComments(worksheet);
            }
        }

        private void WriteHeaderCells(Worksheet worksheet, Style[] headerBaseStyles)
        {
            for (int columnIndex = 0; columnIndex < this._columns.Count; columnIndex++)
            {
                ExcelWriteColumn<T> column = this._columns[columnIndex];

                for (int rowIndex = 0; rowIndex < this._headerRowCount; rowIndex++)
                {
                    Cell cell = worksheet.Cells[this.StartRowIndex + rowIndex, this.StartColumnIndex + columnIndex];
                    string? value = column.HeaderNames.Length > 1 ? column.HeaderNames[rowIndex] : column.HeaderNames[0];
                    cell.Value = value;
                    this.ApplyHeaderCellStyle(cell, column, rowIndex, value, headerBaseStyles[columnIndex]);
                }
            }
        }

        /* Header merge rules:
         * single-level headers merge vertically; AutoMergeHeader merges equal,
         * adjacent values at higher levels within the parent range. The lowest
         * level is never merged.
         */
        private void MergeHeaderCells(Worksheet worksheet)
        {
            if (this._headerRowCount <= 1)
                return;

            for (int columnIndex = 0; columnIndex < this._columns.Count; columnIndex++)
            {
                ExcelWriteColumn<T> column = this._columns[columnIndex];
                if (column.HeaderNames.Length == 1)
                {
                    worksheet.Cells.Merge(this.StartRowIndex, this.StartColumnIndex + columnIndex, this._headerRowCount, 1);
                }
            }

            if (this.AutoMergeHeader)
            {
                int multiLevelStartColumn = -1;
                for (int columnIndex = 0; columnIndex < this._columns.Count; columnIndex++)
                {
                    if (this._columns[columnIndex].HeaderNames.Length > 1)
                    {
                        if (multiLevelStartColumn < 0)
                            multiLevelStartColumn = columnIndex;
                    }
                    else if (multiLevelStartColumn >= 0)
                    {
                        MergeLevel(0, multiLevelStartColumn, columnIndex - 1);
                        multiLevelStartColumn = -1;
                    }
                }

                if (multiLevelStartColumn >= 0)
                {
                    MergeLevel(0, multiLevelStartColumn, this._columns.Count - 1);
                }
            }

            /* Scans one header level within a range merged at the previous level. */
            void MergeLevel(int rowIndex, int startColumn, int endColumn)
            {
                if (rowIndex >= this._headerRowCount - 1 || startColumn >= endColumn)
                    return;

                int mergeStartColumn = startColumn;
                string? mergeValue = this._columns[mergeStartColumn].HeaderNames[rowIndex];

                for (int columnIndex = startColumn + 1; columnIndex <= endColumn; columnIndex++)
                {
                    string? value = this._columns[columnIndex].HeaderNames[rowIndex];
                    if (string.Equals(mergeValue, value, StringComparison.Ordinal))
                        continue;

                    MergeRun(rowIndex, mergeStartColumn, columnIndex - 1);
                    mergeStartColumn = columnIndex;
                    mergeValue = value;
                }

                MergeRun(rowIndex, mergeStartColumn, endColumn);
            }

            /* Merges one contiguous run of equal values and processes its child level. */
            void MergeRun(int rowIndex, int startColumn, int endColumn)
            {
                if (startColumn >= endColumn)
                    return;

                worksheet.Cells.Merge(this.StartRowIndex + rowIndex, this.StartColumnIndex + startColumn, 1, endColumn - startColumn + 1);

                MergeLevel(rowIndex + 1, startColumn, endColumn);
            }
        }

        private void WriteHeaderComments(Worksheet worksheet)
        {
            int rowIndex = this._headerRowCount - 1;
            for (int columnIndex = 0; columnIndex < this._columns.Count; columnIndex++)
            {
                ExcelWriteColumn<T> column = this._columns[columnIndex];
                if (column.HeaderCommentAction != null)
                {
                    Comment comment;
                    Cell cell = worksheet.Cells[this.StartRowIndex + rowIndex, this.StartColumnIndex + columnIndex];
                    if (cell.IsMerged)
                    {
                        var range = cell.GetMergedRange();
                        int commentIndex = worksheet.Comments.Add(range.FirstRow, range.FirstColumn);
                        comment = worksheet.Comments[commentIndex];
                    }
                    else
                    {
                        int commentIndex = worksheet.Comments.Add(cell.Row, cell.Column);
                        comment = worksheet.Comments[commentIndex];
                    }

                    column.HeaderCommentAction(comment);
                }
            }
        }

        private void ApplyHeaderCellStyle(Cell cell, ExcelWriteColumn<T> column, int rowIndex, string? value, Style baseStyle)
        {
            if (column.HeaderStyleAction == null)
            {
                cell.SetStyle(baseStyle);
                return;
            }

            Style style = this._cellsFactory.CreateStyle();
            style.Copy(baseStyle);
            column.HeaderStyleAction(style, rowIndex, value);
            cell.SetStyle(style);
        }
        #endregion

        #region WriteBody
        private void WriteBody(Worksheet worksheet, IEnumerable<T> items, Style[] bodyBaseStyles)
        {
            this.ApplyBodyValidation(worksheet);
            List<(int ColumnIndex, int StartIndex, int Length)> mergeRanges = this.WriteBodyCells(worksheet, items, bodyBaseStyles);
            this.MergeBodyCells(worksheet, mergeRanges);
        }

        private void ApplyBodyValidation(Worksheet worksheet)
        {
            if (this.ValidationRowCount <= 0)
                return;

            int endRow;
            if (this.ValidationRowCount == null || this.StartRowIndex + this._headerRowCount + this.ValidationRowCount.Value - 1 > worksheet.Workbook.Settings.MaxRow)
            {
                endRow = worksheet.Workbook.Settings.MaxRow;
            }
            else
            {
                endRow = this.StartRowIndex + this._headerRowCount + this.ValidationRowCount.Value - 1;
            }

            for (int columnIndex = 0; columnIndex < this._columns.Count; columnIndex++)
            {
                ExcelWriteColumn<T> column = this._columns[columnIndex];
                if (column.ValidationAction != null)
                {
                    ValidationCollection validations = worksheet.Validations;
                    int validationIndex = validations.Add(new CellArea()
                    {
                        StartRow = this.StartRowIndex + this._headerRowCount,
                        EndRow = endRow,
                        StartColumn = this.StartColumnIndex + columnIndex,
                        EndColumn = this.StartColumnIndex + columnIndex,
                    });
                    Validation validation = validations[validationIndex];
                    column.ValidationAction(validation);
                }
            }
        }

        private List<(int ColumnIndex, int StartIndex, int Length)> WriteBodyCells(Worksheet worksheet, IEnumerable<T> items, Style[] bodyBaseStyles)
        {
            bool[] hasMergeRun = new bool[this._columns.Count];
            int[] mergeStartIndexes = new int[this._columns.Count];
            object?[] mergeKeys = new object?[this._columns.Count];
            List<(int ColumnIndex, int StartIndex, int Length)> mergeRanges = new List<(int ColumnIndex, int StartIndex, int Length)>();

            int itemIndex = 0;
            foreach (T item in items)
            {
                for (int columnIndex = 0; columnIndex < this._columns.Count; columnIndex++)
                {
                    ExcelWriteColumn<T> column = this._columns[columnIndex];
                    Cell cell = worksheet.Cells[this.StartRowIndex + this._headerRowCount + itemIndex, this.StartColumnIndex + columnIndex];
                    object? value = column.ValueSelector(item, itemIndex);
                    cell.Value = value;
                    this.ApplyBodyCellStyle(cell, column, item, itemIndex, value, bodyBaseStyles[columnIndex]);

                    if (column.MergeKeySelector == null)
                        continue;

                    // MergeByValue reuses the value already selected for the cell.
                    object? mergeKey = ReferenceEquals(column.MergeKeySelector, column.ValueSelector) ? value : column.MergeKeySelector(item, itemIndex);
                    if (!hasMergeRun[columnIndex])
                    {
                        hasMergeRun[columnIndex] = true;
                        mergeStartIndexes[columnIndex] = itemIndex;
                        mergeKeys[columnIndex] = mergeKey;
                    }
                    else if (!object.Equals(mergeKeys[columnIndex], mergeKey))
                    {
                        AddMergeRange(columnIndex, mergeStartIndexes[columnIndex], itemIndex);
                        mergeStartIndexes[columnIndex] = itemIndex;
                        mergeKeys[columnIndex] = mergeKey;
                    }
                }
                itemIndex++;
            }

            for (int columnIndex = 0; columnIndex < this._columns.Count; columnIndex++)
            {
                if (hasMergeRun[columnIndex])
                {
                    AddMergeRange(columnIndex, mergeStartIndexes[columnIndex], itemIndex);
                }
            }

            return mergeRanges;

            void AddMergeRange(int columnIndex, int startIndex, int endIndex)
            {
                int length = endIndex - startIndex;
                if (length > 1)
                {
                    mergeRanges.Add((columnIndex, startIndex, length));
                }
            }
        }

        private void MergeBodyCells(Worksheet worksheet, List<(int ColumnIndex, int StartIndex, int Length)> mergeRanges)
        {
            int dataStartRowIndex = this.StartRowIndex + this._headerRowCount;
            foreach ((int columnIndex, int startIndex, int length) in mergeRanges)
            {
                worksheet.Cells.Merge(dataStartRowIndex + startIndex, this.StartColumnIndex + columnIndex, length, 1);
            }
        }

        private void ApplyBodyCellStyle(Cell cell, ExcelWriteColumn<T> column, T item, int objIndex, object? value, Style baseStyle)
        {
            if (column.BodyStyleAction == null)
            {
                cell.SetStyle(baseStyle);
                return;
            }

            Style style = this._cellsFactory.CreateStyle();
            style.Copy(baseStyle);
            column.BodyStyleAction(style, item, objIndex, value);
            cell.SetStyle(style);
        }
        #endregion

        #region ApplyAutoFilter
        private void ApplyAutoFilter(Worksheet worksheet)
        {
            if (this.AutoFilter && this._headerRowCount > 0 && this._columns.Count > 0)
            {
                int startRow = this.StartRowIndex + this._headerRowCount - 1;
                int startColumn = this.StartColumnIndex;
                int endColumn = startColumn + this._columns.Count - 1;
                worksheet.AutoFilter.SetRange(startRow, startColumn, endColumn);
            }
        }
        #endregion

        #region ApplyAutoFitColumns
        private void ApplyAutoFitColumns(Worksheet worksheet)
        {
            if (this.AutoFitColumns && this._columns.Count > 0)
            {
                worksheet.AutoFitColumns(this.StartColumnIndex, this.StartColumnIndex + this._columns.Count - 1);
            }
        }
        #endregion

        #region ApplyColumnWidths
        private void ApplyColumnWidths(Worksheet worksheet)
        {
            for (int i = 0; i < this._columns.Count; i++)
            {
                ExcelWriteColumn<T> column = this._columns[i];
                if (column.Width != null)
                {
                    switch (column.WidthUnit)
                    {
                        case WidthUnit.Character:
                            worksheet.Cells.SetColumnWidth(this.StartColumnIndex + i, column.Width.Value);
                            break;
                        case WidthUnit.Pixel:
                            worksheet.Cells.SetColumnWidthPixel(this.StartColumnIndex + i, (int)column.Width.Value);
                            break;
                        case WidthUnit.Inch:
                            worksheet.Cells.SetColumnWidthInch(this.StartColumnIndex + i, column.Width.Value);
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                }
            }
        }
        #endregion

        #region ApplyHiddenColumns
        private void ApplyHiddenColumns(Worksheet worksheet)
        {
            for (int i = 0; i < this._columns.Count; i++)
            {
                if (this._columns[i].IsHidden)
                {
                    worksheet.Cells.Columns[this.StartColumnIndex + i].IsHidden = true;
                }
            }
        }
        #endregion

        #region ApplyFreezePanes
        private void ApplyFreezePanes(Worksheet worksheet)
        {
            int freezeRowCount = this.FreezeHeaderRows && this._headerRowCount > 0 ? this.StartRowIndex + this._headerRowCount : 0;
            int freezeColumnCount = this.FreezeColumnCount > 0 ? this.StartColumnIndex + this.FreezeColumnCount : 0;

            if (freezeRowCount > 0 || freezeColumnCount > 0)
            {
                worksheet.FreezePanes(freezeRowCount, freezeColumnCount, freezeRowCount, freezeColumnCount);
            }
        }
        #endregion

        #region Event
        /// <summary>
        /// Occurs before the worksheet is written.
        /// </summary>
        public event EventHandler<WorksheetWritingEventArgs>? WorksheetWriting;

        private void OnWorksheetWriting(WorksheetWritingEventArgs e)
        {
            this.WorksheetWriting?.Invoke(this, e);
        }

        /// <summary>
        /// Occurs after the worksheet is written.
        /// </summary>
        public event EventHandler<WorksheetWrittenEventArgs>? WorksheetWritten;

        private void OnWorksheetWritten(WorksheetWrittenEventArgs e)
        {
            this.WorksheetWritten?.Invoke(this, e);
        }
        #endregion
    }

    #region EventArgs
    /// <summary>
    /// Provides data for the <see cref="ExcelWriter{T}.WorksheetWriting"/> event.
    /// </summary>
    public sealed class WorksheetWritingEventArgs : EventArgs
    {
        /// <summary>Gets the worksheet that is about to be written.</summary>
        public Worksheet Worksheet { get; }

        /// <summary>Initializes event data for a worksheet-writing notification.</summary>
        /// <param name="worksheet">The worksheet that will be written.</param>
        public WorksheetWritingEventArgs(Worksheet worksheet)
        {
            this.Worksheet = worksheet ?? throw new ArgumentNullException(nameof(worksheet));
        }
    }

    /// <summary>
    /// Provides data for the <see cref="ExcelWriter{T}.WorksheetWritten"/> event.
    /// </summary>
    public sealed class WorksheetWrittenEventArgs : EventArgs
    {
        /// <summary>Gets the worksheet that has been written.</summary>
        public Worksheet Worksheet { get; }

        /// <summary>Initializes event data for a worksheet-written notification.</summary>
        /// <param name="worksheet">The worksheet that was written.</param>
        public WorksheetWrittenEventArgs(Worksheet worksheet)
        {
            this.Worksheet = worksheet ?? throw new ArgumentNullException(nameof(worksheet));
        }
    }
    #endregion
}
