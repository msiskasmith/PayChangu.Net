namespace PayChangu.NET.Tests.Fakes;

public sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<
        HttpRequestMessage,
        CancellationToken,
        Task<HttpResponseMessage>>? _responseFactory;

    /// <summary>
    /// Gets or sets the response returned by the handler.
    /// </summary>
    public HttpResponseMessage? Response { get; set; }

    public HttpRequestMessage? LastRequest { get; private set; }

    public HttpMethod? RequestMethod { get; private set; }

    public Uri? RequestUri { get; private set; }

    public string? AuthorizationScheme { get; private set; }

    public string? AuthorizationParameter { get; private set; }

    public string? RequestContent { get; private set; }

    /// <summary>
    /// Creates a handler that uses a response assigned through the Response property.
    /// </summary>
    public FakeHttpMessageHandler()
    {
    }

    /// <summary>
    /// Creates a handler with a predefined HTTP response.
    /// </summary>
    public FakeHttpMessageHandler(HttpResponseMessage response)
    {
        Response = response ?? throw new ArgumentNullException(nameof(response));
    }

    /// <summary>
    /// Creates a handler that generates responses using the supplied factory.
    /// </summary>
    public FakeHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responseFactory)
    {
        _responseFactory = responseFactory ??
            throw new ArgumentNullException(nameof(responseFactory));
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        LastRequest = request;
        RequestMethod = request.Method;
        RequestUri = request.RequestUri;

        AuthorizationScheme = request.Headers.Authorization?.Scheme;
        AuthorizationParameter = request.Headers.Authorization?.Parameter;

        RequestContent = request.Content == null
            ? null
            : await request.Content.ReadAsStringAsync();

        if (_responseFactory != null)
        {
            return await _responseFactory(request, cancellationToken);
        }

        return Response ?? throw new InvalidOperationException(
            "No HTTP response or response factory has been configured.");
    }
}
