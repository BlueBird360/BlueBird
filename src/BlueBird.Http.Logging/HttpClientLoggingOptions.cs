using System;
using System.Net.Http;
using Microsoft.Extensions.Logging;

namespace BlueBird.Http.Logging
{
    /// <summary>
    /// Provides options that control filtering, log levels, field capture, content handling,
    /// redaction, and formatting for <see cref="HttpClientLoggingHandler"/>.
    /// </summary>
    /// <remarks>
    /// Configure options before use. Delegates and the formatter may be called concurrently
    /// and must be thread-safe. Exceptions from filters, selectors,
    /// <see cref="ShouldTreatContentAsText"/>, and buffering are propagated; failures during
    /// log capture, formatting, or writing are reported and not propagated.
    /// </remarks>
    public sealed class HttpClientLoggingOptions
    {
        /// <summary>
        /// Gets or sets whether HTTP logging is enabled.
        /// When <c>false</c>, the handler passes requests through without producing its own log output.
        /// Defaults to <c>true</c>.
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets a predicate evaluated before sending the request to determine whether
        /// the HTTP operation should be logged. When <c>null</c>, all requests are eligible.
        /// </summary>
        public Func<HttpRequestMessage, bool>? RequestFilter { get; set; }

        /// <summary>
        /// Gets or sets a predicate evaluated after receiving a response to determine whether the
        /// HTTP exchange should be logged. It is not invoked when the operation throws.
        /// When <c>null</c>, all responses are eligible.
        /// </summary>
        public Func<HttpRequestMessage, HttpResponseMessage, bool>? ResponseFilter { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="LogLevel"/> selector for responses.
        /// Defaults to <see cref="LogLevel.Information"/>.
        /// </summary>
        public Func<HttpRequestMessage, HttpResponseMessage, LogLevel> ResponseLevelSelector
        {
            get;
            set => field = value ?? throw new ArgumentNullException(nameof(value));
        } = static (_, _) => LogLevel.Information;

        /// <summary>
        /// Gets or sets the <see cref="LogLevel"/> selector for HTTP operations that throw exceptions.
        /// HTTP error responses use <see cref="ResponseLevelSelector"/>.
        /// Defaults to <see cref="LogLevel.Error"/>.
        /// </summary>
        public Func<HttpRequestMessage, Exception, LogLevel> ExceptionLevelSelector
        {
            get;
            set => field = value ?? throw new ArgumentNullException(nameof(value));
        } = static (_, _) => LogLevel.Error;

        /// <summary>
        /// Gets or sets the selector for optional request fields.
        /// It runs before sending; core metadata is always captured.
        /// Defaults to <see cref="HttpLogFields.None"/>.
        /// </summary>
        public Func<HttpRequestMessage, HttpLogFields> RequestFieldsSelector
        {
            get;
            set => field = value ?? throw new ArgumentNullException(nameof(value));
        } = static _ => HttpLogFields.None;

        /// <summary>
        /// Gets or sets the selector for optional response fields.
        /// It runs after receiving the response and may inspect both messages.
        /// Core metadata is always captured. Defaults to <see cref="HttpLogFields.None"/>.
        /// </summary>
        public Func<HttpRequestMessage, HttpResponseMessage, HttpLogFields> ResponseFieldsSelector
        {
            get;
            set => field = value ?? throw new ArgumentNullException(nameof(value));
        } = static (_, _) => HttpLogFields.None;

        /// <summary>
        /// Gets or sets a function that transforms the request URI before it is written to the log.
        /// By default, user information, the query string, and the fragment are omitted.
        /// Set to <c>null</c> to log the original URI, which may expose sensitive information.
        /// </summary>
        public Func<Uri, string>? RequestUriRedactor { get; set; } = HttpLogDefaults.RedactRequestUri;

        /// <summary>
        /// Gets or sets a function that redacts request and content-header values.
        /// It receives the header name and combined value; multiple values are joined by <c>", "</c>.
        /// The default redacts common sensitive headers; <c>null</c> disables redaction and may expose sensitive data.
        /// </summary>
        public Func<string, string, string>? RequestHeaderRedactor { get; set; } = HttpLogDefaults.RedactSensitiveHeader;

        /// <summary>
        /// Gets or sets a function that redacts response and content-header values.
        /// It receives the header name and combined value; multiple values are joined by <c>", "</c>.
        /// The default redacts common sensitive headers; <c>null</c> disables redaction and may expose sensitive data.
        /// </summary>
        public Func<string, string, string>? ResponseHeaderRedactor { get; set; } = HttpLogDefaults.RedactSensitiveHeader;

        /// <summary>
        /// Gets or sets a function that determines whether selected content is logged as text.
        /// When it returns <c>false</c>, the body is not read and a binary description is logged.
        /// Unknown media types are binary by default.
        /// </summary>
        public Func<HttpContent, bool> ShouldTreatContentAsText
        {
            get;
            set => field = value ?? throw new ArgumentNullException(nameof(value));
        } = HttpLogDefaults.IsTextContent;

        /// <summary>
        /// Gets or sets a function applied to textual request content before truncation.
        /// When <c>null</c>, the text is left unchanged.
        /// </summary>
        public Func<string, string>? RequestContentRedactor { get; set; }

        /// <summary>
        /// Gets or sets a function applied to textual response content before truncation.
        /// When <c>null</c>, the text is left unchanged.
        /// </summary>
        public Func<string, string>? ResponseContentRedactor { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of UTF-16 code units of textual request content to log.
        /// <c>null</c> disables truncation; <c>0</c> logs only a truncation notice for non-empty text.
        /// Defaults to <c>4096</c>.
        /// </summary>
        /// <remarks>
        /// Truncation does not split a UTF-16 surrogate pair.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The assigned value is negative.
        /// </exception>
        public int? RequestContentMaxChars
        {
            get;
            set
            {
                if (value.HasValue)
                {
                    ArgumentOutOfRangeException.ThrowIfNegative(value.Value, nameof(value));
                }

                field = value;
            }
        } = 4096;

        /// <summary>
        /// Gets or sets the maximum number of UTF-16 code units of textual response content to log.
        /// <c>null</c> disables truncation; <c>0</c> logs only a truncation notice for non-empty text.
        /// Defaults to <c>4096</c>.
        /// </summary>
        /// <remarks>
        /// Truncation does not split a UTF-16 surrogate pair.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The assigned value is negative.
        /// </exception>
        public int? ResponseContentMaxChars
        {
            get;
            set
            {
                if (value.HasValue)
                {
                    ArgumentOutOfRangeException.ThrowIfNegative(value.Value, nameof(value));
                }

                field = value;
            }
        } = 4096;

        /// <summary>
        /// Gets or sets the maximum number of bytes permitted when buffering textual request content
        /// selected for logging. Content without a declared <c>Content-Length</c>, or with a declared
        /// length above this value, is not buffered and is omitted from the log.
        /// Defaults to 1 MiB (1048576 bytes).
        /// </summary>
        /// <remarks>
        /// This buffering limit is independent of the output truncation controlled by
        /// <see cref="RequestContentMaxChars"/>. The limit is also enforced while content is buffered.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The assigned value is negative.
        /// </exception>
        public long RequestContentBufferMaxBytes
        {
            get;
            set
            {
                ArgumentOutOfRangeException.ThrowIfNegative(value, nameof(value));
                field = value;
            }
        } = 1024 * 1024;

        /// <summary>
        /// Gets or sets the maximum number of bytes permitted when buffering textual response content
        /// selected for logging. Content without a declared <c>Content-Length</c>, or with a declared
        /// length above this value, is not buffered and is omitted from the log.
        /// Defaults to 1 MiB (1048576 bytes).
        /// </summary>
        /// <remarks>
        /// This buffering limit is independent of the output truncation controlled by
        /// <see cref="ResponseContentMaxChars"/>. The limit is also enforced while content is buffered.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The assigned value is negative.
        /// </exception>
        public long ResponseContentBufferMaxBytes
        {
            get;
            set
            {
                ArgumentOutOfRangeException.ThrowIfNegative(value, nameof(value));
                field = value;
            }
        } = 1024 * 1024;

        /// <summary>
        /// Gets or sets the formatter for log entry text.
        /// Structured properties are unaffected.
        /// Defaults to <see cref="DefaultHttpLogFormatter"/>.
        /// </summary>
        public IHttpLogFormatter Formatter
        {
            get;
            set => field = value ?? throw new ArgumentNullException(nameof(value));
        } = new DefaultHttpLogFormatter();
    }
}
