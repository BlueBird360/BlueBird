using Aspose.Cells;

namespace BlueBird.Aspose.Cells
{
    /// <summary>
    /// Defines the default header and body styles of an Excel writer.
    /// </summary>
    public sealed class ExcelWriterTheme
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExcelWriterTheme"/> class.
        /// </summary>
        public ExcelWriterTheme()
        {
            CellsFactory factory = new CellsFactory();
            this.HeaderStyle = factory.CreateStyle();
            this.BodyStyle = factory.CreateStyle();
        }

        /// <summary>
        /// Gets the default header style.
        /// </summary>
        public Style HeaderStyle { get; }

        /// <summary>
        /// Gets the default body style.
        /// </summary>
        public Style BodyStyle { get; }
    }
}
