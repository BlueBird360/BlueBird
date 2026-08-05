# BlueBird.Http.Logging

Lightweight, configurable request and response logging for .NET `HttpClient` pipelines. It integrates with `IHttpClientFactory` and `Microsoft.Extensions.Logging` without changing application call sites.

The package targets `net8.0` and `net10.0`.

## Features

- Automatic per-client or global logging through `IHttpClientFactory` and a standard `DelegatingHandler`.
- Request and response filtering, configurable log levels, and opt-in field capture.
- Safe defaults for URI and header redaction.
- Configurable text detection, content redaction, buffering, and truncation.
- Custom text formatting, structured properties, and stable event IDs.
- Synchronous and asynchronous `HttpClient` pipelines.

## Installation

```bash
dotnet add package BlueBird.Http.Logging
```

## Quick Start

Enable BlueBird for every client created by `IHttpClientFactory` and remove Microsoft's built-in HTTP logging to avoid duplicate entries:

```csharp
using System;
using BlueBird.Http.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

services.ConfigureHttpClientDefaults(builder =>
{
    builder.RemoveAllLoggers();

    builder.AddHttpClientLogging(options =>
    {
        options.RequestFieldsSelector = _ =>
            HttpLogFields.All;

        options.ResponseFieldsSelector = (_, _) =>
            HttpLogFields.All;

        options.ResponseLevelSelector = (_, response) =>
            (int)response.StatusCode switch
            {
                >= 500 => LogLevel.Error,
                >= 400 => LogLevel.Warning,
                _ => LogLevel.Information,
            };
    });
});

services.AddHttpClient("api", client =>
{
    client.BaseAddress = new Uri("https://api.example.com/");
});
```

This configuration selects all request and response fields, subject to the configured content detection and limits. Responses use `Information`, `Warning`, or `Error` for status codes below 400, from 400 to 499, or 500 and above, respectively. Exceptions use the default `Error` level.

Request and response content may contain sensitive data. In production, capture only the required fields and configure content redactors when bodies are included.

A typical successful log looks like this:

```text
Request: GET https://api.example.com/orders HTTP/1.1
RequestTime: 2026-08-02T10:30:00.0000000+00:00
Request Headers:
    [None]
Content Headers:
    [None]
Content:
    [No content]
----------------------------------------
Response: 200 OK HTTP/1.1
ResponseTime: 2026-08-02T10:30:00.1230000+00:00
Duration: 123.00 ms
Response Headers:
    [None]
Content Headers:
    Content-Type: application/json; charset=utf-8
    Content-Length: 10
Content:
    [{"id":1}]
```

## Configuring an Individual Client

Use `AddHttpClientLogging` directly on an `IHttpClientBuilder` when only a specific client should use BlueBird:

```csharp
services.AddHttpClient("api")
    .RemoveAllLoggers()
    .AddHttpClientLogging(options =>
    {
        options.ResponseFieldsSelector = (_, response) =>
            response.IsSuccessStatusCode
                ? HttpLogFields.None
                : HttpLogFields.Headers;
    });
```

Global and per-client registration apply only to clients created by `IHttpClientFactory`; they cannot intercept clients constructed directly with `new HttpClient()`. After enabling BlueBird globally, do not call `AddHttpClientLogging()` again on an individual client unless duplicate BlueBird entries are intended.

## Disabling the Built-in HttpClient Logging

`IHttpClientFactory` adds Microsoft's default HTTP logging automatically. The preceding examples call `RemoveAllLoggers()` so each exchange produces only a BlueBird entry. Omit that call if both logging implementations are desired.

`RemoveAllLoggers()` also removes previously registered custom `IHttpClientLogger` implementations. It does not remove BlueBird because BlueBird is registered as a `DelegatingHandler`.

For brevity, subsequent examples omit `RemoveAllLoggers()`. Include it when BlueBird should replace the built-in HTTP logging.

## Selecting Logged Fields

Use `RequestFieldsSelector` and `ResponseFieldsSelector` to opt in to headers or content:

```csharp
using BlueBird.Http.Logging;

services.AddHttpClient("api")
    .AddHttpClientLogging(options =>
    {
        options.RequestFieldsSelector = _ =>
            HttpLogFields.Headers | HttpLogFields.ContentHeaders;

        options.ResponseFieldsSelector = (_, response) =>
            response.IsSuccessStatusCode
                ? HttpLogFields.None
                : HttpLogFields.Headers | HttpLogFields.ContentHeaders;
    });
```

`HttpLogFields` supports these flags:

| Value | Captures |
|-------|----------|
| `None` | No headers or content. Request/response summary data is still logged. |
| `Headers` | HTTP message headers, excluding content headers. |
| `ContentHeaders` | HTTP content headers. |
| `Content` | Text content or binary-content metadata. |
| `All` | Headers, content headers, and content. |

Because content bodies may contain sensitive data, select `Content` only when appropriate redaction is configured. See [Redaction](#redaction).

## How It Works

For each enabled request, the handler:

1. Applies the request filter and field selector, then buffers selected textual request content within the configured byte limit.
2. Invokes the inner handler pipeline and measures its duration.
3. Applies the response filter, log-level selector, and field selector, then buffers selected textual response content.
4. Captures redacted and truncated values in an `HttpLogEntry`, then formats and writes it through `ILogger` with structured properties.

The formatter receives prepared values and never receives the original `HttpRequestMessage`, `HttpResponseMessage`, or content streams.

## Configuration Reference

`HttpClientLoggingOptions` is configured independently for each registered client.

| Option | Default | Purpose |
|--------|---------|---------|
| `IsEnabled` | `true` | Enables or disables logging for the handler. |
| `RequestFilter` | `null` | Skips the entire logging pipeline for selected requests. |
| `ResponseFilter` | `null` | Skips logging for selected completed responses. |
| `ResponseLevelSelector` | `Information` | Selects the level for completed HTTP exchanges. |
| `ExceptionLevelSelector` | `Error` | Selects the level for HTTP pipeline exceptions. |
| `RequestFieldsSelector` | `None` | Selects request headers and content to capture. |
| `ResponseFieldsSelector` | `None` | Selects response headers and content to capture. |
| `RequestUriRedactor` | Built-in redactor | Transforms the URI before capture. |
| `RequestHeaderRedactor` | Built-in redactor | Transforms request header values before capture. |
| `ResponseHeaderRedactor` | Built-in redactor | Transforms response header values before capture. |
| `ShouldTreatContentAsText` | Built-in detector | Determines whether selected content is logged as text. |
| `RequestContentRedactor` | `null` | Transforms textual request content before truncation. |
| `ResponseContentRedactor` | `null` | Transforms textual response content before truncation. |
| `RequestContentMaxChars` | `4096` | Limits transformed request text written to the log. |
| `ResponseContentMaxChars` | `4096` | Limits transformed response text written to the log. |
| `RequestContentBufferMaxBytes` | `1048576` | Limits request-content buffering to 1 MiB. |
| `ResponseContentBufferMaxBytes` | `1048576` | Limits response-content buffering to 1 MiB. |
| `Formatter` | `DefaultHttpLogFormatter` | Controls the final text layout. |

Configure options before sending requests and do not modify them while requests are in progress. Delegate-valued options may be called concurrently and must be thread-safe.

## Filtering and Log Levels

`RequestFilter` runs before the request is sent. Returning `false` bypasses all logging for that request, including exception logging.

`ResponseFilter` runs only after a response is received. Returning `false` returns the response without creating a log entry.

```csharp
using System.Net;
using BlueBird.Http.Logging;
using Microsoft.Extensions.Logging;

services.AddHttpClient("api")
    .AddHttpClientLogging(options =>
    {
        options.RequestFilter = request =>
            request.RequestUri?.AbsolutePath != "/health";

        options.ResponseFilter = (_, response) =>
            response.StatusCode != HttpStatusCode.NotModified;

        options.ResponseLevelSelector = (_, response) =>
            (int)response.StatusCode switch
            {
                >= 500 => LogLevel.Error,
                >= 400 => LogLevel.Warning,
                _ => LogLevel.Information,
            };

        options.ExceptionLevelSelector = (_, exception) =>
            exception is OperationCanceledException
                ? LogLevel.Debug
                : LogLevel.Error;
    });
```

HTTP 4xx and 5xx status codes are completed responses, not handler exceptions. `ExceptionLevelSelector` applies only when the HTTP pipeline throws. An exception thrown later by application code, such as from `EnsureSuccessStatusCode()`, occurs outside this logging handler.

## Redaction

The default configuration is conservative:

- URI user information, query strings, and fragments are omitted.
- Common authentication, cookie, API key, and token header values are replaced with `[REDACTED]`.
- Headers and content are not captured unless selected.
- Application-specific request and response content redaction is disabled by default.

Use content redactors for sensitive fields in textual bodies:

```csharp
services.AddHttpClient("api")
    .AddHttpClientLogging(options =>
    {
        options.RequestFieldsSelector = _ => HttpLogFields.Content;
        options.ResponseFieldsSelector = (_, _) => HttpLogFields.Content;

        options.RequestContentRedactor = content =>
            content.Replace("secret", "[REDACTED]", StringComparison.OrdinalIgnoreCase);

        options.ResponseContentRedactor = content =>
            content.Replace("secret", "[REDACTED]", StringComparison.OrdinalIgnoreCase);
    });
```

Content redactors run before truncation. Header and URI redactors can be set to `null` to log original values, but doing so may expose credentials or personal data.

## Content Detection and Limits

The built-in detector treats these media types as text:

- `text/*`
- `application/json` and types ending in `+json`
- `application/xml` and types ending in `+xml`
- `application/x-www-form-urlencoded`
- `application/javascript`
- `application/graphql`

Unknown media types are treated as binary. Selected binary content is not decoded; the log contains only its media type and known size.

Extend the built-in detector for additional textual formats:

```csharp
services.AddHttpClient("api")
    .AddHttpClientLogging(options =>
    {
        var defaultDetector = options.ShouldTreatContentAsText;

        options.ShouldTreatContentAsText = content =>
            defaultDetector(content) ||
            string.Equals(
                content.Headers.ContentType?.MediaType,
                "application/yaml",
                StringComparison.OrdinalIgnoreCase);
    });
```

Exceptions thrown by `ShouldTreatContentAsText` are propagated and may prevent the HTTP operation from completing.

Two independent limits apply to textual content:

| Limit | Unit | Behavior |
|-------|------|----------|
| `RequestContentBufferMaxBytes` / `ResponseContentBufferMaxBytes` | Bytes | Content without a declared `Content-Length`, or with a declared length above the limit, is not buffered and is omitted. |
| `RequestContentMaxChars` / `ResponseContentMaxChars` | UTF-16 code units | Text is truncated after redaction without splitting a surrogate pair. `null` disables truncation; `0` emits only a truncation notice for non-empty text. |

The default limits are 1 MiB for buffering and 4096 UTF-16 code units for log output. The UTF-16 code-unit limit controls output size, not how much content must be read into memory.

### Buffering and cancellation

Content is decoded for logging only after successful buffering, so later reads use the buffered copy rather than the original stream.

The request cancellation token is checked before buffering, but `LoadIntoBufferAsync(long)` cannot be canceled once started. Response buffering does not use the request cancellation token.

If buffering itself throws after reading has started, the exception is propagated because the underlying stream may no longer be safely reusable.

## Custom Formatting

Implement `IHttpLogFormatter` to replace the default human-readable layout. The formatter receives an `HttpLogEntry` containing logging-ready snapshots:

```csharp
using System.Text.Json;
using BlueBird.Http.Logging;

public sealed class JsonHttpLogFormatter : IHttpLogFormatter
{
    public string Format(HttpLogEntry entry)
    {
        return JsonSerializer.Serialize(new
        {
            Method = entry.Request.Method,
            entry.Request.Uri,
            StatusCode = entry.Response?.StatusCode,
            DurationMs = entry.Duration.TotalMilliseconds,
            Exception = entry.Exception?.Message,
        });
    }
}
```

Register it for a client:

```csharp
services.AddHttpClient("api")
    .AddHttpClientLogging(options =>
    {
        options.Formatter = new JsonHttpLogFormatter();
    });
```

Formatter instances may be called concurrently and must be thread-safe. `DefaultHttpLogFormatter` is stateless and thread-safe.

## Log Entry Model

```text
HttpLogEntry
|-- HttpRequestLog Request
|-- HttpResponseLog? Response
|-- Exception? Exception
`-- TimeSpan Duration
```

Entries created by the handler contain a response when the inner pipeline returns one, or an exception when it throws, but never both. `HttpRequestLog` and `HttpResponseLog` contain captured scalar values, read-only header dictionaries, and prepared content.

`Duration` measures time spent in the inner handler pipeline until it returns a response or throws. It excludes content buffering, snapshot capture, formatting, and logger execution.

Handler-generated `RequestTime` and `ResponseTime` values use UTC. `DefaultHttpLogFormatter` writes them using the round-trip `"O"` format.

## Structured Properties and Event IDs

When registered through `AddHttpClientLogging`, the logger category is `BlueBird.Http.Logging.HttpClient.{ClientName}`. The default client uses `BlueBird.Http.Logging.HttpClient.Default`.

Completed and failed HTTP entries include these structured properties:

| Property | Value |
|----------|-------|
| `HttpMethod` | Request method, such as `GET` or `POST`. |
| `Uri` | URI prepared for logging; redacted by default. |
| `StatusCode` | Numeric response status, or `null` when the inner pipeline throws. |
| `DurationMs` | Inner-handler pipeline duration in milliseconds. |
| `HttpLog` | Text produced by the configured formatter. |

The handler uses stable event IDs:

| ID | Name | Meaning |
|---:|------|---------|
| 1 | `HttpRequestCompleted` | A response was received and logged. |
| 2 | `HttpRequestFailed` | The HTTP pipeline threw an exception. |
| 3 | `HttpLoggingFailed` | Capturing, formatting, or writing a log entry failed. |

For `HttpRequestFailed`, the original HTTP exception is passed separately to `ILogger`, so structured logging providers retain its type and stack trace. The default formatter does not duplicate the exception text in `HttpLog`.

## Manual Handler Setup

The handler can also be used without `IHttpClientFactory`:

```csharp
var options = new HttpClientLoggingOptions
{
    ResponseFieldsSelector = (_, response) =>
        response.IsSuccessStatusCode
            ? HttpLogFields.None
            : HttpLogFields.Headers | HttpLogFields.ContentHeaders,
};

var handler = new HttpClientLoggingHandler(logger, options)
{
    InnerHandler = new HttpClientHandler(),
};

using var client = new HttpClient(handler);
```

## Logging Failures

Failures while capturing snapshots, formatting entries, or writing through `ILogger` are isolated from a completed response or an existing HTTP exception. When possible, the handler reports them as a warning with the `HttpLoggingFailed` event.

Exceptions from filters, selectors, content detection, and buffering are propagated to the caller. Application-provided delegates should be fast and should not throw.

## License

MIT
