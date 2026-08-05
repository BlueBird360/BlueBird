using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BlueBird.Http.Logging.Tests;

/// <summary>
/// Tests for <see cref="HttpClientLoggingHandler"/> covering normal logging,
/// error logging, filtering, configuration, and body buffering with
/// non-seekable streams.
/// </summary>
public sealed class HttpClientLoggingHandlerTest
{
    private static HttpClientLoggingHandler CreateHandler(
        TestLogger logger,
        HttpClientLoggingOptions? options = null,
        Func<HttpRequestMessage, HttpResponseMessage>? handlerFunc = null)
    {
        var opts = options ?? new HttpClientLoggingOptions();
        var innerHandler = new FuncDelegatingHandler(handlerFunc ?? (request =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("response body"),
            }));

        var handler = new HttpClientLoggingHandler(logger, opts);
        handler.InnerHandler = innerHandler;
        return handler;
    }

    // ── Basic functionality ─────────────────────────────────────────

    [Fact]
    public async Task IsEnabledFalse_SkipsAllLogging()
    {
        var logger = new TestLogger();
        var options = new HttpClientLoggingOptions { IsEnabled = false };
        var handler = CreateHandler(logger, options);

        var client = new HttpClient(handler);
        await client.GetAsync("http://test/api");

        Assert.Empty(logger.Entries);
    }

    [Fact]
    public async Task NormalRequest_LogsAtConfiguredLevel()
    {
        var logger = new TestLogger();
        var handler = CreateHandler(logger, new HttpClientLoggingOptions
        {
            ResponseLevelSelector = (_, _) => LogLevel.Warning,
        });

        var client = new HttpClient(handler);
        await client.GetAsync("http://test/api");

        Assert.NotEmpty(logger.Entries);
        Assert.Equal(LogLevel.Warning, logger.Entries[0].Level);
    }

    [Fact]
    public async Task NormalRequest_LogContainsRequestAndResponse()
    {
        var logger = new TestLogger();
        var handler = CreateHandler(logger);

        var client = new HttpClient(handler);
        await client.GetAsync("http://test/api");

        string logText = logger.Entries[0].Message;
        Assert.Contains("Request:", logText);
        Assert.Contains("Response:", logText);
        Assert.Contains("http://test/api", logText);
        Assert.Contains("200", logText);
        Assert.Equal(1, logger.Entries[0].EventId.Id);
        Assert.Equal("HttpRequestCompleted", logger.Entries[0].EventId.Name);
        Assert.Equal("GET", logger.Entries[0].Properties["HttpMethod"]);
        Assert.Equal(200, logger.Entries[0].Properties["StatusCode"]);
        Assert.True((double)logger.Entries[0].Properties["DurationMs"]! >= 0);
    }

    [Fact]
    public async Task FailedRequest_LogsAtErrorLevel()
    {
        var logger = new TestLogger();
        var handler = CreateHandler(logger, new HttpClientLoggingOptions(), request =>
        {
            throw new HttpRequestException("Connection refused");
        });

        var client = new HttpClient(handler);
        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync("http://test/api"));

        Assert.NotEmpty(logger.Entries);
        Assert.Equal(LogLevel.Error, logger.Entries[0].Level);
        Assert.Equal(2, logger.Entries[0].EventId.Id);
        Assert.Equal("HttpRequestFailed", logger.Entries[0].EventId.Name);
        Assert.DoesNotContain("Connection refused", logger.Entries[0].Message);
        Assert.IsType<HttpRequestException>(logger.Entries[0].Exception);
        Assert.Equal("Connection refused", logger.Entries[0].Exception!.Message);
    }

    [Fact]
    public async Task RequestFilterFalse_SkipsLogging()
    {
        var logger = new TestLogger();
        var options = new HttpClientLoggingOptions
        {
            RequestFilter = request => request.RequestUri!.Host != "skip-me",
        };
        var handler = CreateHandler(logger, options);

        var client = new HttpClient(handler);
        await client.GetAsync("http://skip-me/api");

        Assert.Empty(logger.Entries);
    }

    [Fact]
    public async Task ResponseFilterFalse_SkipsLogging()
    {
        var logger = new TestLogger();
        var options = new HttpClientLoggingOptions
        {
            ResponseFilter = (_, response) => response.StatusCode != HttpStatusCode.NotFound,
        };
        var handler = CreateHandler(logger, options, request =>
            new HttpResponseMessage(HttpStatusCode.NotFound));

        var client = new HttpClient(handler);
        await client.GetAsync("http://test/api");

        Assert.Empty(logger.Entries);
    }

    [Fact]
    public async Task CustomRequestHeaderRedactor_TransformsCapturedValues()
    {
        var logger = new TestLogger();
        var options = new HttpClientLoggingOptions
        {
            RequestHeaderRedactor = (key, value) => key == "Authorization" ? "[REDACTED]" : value,
            RequestFieldsSelector = _ => HttpLogFields.Headers,
        };
        var handler = CreateHandler(logger, options, _ =>
            new HttpResponseMessage(HttpStatusCode.OK));

        var client = new HttpClient(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, "http://test/api");
        request.Headers.Add("Authorization", "Bearer secret-token");
        request.Headers.Add("Accept", "application/json");
        await client.SendAsync(request);

        string logText = logger.Entries[0].Message;
        Assert.Contains("[REDACTED]", logText);
        Assert.DoesNotContain("secret-token", logText);
        Assert.Contains("application/json", logText);
    }

    [Fact]
    public async Task DefaultRedaction_RemovesSensitiveUriPartsAndHeaderValues()
    {
        var logger = new TestLogger();
        var options = new HttpClientLoggingOptions
        {
            RequestFieldsSelector = _ => HttpLogFields.Headers,
            ResponseFieldsSelector = (_, _) => HttpLogFields.Headers,
        };
        var handler = CreateHandler(logger, options, _ =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Headers.TryAddWithoutValidation("Set-Cookie", "session=response-cookie-secret");
            response.Headers.TryAddWithoutValidation("X-Api-Key", "response-api-key-secret");
            return response;
        });

        using var client = new HttpClient(handler);
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "http://request-user:request-password@test/api?token=request-query-secret#request-fragment-secret");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "request-authorization-secret");
        request.Headers.TryAddWithoutValidation("Cookie", "session=request-cookie-secret");
        request.Headers.TryAddWithoutValidation("X-Api-Key", "request-api-key-secret");

        using HttpResponseMessage response = await client.SendAsync(request);

        Assert.Single(logger.Entries);
        string logText = logger.Entries[0].Message;
        Assert.Contains("Authorization: [REDACTED]", logText);
        Assert.Contains("Cookie: [REDACTED]", logText);
        Assert.Contains("Set-Cookie: [REDACTED]", logText);
        Assert.Contains("X-Api-Key: [REDACTED]", logText);
        Assert.DoesNotContain("request-user", logText);
        Assert.DoesNotContain("request-password", logText);
        Assert.DoesNotContain("request-query-secret", logText);
        Assert.DoesNotContain("request-fragment-secret", logText);
        Assert.DoesNotContain("request-authorization-secret", logText);
        Assert.DoesNotContain("request-cookie-secret", logText);
        Assert.DoesNotContain("request-api-key-secret", logText);
        Assert.DoesNotContain("response-cookie-secret", logText);
        Assert.DoesNotContain("response-api-key-secret", logText);
        Assert.Equal("http://test/api", Assert.IsType<string>(logger.Entries[0].Properties["Uri"]));
    }

    [Fact]
    public async Task DefaultUriRedaction_RemovesQueryAndFragmentFromRelativeUri()
    {
        var logger = new TestLogger();
        var handler = CreateHandler(logger);

        using var invoker = new HttpMessageInvoker(handler);
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            new Uri("relative/path?token=query-secret#fragment-secret", UriKind.Relative));
        using HttpResponseMessage response = await invoker.SendAsync(request, CancellationToken.None);

        Assert.Single(logger.Entries);
        Assert.Equal("relative/path", Assert.IsType<string>(logger.Entries[0].Properties["Uri"]));
        Assert.DoesNotContain("query-secret", logger.Entries[0].Message);
        Assert.DoesNotContain("fragment-secret", logger.Entries[0].Message);
    }

    [Fact]
    public async Task DefaultFields_OmitRequestAndResponseContent()
    {
        var logger = new TestLogger();
        var handler = CreateHandler(logger, handlerFunc: _ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("response-sensitive-data"),
            });

        using var client = new HttpClient(handler);
        using var request = new HttpRequestMessage(HttpMethod.Post, "http://test/api")
        {
            Content = new StringContent("request-sensitive-data"),
        };
        using HttpResponseMessage response = await client.SendAsync(request);

        string logText = logger.Entries[0].Message;
        Assert.DoesNotContain("Content:", logText);
        Assert.DoesNotContain("request-sensitive-data", logText);
        Assert.DoesNotContain("response-sensitive-data", logText);
    }

    [Fact]
    public async Task RequestAndResponseContentLimits_TruncateIndependently()
    {
        const string requestContent = "REQUEST-abcdefghijklmnopqrstuvwxyz";
        const string responseContent = "RESPONSE-abcdefghijklmnopqrstuvwxyz";
        var logger = new TestLogger();
        var options = new HttpClientLoggingOptions
        {
            RequestContentMaxChars = 8,
            ResponseContentMaxChars = 9,
            RequestFieldsSelector = _ => HttpLogFields.Content,
            ResponseFieldsSelector = (_, _) => HttpLogFields.Content,
        };
        var handler = CreateHandler(logger, options, _ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseContent),
            });

        using var client = new HttpClient(handler);
        using var request = new HttpRequestMessage(HttpMethod.Post, "http://test/api")
        {
            Content = new StringContent(requestContent),
        };
        using HttpResponseMessage response = await client.SendAsync(request);

        string logText = logger.Entries[0].Message;
        Assert.Contains("REQUEST-", logText);
        Assert.Contains("showing first 8", logText);
        Assert.Contains("RESPONSE-", logText);
        Assert.Contains("showing first 9", logText);
        Assert.DoesNotContain(requestContent, logText);
        Assert.DoesNotContain(responseContent, logText);
    }

    [Fact]
    public async Task ContentRedactors_TransformCapturedText()
    {
        var logger = new TestLogger();
        var options = new HttpClientLoggingOptions
        {
            RequestFieldsSelector = _ => HttpLogFields.Content,
            ResponseFieldsSelector = (_, _) => HttpLogFields.Content,
            RequestContentRedactor = value => value.Replace("request-secret", "[REDACTED]", StringComparison.Ordinal),
            ResponseContentRedactor = value => value.Replace("response-secret", "[REDACTED]", StringComparison.Ordinal),
        };
        var handler = CreateHandler(logger, options, _ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("response-secret"),
            });

        using var client = new HttpClient(handler);
        using var request = new HttpRequestMessage(HttpMethod.Post, "http://test/api")
        {
            Content = new StringContent("request-secret"),
        };
        await client.SendAsync(request);

        Assert.DoesNotContain("request-secret", logger.Entries[0].Message);
        Assert.DoesNotContain("response-secret", logger.Entries[0].Message);
        Assert.Contains("[REDACTED]", logger.Entries[0].Message);
    }

    [Fact]
    public async Task CustomTextContentRule_LogsCustomMediaTypeAndRunsOnce()
    {
        var logger = new TestLogger();
        int invocationCount = 0;
        var options = new HttpClientLoggingOptions
        {
            ResponseFieldsSelector = (_, _) => HttpLogFields.Content,
            ShouldTreatContentAsText = content =>
            {
                invocationCount++;
                return content.Headers.ContentType?.MediaType == "application/yaml";
            },
        };
        var handler = CreateHandler(logger, options, _ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("name: BlueBird", Encoding.UTF8, "application/yaml"),
            });

        using var client = new HttpClient(handler);
        await client.GetAsync("http://test/api");

        Assert.Equal(1, invocationCount);
        Assert.Contains("name: BlueBird", logger.Entries[0].Message);
    }

    [Fact]
    public async Task TextContentRuleFailure_Propagates()
    {
        var logger = new TestLogger();
        var options = new HttpClientLoggingOptions
        {
            ResponseFieldsSelector = (_, _) => HttpLogFields.Content,
            ShouldTreatContentAsText = _ => throw new InvalidOperationException("Detection failed."),
        };
        var handler = CreateHandler(logger, options, _ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("secret response"),
            });

        using var client = new HttpClient(handler);
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.GetAsync("http://test/api"));

        Assert.Equal("Detection failed.", exception.Message);
        Assert.Empty(logger.Entries);
    }

    [Fact]
    public async Task ZeroLengthNonTextResponseContent_ShowsBinaryMetadata()
    {
        var logger = new TestLogger();
        var options = new HttpClientLoggingOptions
        {
            ResponseFieldsSelector = (_, _) => HttpLogFields.Content,
        };
        var handler = CreateHandler(logger, options, _ =>
            new HttpResponseMessage(HttpStatusCode.NoContent));

        using var client = new HttpClient(handler);
        using HttpResponseMessage response = await client.GetAsync("http://test/api");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Single(logger.Entries);
        Assert.Contains("[Binary content: unknown, 0 bytes]", logger.Entries[0].Message);
    }

    [Fact]
    public async Task TextContentWithIncorrectZeroLength_UsesActualContent()
    {
        var logger = new TestLogger();
        var options = new HttpClientLoggingOptions
        {
            ResponseFieldsSelector = (_, _) => HttpLogFields.Content,
        };
        var handler = CreateHandler(logger, options, _ =>
        {
            var content = new ByteArrayContent(Encoding.UTF8.GetBytes("actual response"));
            content.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
            content.Headers.ContentLength = 0;
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
        });

        using var client = new HttpClient(handler);
        using HttpResponseMessage response = await client.GetAsync("http://test/api");

        Assert.Single(logger.Entries);
        Assert.Contains("actual response", logger.Entries[0].Message);
    }

    [Fact]
    public async Task FormatterFailure_UsesLoggingFailureEventWithoutBreakingResponse()
    {
        var logger = new TestLogger();
        var options = new HttpClientLoggingOptions
        {
            Formatter = new ThrowingFormatter(),
        };
        var handler = CreateHandler(logger, options);

        using var client = new HttpClient(handler);
        using HttpResponseMessage response = await client.GetAsync("http://test/api");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single(logger.Entries);
        Assert.Equal(3, logger.Entries[0].EventId.Id);
        Assert.Equal("HttpLoggingFailed", logger.Entries[0].EventId.Name);
    }

    [Fact]
    public async Task TimestampsUseUtc()
    {
        var logger = new TestLogger();
        var formatter = new CapturingFormatter();
        var handler = CreateHandler(logger, new HttpClientLoggingOptions
        {
            Formatter = formatter,
        });

        using var client = new HttpClient(handler);
        using HttpResponseMessage response = await client.GetAsync("http://test/api");

        Assert.NotNull(formatter.Entry);
        Assert.Equal(TimeSpan.Zero, formatter.Entry.Request.RequestTime.Offset);
        Assert.Equal(TimeSpan.Zero, Assert.IsType<HttpResponseLog>(formatter.Entry.Response).ResponseTime.Offset);
    }

    /// <summary>
    /// Tests the synchronous <see cref="HttpClient.Send(HttpRequestMessage)"/> path.
    /// </summary>
    [Fact]
    public void SyncSend_LogsRequestAndResponse()
    {
        var logger = new TestLogger();
        var innerHandler = new FuncSyncDelegatingHandler(request =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("sync response"),
            });
        var handler = new HttpClientLoggingHandler(logger, new HttpClientLoggingOptions());
        handler.InnerHandler = innerHandler;

        using var client = new HttpClient(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "http://test/sync");
        using HttpResponseMessage response = client.Send(request);

        Assert.NotEmpty(logger.Entries);
        Assert.Contains("Request:", logger.Entries[0].Message);
        Assert.Contains("Response:", logger.Entries[0].Message);
        Assert.Equal("sync response", response.Content.ReadAsStringAsync().GetAwaiter().GetResult());
    }

    [Fact]
    public async Task AddHttpClientLogging_UsesNamedClientCategoryAndAppliesConfiguration()
    {
        var logger = new TestLogger();
        var loggerProvider = new TestLoggerProvider(logger);
        var services = new ServiceCollection();
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddProvider(loggerProvider);
        });
        services.AddHttpClient("api")
            .RemoveAllLoggers()
            .AddHttpClientLogging(options =>
            {
                options.ResponseLevelSelector = static (_, _) => LogLevel.Warning;
            })
            .ConfigurePrimaryHttpMessageHandler(() => new DummyInnerHandler());

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        IHttpClientFactory clientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        using HttpClient client = clientFactory.CreateClient("api");
        using HttpResponseMessage response = await client.GetAsync("http://test/api");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Warning, logger.Entries[0].Level);
        Assert.Contains("BlueBird.Http.Logging.HttpClient.api", loggerProvider.CategoryNames);
    }

    [Fact]
    public async Task AddHttpClientLogging_UsesDefaultClientLoggerCategory()
    {
        var logger = new TestLogger();
        var loggerProvider = new TestLoggerProvider(logger);
        var services = new ServiceCollection();
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddProvider(loggerProvider);
        });
        services.ConfigureHttpClientDefaults(builder =>
        {
            builder.RemoveAllLoggers();
            builder.AddHttpClientLogging();
            builder.ConfigurePrimaryHttpMessageHandler(() => new DummyInnerHandler());
        });
        services.AddHttpClient();

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        IHttpClientFactory clientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        using HttpClient client = clientFactory.CreateClient(string.Empty);
        using HttpResponseMessage response = await client.GetAsync("http://test/default");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single(logger.Entries);
        Assert.Contains("BlueBird.Http.Logging.HttpClient.Default", loggerProvider.CategoryNames);
    }

    [Fact]
    public async Task AddHttpClientLogging_UsesTypedClientLoggerCategory()
    {
        var logger = new TestLogger();
        var loggerProvider = new TestLoggerProvider(logger);
        var services = new ServiceCollection();
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddProvider(loggerProvider);
        });
        services.AddHttpClient<TestTypedClient>()
            .RemoveAllLoggers()
            .AddHttpClientLogging()
            .ConfigurePrimaryHttpMessageHandler(() => new DummyInnerHandler());

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        TestTypedClient client = serviceProvider.GetRequiredService<TestTypedClient>();
        using HttpResponseMessage response = await client.GetAsync("http://test/typed");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single(logger.Entries);
        Assert.Contains(
            $"BlueBird.Http.Logging.HttpClient.{nameof(TestTypedClient)}",
            loggerProvider.CategoryNames);
    }

    [Fact]
    public async Task ConfigureHttpClientDefaults_UsesActualClientNamesInLoggerCategories()
    {
        var logger = new TestLogger();
        var loggerProvider = new TestLoggerProvider(logger);
        var services = new ServiceCollection();
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddProvider(loggerProvider);
        });
        services.ConfigureHttpClientDefaults(builder =>
        {
            builder.RemoveAllLoggers();
            builder.AddHttpClientLogging();
        });
        services.AddHttpClient("orders")
            .ConfigurePrimaryHttpMessageHandler(() => new DummyInnerHandler());
        services.AddHttpClient("payments")
            .ConfigurePrimaryHttpMessageHandler(() => new DummyInnerHandler());

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        IHttpClientFactory clientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        using HttpClient ordersClient = clientFactory.CreateClient("orders");
        using HttpClient paymentsClient = clientFactory.CreateClient("payments");
        using HttpResponseMessage ordersResponse = await ordersClient.GetAsync("http://test/orders");
        using HttpResponseMessage paymentsResponse = await paymentsClient.GetAsync("http://test/payments");

        Assert.Equal(HttpStatusCode.OK, ordersResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, paymentsResponse.StatusCode);
        Assert.Equal(2, logger.Entries.Count);
        Assert.Contains("BlueBird.Http.Logging.HttpClient.orders", loggerProvider.CategoryNames);
        Assert.Contains("BlueBird.Http.Logging.HttpClient.payments", loggerProvider.CategoryNames);
    }

    // ── Non-seekable stream + buffer limit tests ────────────────────

    [Fact]
    public async Task UnseekableRequestBody_WithinBufferLimit_LogsContent()
    {
        var logger = new TestLogger();
        var handler = new HttpClientLoggingHandler(logger, new HttpClientLoggingOptions
        {
            RequestFieldsSelector = _ => HttpLogFields.Content,
        })
        {
            InnerHandler = new StreamConsumingHandler(),
        };

        using var client = new HttpClient(handler);
        var stream = new UnseekableStream(new MemoryStream(Encoding.UTF8.GetBytes("{\"hello\":\"world\"}")));
        var content = new StreamContent(stream);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        content.Headers.ContentLength = Encoding.UTF8.GetByteCount("{\"hello\":\"world\"}");

        var request = new HttpRequestMessage(HttpMethod.Post, "http://test/api") { Content = content };
        await client.SendAsync(request);

        Assert.Single(logger.Entries);
        string logText = logger.Entries[0].Message;
        Assert.Contains("{\"hello\":\"world\"}", logText);
    }

    [Fact]
    public async Task UnseekableRequestBody_ExceedsBufferLimit_OmitsContent()
    {
        var logger = new TestLogger();
        var options = new HttpClientLoggingOptions
        {
            RequestContentBufferMaxBytes = 100,
            RequestFieldsSelector = _ => HttpLogFields.Content,
        };
        var innerHandler = new StreamConsumingHandler();
        var handler = new HttpClientLoggingHandler(logger, options)
        {
            InnerHandler = innerHandler,
        };

        using var client = new HttpClient(handler);
        var bodyBytes = new byte[200];
        var stream = new UnseekableStream(new MemoryStream(bodyBytes));
        var content = new StreamContent(stream);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        content.Headers.ContentLength = 200;

        using var request = new HttpRequestMessage(HttpMethod.Post, "http://test/api") { Content = content };
        using HttpResponseMessage response = await client.SendAsync(request);

        // HTTP call succeeds — body passes through to inner handler without buffering.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(200L, innerHandler.ConsumedRequestContentLength);
        Assert.Single(logger.Entries);
        string logText = logger.Entries[0].Message;
        Assert.DoesNotContain("Content:", logText);
    }

    [Fact]
    public async Task RequestBody_BufferingFailure_Propagates()
    {
        var logger = new TestLogger();
        var options = new HttpClientLoggingOptions
        {
            RequestFieldsSelector = _ => HttpLogFields.Content,
        };
        var handler = CreateHandler(logger, options);

        using var client = new HttpClient(handler);
        using var request = new HttpRequestMessage(HttpMethod.Post, "http://test/api")
        {
            Content = new ThrowingHttpContent(),
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.SendAsync(request));

        Assert.Equal("Buffering failed.", exception.Message);
        Assert.Empty(logger.Entries);
    }

    [Fact]
    public async Task UnseekableResponseBody_WithinBufferLimit_LogsContent()
    {
        var logger = new TestLogger();
        var options = new HttpClientLoggingOptions
        {
            ResponseFieldsSelector = (_, _) => HttpLogFields.Content,
        };
        var handler = CreateHandler(logger, options, handlerFunc: _ =>
        {
            var stream = new UnseekableStream(new MemoryStream(Encoding.UTF8.GetBytes("response data")));
            var content = new StreamContent(stream);
            content.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
            content.Headers.ContentLength = Encoding.UTF8.GetByteCount("response data");
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
        });

        using var client = new HttpClient(handler);
        using HttpResponseMessage response = await client.GetAsync("http://test/api");

        string responseBody = await response.Content.ReadAsStringAsync();
        Assert.Equal("response data", responseBody);
        Assert.Single(logger.Entries);
        Assert.Contains("response data", logger.Entries[0].Message);
    }

    [Fact]
    public async Task UnseekableResponseBody_ExceedsBufferLimit_OmitsContent()
    {
        var logger = new TestLogger();
        var options = new HttpClientLoggingOptions
        {
            ResponseContentBufferMaxBytes = 1024,
            ResponseFieldsSelector = (_, _) => HttpLogFields.Content,
        };
        var handler = CreateHandler(logger, options, handlerFunc: _ =>
        {
            var body = Encoding.UTF8.GetBytes(new string('A', 2048));
            var stream = new UnseekableStream(new MemoryStream(body));
            var content = new StreamContent(stream);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            content.Headers.ContentLength = 2048;
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
        });

        using var client = new HttpClient(handler);
        using HttpResponseMessage response = await client.GetAsync("http://test/api");

        // HTTP call succeeds — body not buffered, stream intact for caller.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new string('A', 2048), await response.Content.ReadAsStringAsync());
        Assert.Single(logger.Entries);
        Assert.DoesNotContain("Content:", logger.Entries[0].Message);
    }

    [Fact]
    public async Task UnseekableResponseBody_UnknownLength_OmitsContent()
    {
        var logger = new TestLogger();
        var options = new HttpClientLoggingOptions
        {
            ResponseContentBufferMaxBytes = 1024,
            ResponseFieldsSelector = (_, _) => HttpLogFields.Content,
        };
        var handler = CreateHandler(logger, options, handlerFunc: _ =>
        {
            // No Content-Length is available (simulates chunked transfer), so the body is
            // omitted without attempting unbounded buffering.
            var body = Encoding.UTF8.GetBytes(new string('A', 2048));
            var stream = new UnseekableStream(new MemoryStream(body));
            var content = new StreamContent(stream);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            // Do NOT set Content-Length — simulates chunked transfer encoding.
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
        });

        using var client = new HttpClient(handler);
        using HttpResponseMessage response = await client.GetAsync("http://test/api");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new string('A', 2048), await response.Content.ReadAsStringAsync());
        Assert.Single(logger.Entries);
        Assert.DoesNotContain("Content:", logger.Entries[0].Message);
    }

    [Fact]
    public async Task ResponseReceived_AfterRequestCancellation_IsLoggedAndReturned()
    {
        var logger = new TestLogger();
        using var cancellation = new CancellationTokenSource();
        var options = new HttpClientLoggingOptions
        {
            ResponseFieldsSelector = (_, _) => HttpLogFields.Content,
        };
        var handler = CreateHandler(logger, options, _ =>
        {
            cancellation.Cancel();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("response after cancellation"),
            };
        });

        using var client = new HttpClient(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "http://test/api");
        using HttpResponseMessage response = await client.SendAsync(request, cancellation.Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single(logger.Entries);
        Assert.Contains("response after cancellation", logger.Entries[0].Message);
    }
}

internal sealed class TestTypedClient
{
    private readonly HttpClient _client;

    public TestTypedClient(HttpClient client)
    {
        this._client = client;
    }

    public Task<HttpResponseMessage> GetAsync(string requestUri)
    {
        return this._client.GetAsync(requestUri);
    }
}

internal sealed class ThrowingFormatter : IHttpLogFormatter
{
    public string Format(HttpLogEntry entry)
    {
        throw new InvalidOperationException("Formatter failed.");
    }
}

internal sealed class CapturingFormatter : IHttpLogFormatter
{
    public HttpLogEntry? Entry { get; private set; }

    public string Format(HttpLogEntry entry)
    {
        this.Entry = entry;
        return string.Empty;
    }
}

internal sealed class ThrowingHttpContent : HttpContent
{
    public ThrowingHttpContent()
    {
        this.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        this.Headers.ContentLength = 1;
    }

    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
    {
        return Task.FromException(new InvalidOperationException("Buffering failed."));
    }

    protected override bool TryComputeLength(out long length)
    {
        length = 1;
        return true;
    }
}

// ── Test helpers ────────────────────────────────────────────────────────

/// <summary>
/// A <see cref="DelegatingHandler"/> that invokes a provided function from the asynchronous send path.
/// </summary>
internal sealed class FuncDelegatingHandler : DelegatingHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _func;

    public FuncDelegatingHandler(Func<HttpRequestMessage, HttpResponseMessage> func)
    {
        this._func = func;
        this.InnerHandler = new DummyInnerHandler();
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(this._func(request));
    }
}

/// <summary>
/// A <see cref="DelegatingHandler"/> that delegates to a provided synchronous function.
/// </summary>
internal sealed class FuncSyncDelegatingHandler : DelegatingHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _func;

    public FuncSyncDelegatingHandler(Func<HttpRequestMessage, HttpResponseMessage> func)
    {
        this._func = func;
        this.InnerHandler = new DummyInnerHandler();
    }

    protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return this._func(request);
    }
}

/// <summary>
/// A minimal inner handler that always returns 200 OK. Used as the bottom of the handler chain.
/// </summary>
internal sealed class DummyInnerHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
    }

    protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return new HttpResponseMessage(HttpStatusCode.OK);
    }
}

/// <summary>
/// A <see cref="Stream"/> wrapper that reports <see cref="CanSeek"/> as <c>false</c>,
/// preventing <see cref="StreamContent"/> from inferring <c>Content-Length</c>.
/// Simulates chunked transfer encoding.
/// </summary>
internal sealed class UnseekableStream : Stream
{
    private readonly Stream _inner;

    public UnseekableStream(Stream inner) => this._inner = inner;

    public override bool CanSeek => false;
    public override bool CanRead => true;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position
    {
        get => this._inner.Position;
        set => throw new NotSupportedException();
    }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override int Read(byte[] buffer, int offset, int count) => this._inner.Read(buffer, offset, count);
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        this._inner.ReadAsync(buffer, offset, count, cancellationToken);
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override void Flush() { }
}

/// <summary>
/// Simulates a real network handler: consumes the request body via <c>CopyToAsync(Stream.Null)</c>
/// (like writing to a network stream), then returns a pre-configured response.
/// Proves that non-seekable request bodies must be pre-buffered to be logged.
/// </summary>
internal sealed class StreamConsumingHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage>? _responseFactory;

    public StreamConsumingHandler(Func<HttpRequestMessage, HttpResponseMessage>? responseFactory = null)
    {
        this._responseFactory = responseFactory;
    }

    public long? ConsumedRequestContentLength { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Content != null)
        {
            using var buffer = new MemoryStream();
            await request.Content.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
            this.ConsumedRequestContentLength = buffer.Length;
        }

        return this._responseFactory?.Invoke(request)
               ?? new HttpResponseMessage(HttpStatusCode.OK)
               {
                   Content = new StringContent("response ok"),
               };
    }
}
