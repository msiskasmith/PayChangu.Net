using PayChangu.NET.Models;
using PayChangu.NET.Models.Base;
using System.Threading;
using System.Threading.Tasks;

namespace PayChangu.NET;

/// <summary>Provides wallet operations.</summary>
public interface IPayChanguWalletClient
{
    /// <summary>Retrieves the wallet balance for the specified currency.</summary>
    Task<PayChanguApiResponse<GetBalanceResponse>> GetBalanceAsync(
        PayChanguCurrency currency = PayChanguCurrency.MWK,
        CancellationToken cancellationToken = default);
}

internal sealed class PayChanguWalletClient : IPayChanguWalletClient
{
    private readonly PayChanguClient _client;
    public PayChanguWalletClient(PayChanguClient client) => _client = client;
    public Task<PayChanguApiResponse<GetBalanceResponse>> GetBalanceAsync(
        PayChanguCurrency currency = PayChanguCurrency.MWK,
        CancellationToken cancellationToken = default) =>
        _client.GetBalanceAsync(currency, cancellationToken);
}
