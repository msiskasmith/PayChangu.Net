using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using System.Net.Http.Headers;

namespace PayChangu.NET.Extensions;

/// <summary>
/// Provides extension methods for registering PayChangu services
/// with an <see cref="IServiceCollection"/>.
/// </summary>
public static class PayChanguServiceCollectionExtensions
{
    /// <summary>
    /// Registers the PayChangu client and configures its HTTP communication
    /// settings using dependency injection.
    /// </summary>
    /// <param name="services">
    /// The service collection to which the PayChangu client will be registered.
    /// </param>
    /// <param name="configureOptions">
    /// An action used to configure the PayChangu API options, including
    /// the required secret key and optional API base URL.
    /// </param>
    /// <param name="configureHttpClient">
    /// An optional action that allows additional configuration of the
    /// underlying <see cref="IHttpClientBuilder"/>, such as configuring
    /// a custom HTTP message handler.
    /// </param>
    /// <returns>
    /// The original <see cref="IServiceCollection"/> so that additional
    /// service registrations can be chained.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> or
    /// <paramref name="configureOptions"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the PayChangu secret key is missing or the configured
    /// API base URL is invalid.
    /// </exception>
    /// <remarks>
    /// The client is registered using <see cref="IHttpClientFactory"/>.
    /// Bearer token authentication and JSON accept headers are configured
    /// automatically.
    ///
    /// The default API base URL is <c>https://api.paychangu.com/</c>.
    /// </remarks>
    public static IServiceCollection AddPayChangu(
        this IServiceCollection services,
        Action<PayChanguOptions> configureOptions,
        Action<IHttpClientBuilder>? configureHttpClient = null)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        if (configureOptions == null)
            throw new ArgumentNullException(nameof(configureOptions));

        var options = new PayChanguOptions();
        configureOptions(options);

        if (string.IsNullOrWhiteSpace(options.SecretKey))
            throw new ArgumentException("PayChangu SecretKey is required.");

        if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var baseUri))
            throw new ArgumentException("Invalid PayChangu BaseUrl.");

        var builder = services.AddHttpClient<IPayChanguClient, PayChanguClient>(
            client =>
            {
                client.BaseAddress = baseUri;

                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", options.SecretKey);

                client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));
            });

        configureHttpClient?.Invoke(builder);

        services.AddSingleton<IPayChanguWebhookService>(
            _ => new PayChanguWebhookService(options.WebhookSecret));

        return services;
    }
}