using PayChangu.NET.Models;
using PayChangu.NET.Models.Base;
using System.Threading;
using System.Threading.Tasks;

namespace PayChangu.NET;

/// <summary>Provides transaction operations.</summary>
public interface IPayChanguTransactionsClient
{
    /// <summary>
    /// Verifies a PayChangu payment using its transaction reference or charge
    /// identifier.
    /// </summary>
    /// <remarks>
    /// Supports Standard Checkout and Direct Charge payments using Mobile
    /// Money, Bank Transfer, or Card. The returned verification data is
    /// normalized into a common response model.
    /// </remarks>
    /// <param name="transactionIdentifier">
    /// The merchant transaction reference for Checkout, or the PayChangu
    /// charge identifier for Direct Charge.
    /// </param>
    /// <param name="paymentFlow">The payment flow used to initiate the payment.</param>
    /// <param name="paymentMethod">The payment method used for the payment.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>
    /// A normalized payment verification response, or failure details if
    /// verification was unsuccessful.
    /// </returns>
    Task<PayChanguApiResponse<VerifyPaymentResponse>> VerifyAsync(
        string transactionIdentifier,
        PayChanguPaymentFlow paymentFlow,
        PayChanguPaymentMethod paymentMethod,
        CancellationToken cancellationToken = default);
}

internal sealed class PayChanguTransactionsClient : IPayChanguTransactionsClient
{
    private readonly PayChanguClient _client;
    public PayChanguTransactionsClient(PayChanguClient client) => _client = client;
    public Task<PayChanguApiResponse<VerifyPaymentResponse>> VerifyAsync(
        string transactionIdentifier, PayChanguPaymentFlow paymentFlow,
        PayChanguPaymentMethod paymentMethod, CancellationToken cancellationToken = default) =>
        _client.VerifyPaymentAsync(transactionIdentifier, paymentFlow, paymentMethod, cancellationToken);
}
