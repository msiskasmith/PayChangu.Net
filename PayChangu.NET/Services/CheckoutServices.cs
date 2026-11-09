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
    /// <summary>Initiates a Standard Checkout payment.</summary>
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
