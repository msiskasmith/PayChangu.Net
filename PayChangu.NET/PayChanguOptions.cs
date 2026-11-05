namespace PayChangu.NET;

/// <summary>
/// Provides configuration options for connecting to the PayChangu API.
/// </summary>
/// <remarks>
/// Configure these options when registering the PayChangu client using
/// <see cref="PayChanguServiceCollectionExtensions.AddPayChangu"/>.
/// </remarks>
public sealed class PayChanguOptions
{
    /// <summary>
    /// Gets or sets the PayChangu API secret key used for Bearer authentication.
    /// </summary>
    /// <remarks>
    /// This value is required. Keep your secret key secure and avoid exposing
    /// it in client-side applications or source control.
    /// </remarks>
    public string SecretKey { get; set; } = null!;

    /// <summary>
    /// Gets or sets the base URL of the PayChangu API.
    /// </summary>
    /// <value>
    /// Defaults to <c>https://api.paychangu.com/</c>.
    /// </value>
    public string BaseUrl { get; set; } = "https://api.paychangu.com/";

    /// <summary>
    /// Gets or sets the webhook secret key used to validate
    /// incoming PayChangu webhook signatures.
    /// </summary>
    public string? WebhookSecret { get; set; }
}