using System.Text.Json.Serialization;

/// <summary>
/// Represents the response returned when a mobile money payout is initiated.
/// </summary>
public sealed class InitiateMobileMoneyPayoutResponse
{
    /// <summary>
    /// Gets or sets the status of the API request.
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = null!;

    /// <summary>
    /// Gets or sets the response message.
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = null!;

    /// <summary>
    /// Gets or sets the mobile money payout transaction details.
    /// </summary>
    [JsonPropertyName("data")]
    public PayChanguPayoutData Data { get; set; } = null!;
}