namespace PayChangu.NET.Constants;

internal sealed class PayChanguApiEndpoints
{
    internal sealed class Account
    {
        internal const string GetBalance = "wallet-balance?currency={Iso}";
    }

    internal sealed class HostedCheckout
    {
        internal const string InitiateTransaction = "payment";
        internal const string VerifyTransactionStatus = "verify-payment/{ref}";
    }

    internal sealed class DirectChargeMomo
    {
        internal const string GetMobileMoneyOperators = "mobile-money";
        internal const string ChargeMobileMoney = "mobile-money/payments/initialize";
        internal const string VerifyDirectChargeStatus = "mobile-money/payments/{id}/verify";
        internal const string GetSingleChargeDetails = "mobile-money/payments/{id}/details";
    }

    internal sealed class DirectChargeBankTransfer
    {
        internal const string ChargeBankTransfer = "direct-charge/payments/initialize";
        internal const string GetSingleChargeDetails = "direct-charge/transactions/{id}/details";
    }

    internal sealed class DirectChargeCard
    {
        internal const string DirectChargeBank = "charge-card/payments";
        internal const string VerifyDirectChargeStatus = "charge-card/verify/{id}";
        internal const string RefundCardCharge = "charge-card/refund/{id}";
    }

    internal sealed class Payout
    {
        internal const string GetOperators = "mobile-money/";
        internal const string MobileMoneyPayout = "mobile-money/payouts/initialize";
        internal const string GetSingleMomoPayoutDetails = "mobile-money/payments/{id}/details";

        internal const string GetBanks = "direct-charge/payouts/supported-banks?currency={Iso}";
        internal const string BankPayout = "direct-charge/payouts/initialize";
        internal const string GetSingleBankPayoutDetails = "direct-charge/payouts/{id}";
        internal const string GetAllBankPayoutDetails = "direct-charge/payouts";
    }

    internal sealed class BillPayments
    {
        internal const string GetAllAvailableBillers = "bills/getBillers";
        internal const string GetBillerDetails = "bills/getBillers/{id}";
        internal const string ValidateBill = "bills/validate";
        internal const string PayBill = "bills/pay";
        internal const string AirtimeRecharge = "bills/buy-airtime";
        internal const string GetBillDetails = "bills/getTransactions/{ref}";
        internal const string GetBillStatistics = "bills/getStatistics";
    }

    internal sealed class Connect
    {
        internal const string ConnectLink = "connect/authorize-link";
        internal const string AccessToken = "connect/user";
        internal const string RevokeAccessToken = "new-endpoint-2";
    }

    internal sealed class USBankCustomer
    {
        internal const string CreateCustomer = "virtual-account/api/customers/create";
        internal const string GetAllCustomers = "virtual-account/api/customers";
        internal const string GetSingleCustomer = "virtual-account/api/customers/{id}";
        internal const string UpdateSingleCustomer = "virtual-account/api/customers/{id}";
        internal const string DeleteSingleCustomer = "virtual-account/api/customers/{id}";
    }

    internal sealed class USBankAccount
    {
        internal const string CreateUSAccount = "virtual-account/api/customers/{id}/virtual-account";
        internal const string DeactivateUSAccount = "virtual-account/api/customers/{id}/virtual-account/deactivate";
        internal const string ReactivateUSAccount = "virtual-account/api/customers/{id}/virtual-account/reactivate";
        internal const string GetUSAccountActivity = "virtual-account/api/customers/{id}/virtual-account/activity";
    }
}