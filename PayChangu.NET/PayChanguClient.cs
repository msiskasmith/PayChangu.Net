using PayChangu.NET.Exceptions;
using PayChangu.NET.Models;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PayChangu.NET;

/// <summary>
/// Provides methods for interacting with the PayChangu payment gateway.
/// </summary>
/// <remarks>
/// Implementations handle HTTP communication, authentication, and JSON
/// serialization when communicating with the PayChangu API.
/// Register the client using <see cref="PayChanguServiceCollectionExtensions.AddPayChangu"/>.
/// </remarks>
public interface IPayChanguClient
{
    /// <summary>
    /// Initiates a payment using PayChangu Standard Checkout.
    /// </summary>
    /// <param name="request">
    /// The payment details, including the amount, customer information,
    /// transaction reference, and checkout configuration.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel the asynchronous operation.
    /// </param>
    /// <returns>
    /// A <see cref="PaymentResponse"/> containing the checkout URL and
    /// details of the created payment session.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="request"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="PayChanguException">
    /// Thrown when PayChangu returns an unsuccessful HTTP response or
    /// when the response cannot be deserialized.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled through the
    /// <paramref name="cancellationToken"/>.
    /// </exception>
    Task<PaymentResponse> InitiatePaymentAsync(
        PaymentRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Provides the HTTP implementation of <see cref="IPayChanguClient"/>.
/// </summary>
/// <remarks>
/// This implementation is registered through dependency injection
/// and is not intended to be instantiated directly by consumers.
/// </remarks>
internal sealed class PayChanguClient : IPayChanguClient
{
    private readonly HttpClient _httpClient;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="PayChanguClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client used for API communication.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="httpClient"/> is <see langword="null"/>.
    /// </exception>
    public PayChanguClient(HttpClient httpClient)
    {
        _httpClient = httpClient ??
            throw new ArgumentNullException(nameof(httpClient));
    }

    /// <inheritdoc />
    public async Task<PaymentResponse> InitiatePaymentAsync(
        PaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "payment");

        var json = JsonSerializer.Serialize(request, _jsonOptions);

        httpRequest.Content = new StringContent(
            json,
            Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.SendAsync(
            httpRequest,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new PayChanguException(
                $"PayChangu returned an error ({(int)response.StatusCode}): {content}",
                response.StatusCode,
                content);
        }

        try
        {
            var result = JsonSerializer.Deserialize<PaymentResponse>(
                content,
                _jsonOptions);

            return result ?? throw new PayChanguException(
                "PayChangu returned an empty response.");
        }
        catch (JsonException ex)
        {
            throw new PayChanguException(
                "Unable to deserialize PayChangu response.",
                ex);
        }
    }
}