using PayChangu.NET.Models;
using PayChangu.NET.Models.Base;
using System.Threading;
using System.Threading.Tasks;

namespace PayChangu.NET;

/// <summary>Provides transaction operations.</summary>
public interface IPayChanguTransactionsClient
{
    /// <summary>Verifies a Checkout or Direct Charge transaction.</summary>
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
