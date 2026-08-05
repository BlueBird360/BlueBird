using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace BlueBird.Http.Logging
{
    /// <summary>
    /// A <see cref="DelegatingHandler"/> that logs HTTP request and response data (or exceptions on failure) via <see cref="ILogger"/>.
    /// </summary>
    public sealed class HttpClientLoggingHandler : DelegatingHandler
    {
        private static readonly EventId RequestCompletedEventId = new EventId(1, "HttpRequestCompleted");
        private static readonly EventId RequestFailedEventId = new EventId(2, "HttpRequestFailed");
        private static readonly EventId LoggingFailedEventId = new EventId(3, "HttpLoggingFailed");

        private readonly ILogger _logger;
        private readonly HttpClientLoggingOptions _options;

        /// <summary>
        /// Initializes a new instance of <see cref="HttpClientLoggingHandler"/>.
        /// </summary>
        /// <param name="logger">The logger to write HTTP call entries to.</param>
        /// <param name="options">Configuration options controlling logging behavior.</param>
        public HttpClientLoggingHandler(ILogger logger, HttpClientLoggingOptions options)
        {
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this._options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <inheritdoc/>
        protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return this.SendCoreAsync(request, useAsync: false, cancellationToken).GetAwaiter().GetResult();
        }

        /// <inheritdoc/>
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return this.SendCoreAsync(request, useAsync: true, cancellationToken);
        }

        private async Task<HttpResponseMessage> SendCoreAsync(HttpRequestMessage request, bool useAsync, CancellationToken cancellationToken)
        {
            if (!this._options.IsEnabled)
            {
                return await this.SendInnerAsync(request, useAsync, cancellationToken).ConfigureAwait(false);
            }

            if (this._options.RequestFilter != null && !this._options.RequestFilter(request))
            {
                return await this.SendInnerAsync(request, useAsync, cancellationToken).ConfigureAwait(false);
            }

            HttpLogFields requestFields = this._options.RequestFieldsSelector(request);
            bool shouldTreatRequestContentAsText = requestFields.HasFlag(HttpLogFields.Content) &&
                request.Content != null &&
                this._options.ShouldTreatContentAsText(request.Content);

            if (shouldTreatRequestContentAsText)
            {
                cancellationToken.ThrowIfCancellationRequested();

                bool contentBuffered = await BufferContentIfWithinLimitAsync(
                    request.Content!,
                    this._options.RequestContentBufferMaxBytes).ConfigureAwait(false);

                if (!contentBuffered)
                {
                    requestFields &= ~HttpLogFields.Content;
                }
            }

            DateTimeOffset requestTime = DateTimeOffset.UtcNow;
            long startTimestamp = Stopwatch.GetTimestamp();

            HttpResponseMessage response;
            try
            {
                response = await this.SendInnerAsync(request, useAsync, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                TimeSpan failureDuration = Stopwatch.GetElapsedTime(startTimestamp);
                LogLevel logLevel = this._options.ExceptionLevelSelector(request, ex);
                if (this._logger.IsEnabled(logLevel))
                {
                    await this.SafeLogAsync(() => this.LogExceptionAsync(
                        logLevel,
                        request,
                        requestTime,
                        requestFields,
                        shouldTreatRequestContentAsText,
                        ex,
                        failureDuration)).ConfigureAwait(false);
                }

                throw;
            }

            DateTimeOffset responseTime = DateTimeOffset.UtcNow;
            TimeSpan duration = Stopwatch.GetElapsedTime(startTimestamp);

            if (this._options.ResponseFilter != null && !this._options.ResponseFilter(request, response))
            {
                return response;
            }

            LogLevel responseLevel = this._options.ResponseLevelSelector(request, response);
            if (!this._logger.IsEnabled(responseLevel))
            {
                return response;
            }

            HttpLogFields responseFields = this._options.ResponseFieldsSelector(request, response);
            bool shouldTreatResponseContentAsText = responseFields.HasFlag(HttpLogFields.Content) &&
                this._options.ShouldTreatContentAsText(response.Content);

            if (shouldTreatResponseContentAsText)
            {
                bool contentBuffered = await BufferContentIfWithinLimitAsync(
                    response.Content,
                    this._options.ResponseContentBufferMaxBytes).ConfigureAwait(false);

                if (!contentBuffered)
                {
                    responseFields &= ~HttpLogFields.Content;
                }
            }

            await this.SafeLogAsync(() => this.LogResponseAsync(
                responseLevel,
                request,
                requestTime,
                requestFields,
                shouldTreatRequestContentAsText,
                response,
                responseTime,
                responseFields,
                shouldTreatResponseContentAsText,
                duration)).ConfigureAwait(false);

            return response;
        }

        private Task<HttpResponseMessage> SendInnerAsync(HttpRequestMessage request, bool useAsync, CancellationToken cancellationToken)
        {
            if (useAsync)
            {
                return base.SendAsync(request, cancellationToken);
            }

            return Task.FromResult(base.Send(request, cancellationToken));
        }

        private async Task LogResponseAsync(
            LogLevel logLevel,
            HttpRequestMessage request,
            DateTimeOffset requestTime,
            HttpLogFields requestFields,
            bool shouldTreatRequestContentAsText,
            HttpResponseMessage response,
            DateTimeOffset responseTime,
            HttpLogFields responseFields,
            bool shouldTreatResponseContentAsText,
            TimeSpan duration)
        {
            var logEntry = new HttpLogEntry
            {
                Request = await this.CaptureRequestAsync(request, requestTime, requestFields, shouldTreatRequestContentAsText).ConfigureAwait(false),
                Response = await this.CaptureResponseAsync(response, responseTime, responseFields, shouldTreatResponseContentAsText).ConfigureAwait(false),
                Duration = duration,
            };

            this.WriteLog(logLevel, RequestCompletedEventId, logEntry);
        }

        private async Task LogExceptionAsync(
            LogLevel logLevel,
            HttpRequestMessage request,
            DateTimeOffset requestTime,
            HttpLogFields requestFields,
            bool shouldTreatRequestContentAsText,
            Exception exception,
            TimeSpan duration)
        {
            var logEntry = new HttpLogEntry
            {
                Request = await this.CaptureRequestAsync(request, requestTime, requestFields, shouldTreatRequestContentAsText).ConfigureAwait(false),
                Exception = exception,
                Duration = duration,
            };

            this.WriteLog(logLevel, RequestFailedEventId, logEntry);
        }

        private void WriteLog(LogLevel logLevel, EventId eventId, HttpLogEntry logEntry)
        {
            string message = this._options.Formatter.Format(logEntry);
            var state = new KeyValuePair<string, object?>[]
            {
                new KeyValuePair<string, object?>("HttpMethod", logEntry.Request.Method),
                new KeyValuePair<string, object?>("Uri", logEntry.Request.Uri),
                new KeyValuePair<string, object?>("StatusCode", logEntry.Response?.StatusCode),
                new KeyValuePair<string, object?>("DurationMs", logEntry.Duration.TotalMilliseconds),
                new KeyValuePair<string, object?>("HttpLog", message),
                new KeyValuePair<string, object?>("{OriginalFormat}", "{HttpLog}"),
            };

            this._logger.Log(
                logLevel,
                eventId,
                state,
                logEntry.Exception,
                static (values, _) => (string)values[4].Value!);
        }

        private async Task<HttpRequestLog> CaptureRequestAsync(
            HttpRequestMessage request,
            DateTimeOffset requestTime,
            HttpLogFields fields,
            bool shouldTreatContentAsText)
        {
            return new HttpRequestLog
            {
                RequestTime = requestTime,
                Method = request.Method.Method,
                Uri = FormatRequestUri(request.RequestUri, this._options.RequestUriRedactor),
                Version = request.Version.ToString(),
                Fields = fields,
                Headers = CaptureHeaders(request.Headers, fields.HasFlag(HttpLogFields.Headers), this._options.RequestHeaderRedactor),
                ContentHeaders = CaptureHeaders(request.Content?.Headers, fields.HasFlag(HttpLogFields.ContentHeaders), this._options.RequestHeaderRedactor),
                Content = fields.HasFlag(HttpLogFields.Content)
                    ? await CaptureContentAsync(
                        request.Content,
                        shouldTreatContentAsText,
                        this._options.RequestContentMaxChars,
                        this._options.RequestContentRedactor).ConfigureAwait(false)
                    : null,
            };
        }

        private async Task<HttpResponseLog> CaptureResponseAsync(
            HttpResponseMessage response,
            DateTimeOffset responseTime,
            HttpLogFields fields,
            bool shouldTreatContentAsText)
        {
            return new HttpResponseLog
            {
                ResponseTime = responseTime,
                StatusCode = (int)response.StatusCode,
                ReasonPhrase = response.ReasonPhrase,
                Version = response.Version.ToString(),
                Fields = fields,
                Headers = CaptureHeaders(response.Headers, fields.HasFlag(HttpLogFields.Headers), this._options.ResponseHeaderRedactor),
                ContentHeaders = CaptureHeaders(response.Content.Headers, fields.HasFlag(HttpLogFields.ContentHeaders), this._options.ResponseHeaderRedactor),
                Content = fields.HasFlag(HttpLogFields.Content)
                    ? await CaptureContentAsync(
                        response.Content,
                        shouldTreatContentAsText,
                        this._options.ResponseContentMaxChars,
                        this._options.ResponseContentRedactor).ConfigureAwait(false)
                    : null,
            };
        }

        private static string? FormatRequestUri(Uri? requestUri, Func<Uri, string>? redactor)
        {
            if (requestUri == null)
            {
                return null;
            }

            if (redactor != null)
            {
                return redactor(requestUri);
            }

            return requestUri.IsAbsoluteUri ? requestUri.AbsoluteUri : requestUri.OriginalString;
        }

        private static IReadOnlyDictionary<string, string> CaptureHeaders(
            HttpHeaders? headers,
            bool shouldCapture,
            Func<string, string, string>? redactor)
        {
            if (!shouldCapture || headers == null || headers.NonValidated.Count == 0)
            {
                return ReadOnlyDictionary<string, string>.Empty;
            }

            var captured = new Dictionary<string, string>(headers.NonValidated.Count, StringComparer.OrdinalIgnoreCase);
            foreach (var header in headers)
            {
                string value = string.Join(", ", header.Value);
                captured[header.Key] = redactor != null ? redactor(header.Key, value) : value;
            }

            return new ReadOnlyDictionary<string, string>(captured);
        }

        private static async Task<string?> CaptureContentAsync(
            HttpContent? content,
            bool shouldTreatContentAsText,
            int? maxChars,
            Func<string, string>? redactor)
        {
            if (content == null)
            {
                return null;
            }

            string mediaType = content.Headers.ContentType?.MediaType ?? "unknown";
            if (!shouldTreatContentAsText)
            {
                long? length = content.Headers.ContentLength;
                return length.HasValue
                    ? $"[Binary content: {mediaType}, {length.Value} bytes]"
                    : $"[Binary content: {mediaType}, size unknown]";
            }

            string contentText = await content.ReadAsStringAsync().ConfigureAwait(false);
            if (redactor != null)
            {
                contentText = redactor(contentText);
            }

            return TruncateContent(contentText, maxChars);
        }

        private static string TruncateContent(string content, int? maxChars)
        {
            if (!maxChars.HasValue || content.Length <= maxChars.Value)
            {
                return content;
            }

            int charsToTake = maxChars.Value;
            if (charsToTake > 0 &&
                charsToTake < content.Length &&
                char.IsSurrogatePair(content, charsToTake - 1))
            {
                charsToTake--;
            }

            string truncationMessage = $"[Content truncated: total {content.Length} length, showing first {charsToTake}]";
            if (charsToTake == 0)
            {
                return truncationMessage;
            }

            return content.Substring(0, charsToTake) + Environment.NewLine + truncationMessage;
        }

        private static async Task<bool> BufferContentIfWithinLimitAsync(HttpContent content, long maxBufferBytes)
        {
            long? length = content.Headers.ContentLength;
            if (!length.HasValue || length.Value > maxBufferBytes)
            {
                return false;
            }

            await content.LoadIntoBufferAsync(maxBufferBytes).ConfigureAwait(false);
            return true;
        }

        private async Task SafeLogAsync(Func<Task> logAction)
        {
            try
            {
                await logAction().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this.ReportLoggingFailure(ex);
            }
        }

        private void ReportLoggingFailure(Exception exception)
        {
            try
            {
                if (this._logger.IsEnabled(LogLevel.Warning))
                {
                    this._logger.LogWarning(LoggingFailedEventId, exception, "HttpClient logging failed.");
                }
            }
            catch (Exception)
            {
                // Ignore failures while reporting a logging failure.
            }
        }
    }
}
