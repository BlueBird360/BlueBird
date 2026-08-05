namespace BlueBird.Http.Logging
{
    /// <summary>
    /// Defines a strategy for formatting HTTP call log entries into a string.
    /// </summary>
    /// <remarks>
    /// Implementations control the textual layout; the handler writes the returned string
    /// to the logger. Implementations must be safe for concurrent calls.
    /// </remarks>
    public interface IHttpLogFormatter
    {
        /// <summary>
        /// Formats an <see cref="HttpLogEntry"/> into a string.
        /// </summary>
        /// <param name="entry">The HTTP log entry to format.</param>
        /// <returns>The formatted log string.</returns>
        string Format(HttpLogEntry entry);
    }
}
