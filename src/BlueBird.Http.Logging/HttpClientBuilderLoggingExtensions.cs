using System;
using BlueBird.Http.Logging;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for registering <see cref="HttpClientLoggingHandler"/> with
    /// dependency injection and <see cref="System.Net.Http.IHttpClientFactory"/> pipelines.
    /// </summary>
    public static class HttpClientBuilderLoggingExtensions
    {
        private const string LoggerCategoryPrefix = "BlueBird.Http.Logging.HttpClient";

        /// <summary>
        /// Adds an <see cref="HttpClientLoggingHandler"/> to the <see cref="System.Net.Http.HttpClient"/>
        /// pipeline represented by <paramref name="builder"/> and configures its options.
        /// </summary>
        /// <param name="builder">The <see cref="IHttpClientBuilder"/> for the client being configured.</param>
        /// <param name="configure">
        /// An optional delegate used to configure <see cref="HttpClientLoggingOptions"/>.
        /// When <c>null</c>, the default <see cref="HttpClientLoggingOptions"/> values are used.
        /// </param>
        /// <returns>The <paramref name="builder"/> for chaining.</returns>
        /// <remarks>
        /// The logger category uses the <c>BlueBird.Http.Logging.HttpClient.</c> prefix followed
        /// by the client name. The default client uses <c>Default</c> as its name.
        /// </remarks>
        public static IHttpClientBuilder AddHttpClientLogging(this IHttpClientBuilder builder, Action<HttpClientLoggingOptions>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.Services.Configure<HttpClientFactoryOptions>(builder.Name, factoryOptions =>
            {
                factoryOptions.HttpMessageHandlerBuilderActions.Add(handlerBuilder =>
                {
                    var options = new HttpClientLoggingOptions();
                    configure?.Invoke(options);

                    string clientName = string.IsNullOrEmpty(handlerBuilder.Name) ? "Default" : handlerBuilder.Name;
                    ILogger logger = handlerBuilder.Services.GetRequiredService<ILoggerFactory>().CreateLogger($"{LoggerCategoryPrefix}.{clientName}");

                    handlerBuilder.AdditionalHandlers.Add(new HttpClientLoggingHandler(logger, options));
                });
            });

            return builder;
        }
    }
}
