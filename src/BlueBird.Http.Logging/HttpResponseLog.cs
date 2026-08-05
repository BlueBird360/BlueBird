using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace BlueBird.Http.Logging
{
    /// <summary>
    /// Represents the captured response portion of an HTTP log entry.
    /// </summary>
    /// <remarks>
    /// Instances created by <see cref="HttpClientLoggingHandler"/> contain only values
    /// prepared for logging and do not expose the original HTTP response.
    /// </remarks>
    public sealed class HttpResponseLog
    {
        /// <summary>
        /// Gets the time at which the response was received.
        /// </summary>
        public required DateTimeOffset ResponseTime { get; init; }

        /// <summary>
        /// Gets the numeric HTTP status code.
        /// </summary>
        public required int StatusCode { get; init; }

        /// <summary>
        /// Gets the HTTP reason phrase, or <c>null</c> when none was supplied.
        /// </summary>
        public required string? ReasonPhrase { get; init; }

        /// <summary>
        /// Gets the HTTP protocol version.
        /// </summary>
        public required string Version { get; init; }

        /// <summary>
        /// Gets the fields captured for the response.
        /// </summary>
        public required HttpLogFields Fields { get; init; }

        /// <summary>
        /// Gets the captured response headers after redaction, if configured.
        /// </summary>
        public IReadOnlyDictionary<string, string> Headers { get; init; } = ReadOnlyDictionary<string, string>.Empty;

        /// <summary>
        /// Gets the captured content headers after response-header redaction, if configured.
        /// </summary>
        public IReadOnlyDictionary<string, string> ContentHeaders { get; init; } = ReadOnlyDictionary<string, string>.Empty;

        /// <summary>
        /// Gets the captured response content, or <c>null</c> if not captured. Empty text is
        /// represented by an empty string; binary content is represented by a description.
        /// Text is redacted and truncated as configured.
        /// </summary>
        public string? Content { get; init; }
    }
}
