using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace BlueBird.Http.Logging
{
    /// <summary>
    /// Default implementation of <see cref="IHttpLogFormatter"/> that produces a human-readable text representation of HTTP calls.
    /// </summary>
    public sealed class DefaultHttpLogFormatter : IHttpLogFormatter
    {
        private const string Separator = "----------------------------------------";
        private const string Indent = "    ";

        /// <inheritdoc/>
        public string Format(HttpLogEntry entry)
        {
            ArgumentNullException.ThrowIfNull(entry);

            var builder = new StringBuilder();
            AppendRequest(builder, entry.Request);

            if (entry.Response != null)
            {
                builder.AppendLine(Separator);
                AppendResponse(builder, entry.Response, entry.Duration);
            }

            if (entry.Exception != null)
            {
                builder.AppendLine(Separator);
                builder.AppendLine(CultureInfo.InvariantCulture, $"Duration: {entry.Duration.TotalMilliseconds:F2} ms");
            }

            return builder.ToString();
        }

        private static void AppendRequest(StringBuilder builder, HttpRequestLog requestLog)
        {
            string uri = string.IsNullOrWhiteSpace(requestLog.Uri) ? "[No URI]" : requestLog.Uri;
            builder.AppendLine($"Request: {requestLog.Method} {uri} HTTP/{requestLog.Version}");
            builder.AppendLine($"RequestTime: {requestLog.RequestTime:O}");

            AppendHeaderSection(builder, "Request Headers:", requestLog.Headers, requestLog.Fields.HasFlag(HttpLogFields.Headers));
            AppendHeaderSection(builder, "Content Headers:", requestLog.ContentHeaders, requestLog.Fields.HasFlag(HttpLogFields.ContentHeaders));
            AppendContentSection(builder, requestLog.Content, requestLog.Fields.HasFlag(HttpLogFields.Content));
        }

        private static void AppendResponse(StringBuilder builder, HttpResponseLog responseLog, TimeSpan duration)
        {
            builder.Append($"Response: {responseLog.StatusCode}");
            if (!string.IsNullOrWhiteSpace(responseLog.ReasonPhrase))
            {
                builder.Append($" {responseLog.ReasonPhrase}");
            }

            builder.AppendLine($" HTTP/{responseLog.Version}");
            builder.AppendLine($"ResponseTime: {responseLog.ResponseTime:O}");
            builder.AppendLine(CultureInfo.InvariantCulture, $"Duration: {duration.TotalMilliseconds:F2} ms");

            AppendHeaderSection(builder, "Response Headers:", responseLog.Headers, responseLog.Fields.HasFlag(HttpLogFields.Headers));
            AppendHeaderSection(builder, "Content Headers:", responseLog.ContentHeaders, responseLog.Fields.HasFlag(HttpLogFields.ContentHeaders));
            AppendContentSection(builder, responseLog.Content, responseLog.Fields.HasFlag(HttpLogFields.Content));
        }

        private static void AppendHeaderSection(
            StringBuilder builder,
            string label,
            IReadOnlyDictionary<string, string> headers,
            bool shouldInclude)
        {
            if (!shouldInclude)
            {
                return;
            }

            builder.AppendLine(label);
            if (headers.Count == 0)
            {
                builder.AppendLine(Indent + "[None]");
                return;
            }

            foreach (var header in headers)
            {
                builder.Append($"{Indent}{header.Key}: ");
                builder.AppendLine(header.Value);
            }
        }

        private static void AppendContentSection(StringBuilder builder, string? content, bool shouldInclude)
        {
            if (!shouldInclude)
            {
                return;
            }

            builder.AppendLine("Content:");
            if (content == null)
            {
                builder.AppendLine(Indent + "[No content]");
                return;
            }

            if (content.Length == 0)
            {
                builder.AppendLine(Indent + "[Empty]");
                return;
            }

            AppendIndentedLines(builder, content);
        }

        private static void AppendIndentedLines(StringBuilder builder, string content)
        {
            using var reader = new StringReader(content);
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                builder.Append(Indent);
                builder.AppendLine(line);
            }
        }
    }
}
