using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace BlueBird.Http.Logging
{
    /// <summary>
    /// Represents the captured request portion of an HTTP log entry.
    /// </summary>
    /// <remarks>
    /// Instances created by <see cref="HttpClientLoggingHandler"/> contain only values
    /// prepared for logging and do not expose the original HTTP request.
    /// </remarks>
    public sealed class HttpRequestLog
    {
        /// <summary>
        /// Gets the time at which the request was sent.
        /// </summary>
        public required DateTimeOffset RequestTime { get; init; }

        /// <summary>
        /// Gets the HTTP method.
        /// </summary>
        public required string Method { get; init; }

        /// <summary>
        /// Gets the request URI after applying the configured URI redactor, or the original URI
        /// when no redactor was configured. The value is <c>null</c> when no URI was supplied.
        /// </summary>
        public required string? Uri { get; init; }

        /// <summary>
        /// Gets the HTTP protocol version.
        /// </summary>
        public required string Version { get; init; }

        /// <summary>
        /// Gets the fields captured for the request.
        /// </summary>
        public required HttpLogFields Fields { get; init; }

        /// <summary>
        /// Gets the captured request headers after redaction, if configured.
        /// </summary>
        public IReadOnlyDictionary<string, string> Headers { get; init; } = ReadOnlyDictionary<string, string>.Empty;

        /// <summary>
        /// Gets the captured content headers after request-header redaction, if configured.
        /// </summary>
        public IReadOnlyDictionary<string, string> ContentHeaders { get; init; } = ReadOnlyDictionary<string, string>.Empty;

        /// <summary>
        /// Gets the captured request content, or <c>null</c> if unavailable or not captured.
        /// Empty text is represented by an empty string; binary content is represented by a
        /// description. Text is redacted and truncated as configured.
        /// </summary>
        public string? Content { get; init; }
    }
}
