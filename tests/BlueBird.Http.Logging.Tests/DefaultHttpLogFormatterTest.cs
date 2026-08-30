using System;
using System.Collections.Generic;
using System.Net;

namespace BlueBird.Http.Logging.Tests;

public sealed class DefaultHttpLogFormatterTest
{
    private static HttpRequestLog CreateRequestLog(
        HttpLogFields fields = HttpLogFields.All,
        string? content = "request body",
        IReadOnlyDictionary<string, string>? headers = null,
        IReadOnlyDictionary<string, string>? contentHeaders = null,
        string? uri = "http://test/api")
    {
        return new HttpRequestLog
        {
            RequestTime = DateTimeOffset.UtcNow,
            Method = "POST",
            Uri = uri,
            Version = "1.1",
            Fields = fields,
            Headers = headers ?? new Dictionary<string, string>(),
            ContentHeaders = contentHeaders ?? new Dictionary<string, string>(),
            Content = content,
        };
    }

    private static HttpResponseLog CreateResponseLog(
        HttpLogFields fields = HttpLogFields.All,
        string? content = "response body",
        string? reasonPhrase = "OK")
    {
        return new HttpResponseLog
        {
            ResponseTime = DateTimeOffset.UtcNow,
            StatusCode = (int)HttpStatusCode.OK,
            ReasonPhrase = reasonPhrase,
            Version = "1.1",
            Fields = fields,
            Content = content,
        };
    }

    [Fact]
    public void Format_NoContent_ShowsNoContent()
    {
        var entry = new HttpLogEntry
        {
            Request = CreateRequestLog(content: null),
            Duration = TimeSpan.Zero,
        };

        string result = new DefaultHttpLogFormatter().Format(entry);

        Assert.Contains("[No content]", result);
    }

    [Fact]
    public void Format_TextContent_ShowsContent()
    {
        var entry = new HttpLogEntry
        {
            Request = CreateRequestLog(content: "{\"key\":\"value\"}"),
            Duration = TimeSpan.Zero,
        };

        string result = new DefaultHttpLogFormatter().Format(entry);

        Assert.Contains("{\"key\":\"value\"}", result);
    }

    [Fact]
    public void Format_PreTruncatedContent_PreservesCaptureResult()
    {
        const string content = "abcdefghij\n[Content truncated: total 26 characters, showing first 10]";
        var entry = new HttpLogEntry
        {
            Request = CreateRequestLog(content: content),
            Duration = TimeSpan.Zero,
        };

        string result = new DefaultHttpLogFormatter().Format(entry);

        Assert.Contains("abcdefghij", result);
        Assert.Contains("showing first 10", result);
    }

    [Fact]
    public void Format_ExcludedHeaders_OmitsSection()
    {
        var entry = new HttpLogEntry
        {
            Request = CreateRequestLog(
                fields: HttpLogFields.ContentHeaders | HttpLogFields.Content,
                headers: new Dictionary<string, string> { ["Authorization"] = "secret" }),
            Duration = TimeSpan.Zero,
        };

        string result = new DefaultHttpLogFormatter().Format(entry);

        Assert.DoesNotContain("Request Headers:", result);
        Assert.DoesNotContain("[Not logged]", result);
        Assert.DoesNotContain("secret", result);
    }

    [Fact]
    public void Format_CapturedEmptyHeaders_ShowsNone()
    {
        var entry = new HttpLogEntry
        {
            Request = CreateRequestLog(fields: HttpLogFields.Headers),
            Duration = TimeSpan.Zero,
        };

        string result = new DefaultHttpLogFormatter().Format(entry);

        Assert.Contains("Request Headers:", result);
        Assert.Contains("[None]", result);
    }

    [Fact]
    public void Format_CapturedHeaders_ShowsPreparedValues()
    {
        var entry = new HttpLogEntry
        {
            Request = CreateRequestLog(
                headers: new Dictionary<string, string>
                {
                    ["Authorization"] = "[REDACTED]",
                    ["Accept"] = "application/json",
                }),
            Duration = TimeSpan.Zero,
        };

        string result = new DefaultHttpLogFormatter().Format(entry);

        Assert.Contains("Authorization: [REDACTED]", result);
        Assert.Contains("Accept: application/json", result);
    }

    [Fact]
    public void Format_WithException_LeavesExceptionDetailsToLogger()
    {
        var entry = new HttpLogEntry
        {
            Request = CreateRequestLog(),
            Exception = new InvalidOperationException("Connection refused"),
            Duration = TimeSpan.FromMilliseconds(25),
        };

        string result = new DefaultHttpLogFormatter().Format(entry);

        Assert.Contains("Duration: 25.00 ms", result);
        Assert.DoesNotContain("Exception:", result);
        Assert.DoesNotContain("Connection refused", result);
        Assert.DoesNotContain(nameof(InvalidOperationException), result);
    }

    [Fact]
    public void Format_MissingUri_ShowsPlaceholder()
    {
        var entry = new HttpLogEntry
        {
            Request = CreateRequestLog(fields: HttpLogFields.None, uri: null),
            Duration = TimeSpan.Zero,
        };

        string result = new DefaultHttpLogFormatter().Format(entry);

        Assert.Contains("Request: POST [No URI] HTTP/1.1", result);
    }

    [Fact]
    public void Format_MissingReasonPhrase_DoesNotAddExtraSpace()
    {
        var entry = new HttpLogEntry
        {
            Request = CreateRequestLog(fields: HttpLogFields.None),
            Response = CreateResponseLog(fields: HttpLogFields.None, reasonPhrase: null),
            Duration = TimeSpan.Zero,
        };

        string result = new DefaultHttpLogFormatter().Format(entry);

        Assert.Contains("Response: 200 HTTP/1.1", result);
        Assert.DoesNotContain("Response: 200  HTTP/1.1", result);
    }

    [Fact]
    public void Format_ContentEndingWithNewline_DoesNotAddBlankLine()
    {
        var entry = new HttpLogEntry
        {
            Request = CreateRequestLog(fields: HttpLogFields.Content, content: "request body\n"),
            Duration = TimeSpan.Zero,
        };

        string result = new DefaultHttpLogFormatter().Format(entry);

        Assert.EndsWith($"Content:{Environment.NewLine}    request body{Environment.NewLine}", result);
    }

    [Fact]
    public void Format_WithResponse_IncludesRequestResponseAndDuration()
    {
        var entry = new HttpLogEntry
        {
            Request = CreateRequestLog(),
            Response = CreateResponseLog(),
            Duration = TimeSpan.FromMilliseconds(123.45),
        };

        string result = new DefaultHttpLogFormatter().Format(entry);

        Assert.Contains("Request:", result);
        Assert.Contains("Response:", result);
        Assert.Contains("200", result);
        Assert.Contains("123.45 ms", result);
        Assert.Contains("----------------------------------------", result);
    }
}
