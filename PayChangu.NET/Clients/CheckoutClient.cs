using PayChangu.NET.Models;
using PayChangu.NET.Models.Base;
using System.Threading;
using System.Threading.Tasks;

namespace PayChangu.NET;

/// <summary>
/// Provides Standard Checkout operations.
/// </summary>
public interface IPayChanguCheckoutClient
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
    /// A response containing the checkout URL and details of the created
    /// payment session, or failure details if the request was unsuccessful.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="request"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled.
    /// </exception>
    Task<PayChanguApiResponse<InitiateCheckoutResponse>> InitiateAsync(
        InitiateCheckoutRequest request,
        CancellationToken cancellationToken = default);
}

internal sealed class PayChanguCheckoutClient : IPayChanguCheckoutClient
{
    private readonly PayChanguClient _client;
    public PayChanguCheckoutClient(PayChanguClient client) => _client = client;
    public Task<PayChanguApiResponse<InitiateCheckoutResponse>> InitiateAsync(
        InitiateCheckoutRequest request, CancellationToken cancellationToken = default) =>
        _client.InitiateCheckoutAsync(request, cancellationToken);
}
