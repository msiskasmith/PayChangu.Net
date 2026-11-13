using PayChangu.NET.Constants;
using PayChangu.NET.Models;
using System;

/// <summary>
/// Provides extension methods for validating PayChangu payment verification responses.
/// </summary>

public static class VerifyPaymentResponseExtensions
{
    /// <summary>
    /// Determines whether the payment verification response represents
    /// a successful transaction matching the expected transaction details.
    /// </summary>
    /// <param name="response">The payment verification response.</param>
    /// <param name="expectedTransactionIdentifier">
    /// The expected transaction identifier.
    /// </param>
    /// <param name="expectedCurrency">The expected transaction currency.</param>
    /// <param name="expectedAmount">The expected transaction amount.</param>
    /// <returns>
    /// <see langword="true"/> if the transaction was successfully completed
    /// and all expected values match; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool IsValid(
        this VerifyPaymentResponse? response,
        string expectedTransactionIdentifier,
        string expectedCurrency,
        decimal expectedAmount)
    {
        if (response == null)
        {
            return false;
        }

        return
            string.Equals(
                response.Status,
                PayChanguTransactionStatus.Success,
                StringComparison.OrdinalIgnoreCase) &&
            string.Equals(
                response.TransactionIdentifier,
                expectedTransactionIdentifier,
                StringComparison.Ordinal) &&
            string.Equals(
                response.Currency,
                expectedCurrency,
                StringComparison.OrdinalIgnoreCase) &&
            response.Amount == expectedAmount;
    }
}