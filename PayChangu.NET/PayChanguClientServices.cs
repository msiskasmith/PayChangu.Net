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

/// <summary>
/// Provides Direct Charge operations grouped by payment method.
/// </summary>
public interface IPayChanguDirectChargesClient
{
    /// <summary>Gets Mobile Money Direct Charge operations.</summary>
    IPayChanguMobileMoneyDirectChargeClient MobileMoney { get; }

    /// <summary>Gets Bank Transfer Direct Charge operations.</summary>
    IPayChanguBankTransferDirectChargeClient BankTransfer { get; }

    /// <summary>Gets Card Direct Charge operations.</summary>
    IPayChanguCardDirectChargeClient Card { get; }
}

/// <summary>Provides Mobile Money Direct Charge operations.</summary>
public interface IPayChanguMobileMoneyDirectChargeClient
{
    /// <summary>Initiates a Mobile Money Direct Charge request.</summary>
    Task<PayChanguApiResponse<InitiateMobileMoneyDirectChargeResponse>> InitiateAsync(
        InitiateMobileMoneyDirectChargeRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Retrieves Mobile Money Direct Charge transaction details.</summary>
    Task<PayChanguApiResponse<GetMobileMoneyChargeDetailsResponse>> GetDetailsAsync(
        string chargeId,
        CancellationToken cancellationToken = default);
}

/// <summary>Provides Bank Transfer Direct Charge operations.</summary>
public interface IPayChanguBankTransferDirectChargeClient
{
    /// <summary>Initiates a Bank Transfer Direct Charge request.</summary>
    Task<PayChanguApiResponse<InitiateBankTransferDirectChargeResponse>> InitiateAsync(
        InitiateBankTransferDirectChargeRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Retrieves Bank Transfer Direct Charge transaction details.</summary>
    Task<PayChanguApiResponse<GetBankTransferDirectChargeDetailsResponse>> GetDetailsAsync(
        string chargeId,
        CancellationToken cancellationToken = default);
}

/// <summary>Provides Card Direct Charge operations.</summary>
public interface IPayChanguCardDirectChargeClient
{
    /// <summary>Initiates a Card Direct Charge request.</summary>
    Task<PayChanguApiResponse<InitiateCardDirectChargeResponse>> InitiateAsync(
        InitiateCardDirectChargeRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Refunds a Card Direct Charge transaction.</summary>
    Task<PayChanguApiResponse> RefundAsync(
        string chargeId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Provides payout operations grouped by payout method.
/// </summary>
public interface IPayChanguPayoutsClient
{
    /// <summary>Gets Mobile Money payout operations.</summary>
    IPayChanguMobileMoneyPayoutClient MobileMoney { get; }

    /// <summary>Gets bank payout operations.</summary>
    IPayChanguBankPayoutClient Bank { get; }
}

/// <summary>Provides Mobile Money payout operations.</summary>
public interface IPayChanguMobileMoneyPayoutClient
{
    /// <summary>Initiates a Mobile Money payout.</summary>
    Task<PayChanguApiResponse<InitiateMobileMoneyPayoutResponse>> InitiateAsync(
        InitiateMobileMoneyPayoutRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>Provides bank payout operations.</summary>
public interface IPayChanguBankPayoutClient
{
    /// <summary>Initiates a bank payout.</summary>
    Task<PayChanguApiResponse<InitiateBankPayoutResponse>> InitiateAsync(
        InitiateBankPayoutRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Retrieves the details of a bank payout.</summary>
    Task<PayChanguApiResponse<GetSingleBankPayoutDetailsResponse>> GetDetailsAsync(
        string chargeId,
        CancellationToken cancellationToken = default);

    /// <summary>Retrieves the available page of bank payout transactions.</summary>
    Task<PayChanguApiResponse<GetAllBankPayoutDetailsResponse>> GetAllAsync(
        CancellationToken cancellationToken = default);
}

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

/// <summary>Provides wallet operations.</summary>
public interface IPayChanguWalletClient
{
    /// <summary>Retrieves the wallet balance for the specified currency.</summary>
    Task<PayChanguApiResponse<GetBalanceResponse>> GetBalanceAsync(
        PayChanguCurrency currency = PayChanguCurrency.MWK,
        CancellationToken cancellationToken = default);
}

/// <summary>Provides access to PayChangu reference data.</summary>
public interface IPayChanguReferenceDataClient
{
    /// <summary>Gets the supported Mobile Money operators.</summary>
    IPayChanguMobileMoneyOperatorsClient MobileMoneyOperators { get; }

    /// <summary>Gets the supported banks and payout providers.</summary>
    IPayChanguBanksClient Banks { get; }
}

/// <summary>Provides Mobile Money operator reference data operations.</summary>
public interface IPayChanguMobileMoneyOperatorsClient
{
    /// <summary>Retrieves the supported Mobile Money operators.</summary>
    Task<PayChanguApiResponse<GetMobileMoneyOperatorsResponse>> GetAllAsync(
        CancellationToken cancellationToken = default);
}

/// <summary>Provides bank and payout provider reference data operations.</summary>
public interface IPayChanguBanksClient
{
    /// <summary>Retrieves banks and payout providers supported for a currency.</summary>
    Task<PayChanguApiResponse<GetSupportedBanksResponse>> GetSupportedAsync(
        PayChanguCurrency currency = PayChanguCurrency.MWK,
        CancellationToken cancellationToken = default);
}
