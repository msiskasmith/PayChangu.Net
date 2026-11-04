namespace PayChangu.NET.Tests.Fakes;

public sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpResponseMessage _response;

    public HttpMethod? RequestMethod { get; private set; }

    public Uri? RequestUri { get; private set; }

    public string? RequestContent { get; private set; }

    public string? AuthorizationScheme { get; private set; }

    public string? AuthorizationParameter { get; private set; }

    public FakeHttpMessageHandler(HttpResponseMessage response)
    {
        _response = response;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
    HttpRequestMessage request,
    CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        RequestMethod = request.Method;
        RequestUri = request.RequestUri;
        RequestContent = request.Content == null
            ? null
            : await request.Content.ReadAsStringAsync();

        AuthorizationScheme = request.Headers.Authorization?.Scheme;
        AuthorizationParameter = request.Headers.Authorization?.Parameter;

        return _response;
    }
}