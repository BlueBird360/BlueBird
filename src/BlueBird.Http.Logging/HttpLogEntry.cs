using System;

namespace BlueBird.Http.Logging
{
    /// <summary>
    /// Represents a log entry for an HTTP exchange, including the request, optional response, and optional exception.
    /// </summary>
    /// <remarks>
    /// Entries created by <see cref="HttpClientLoggingHandler"/> contain either a response or an exception, but never both.
    /// </remarks>
    public sealed class HttpLogEntry
    {
        /// <summary>
        /// Gets the captured request log. Always present.
        /// </summary>
        public required HttpRequestLog Request { get; init; }

        /// <summary>
        /// Gets the captured response, or <c>null</c> if the HTTP operation threw.
        /// </summary>
        public HttpResponseLog? Response { get; init; }

        /// <summary>
        /// Gets the exception, or <c>null</c> if the HTTP operation returned a response.
        /// </summary>
        public Exception? Exception { get; init; }

        /// <summary>
        /// Gets the time spent in the inner handler pipeline, excluding content buffering and logging performed by this handler.
        /// </summary>
        public required TimeSpan Duration { get; init; }
    }
}
