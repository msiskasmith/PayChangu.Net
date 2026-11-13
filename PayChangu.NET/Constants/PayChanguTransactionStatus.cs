namespace PayChangu.NET.Constants;

/// <summary>
/// Defines the transaction statuses returned by PayChangu.
/// </summary>
public static class PayChanguTransactionStatus
{
    /// <summary>
    /// The transaction is awaiting completion.
    /// </summary>
    public const string Pending = "pending";

    /// <summary>
    /// The transaction completed successfully.
    /// </summary>
    public const string Success = "success";

    /// <summary>
    /// The transaction failed.
    /// </summary>
    public const string Failed = "failed";

    /// <summary>
    /// The transaction was refunded.
    /// </summary>
    public const string Refunded = "refunded";
}
