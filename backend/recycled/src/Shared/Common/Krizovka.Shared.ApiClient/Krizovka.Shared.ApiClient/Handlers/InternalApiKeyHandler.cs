using Microsoft.Extensions.Configuration;

namespace Krizovka.Shared.ApiClient.Handlers;

/// <summary>
/// A DelegatingHandler that injects the internal M2M X-Api-Key into outgoing requests.
/// Used for secure internal communication between Krizovka microservices.
/// </summary>
public class InternalApiKeyHandler : DelegatingHandler
{
    private readonly string _apiKey;

    public InternalApiKeyHandler(IConfiguration configuration)
    {
        _apiKey = configuration["INTERNAL_API_KEY"]
            ?? configuration["InternalApiKey"]
            ?? Environment.GetEnvironmentVariable("INTERNAL_API_KEY")
            ?? throw new InvalidOperationException(
                "INTERNAL_API_KEY is not configured. Internal S2S calls must fail closed until the key is provided.");

        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            throw new InvalidOperationException(
                "INTERNAL_API_KEY is empty. Internal S2S calls must fail closed until the key is provided.");
        }
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.TryAddWithoutValidation("X-Api-Key", _apiKey);
        return await base.SendAsync(request, cancellationToken);
    }
}
