using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PayChangu.NET.Models;

/// <summary>
/// Represents the response returned when retrieving all bank payout transactions.
/// </summary>
public sealed class GetAllBankPayoutDetailsResponse
{
    /// <summary>
    /// Gets or sets the API response status.
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = null!;

    /// <summary>
    /// Gets or sets the API response message.
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = null!;

    /// <summary>
    /// Gets or sets the paginated bank payout transaction data.
    /// </summary>
    [JsonPropertyName("data")]
    public BankPayoutPaginationData Data { get; set; } = null!;
}

/// <summary>
/// Represents paginated bank payout transaction data.
/// </summary>
public sealed class BankPayoutPaginationData
{
    /// <summary>
    /// Gets or sets the current page number.
    /// </summary>
    [JsonPropertyName("current_page")]
    public int CurrentPage { get; set; }

    /// <summary>
    /// Gets or sets the total number of pages.
    /// </summary>
    [JsonPropertyName("total_pages")]
    public int TotalPages { get; set; }

    /// <summary>
    /// Gets or sets the number of transactions per page.
    /// </summary>
    [JsonPropertyName("per_page")]
    public int PerPage { get; set; }

    /// <summary>
    /// Gets or sets the URL of the next page, if available.
    /// </summary>
    [JsonPropertyName("next_page_url")]
    public string? NextPageUrl { get; set; }

    /// <summary>
    /// Gets or sets the bank payout transactions on the current page.
    /// </summary>
    [JsonPropertyName("data")]
    public List<BankPayoutTransaction> Transactions { get; set; } = new();
}