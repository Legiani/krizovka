using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Polly;
using Polly.Extensions.Http;
using System;
using System.Net.Http;

namespace Krizovka.Shared.ApiClient.Extensions;

public static class ServiceHttpClientExtensions
{
    public static IHttpClientBuilder AddServiceHttpClient(
        this IServiceCollection services,
        string name,
        Action<HttpClient> configureClient)
    {
        services.TryAddTransient<Krizovka.Shared.ApiClient.Handlers.InternalApiKeyHandler>();
        return services.AddHttpClient(name, configureClient)
            .AddHttpMessageHandler<Krizovka.Shared.ApiClient.Handlers.InternalApiKeyHandler>()
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy())
            .AddPolicyHandler(GetTimeoutPolicy());
    }

    public static IHttpClientBuilder AddServiceHttpClient<TClient, TImplementation>(
        this IServiceCollection services,
        Action<HttpClient> configureClient)
        where TClient : class
        where TImplementation : class, TClient
    {
        services.TryAddTransient<Krizovka.Shared.ApiClient.Handlers.InternalApiKeyHandler>();
        return services.AddHttpClient<TClient, TImplementation>(configureClient)
            .AddHttpMessageHandler<Krizovka.Shared.ApiClient.Handlers.InternalApiKeyHandler>()
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy())
            .AddPolicyHandler(GetTimeoutPolicy());
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .Or<Polly.Timeout.TimeoutRejectedException>() // Safely retry timeouts but with rapid decay
            .Or<System.Threading.Tasks.TaskCanceledException>()
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Min(2, retryAttempt))); // Max 2 sec backoff
    }

    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
    }

    private static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy()
    {
        // 5 seconds max per attempt. Prevents hanging threads across dependent microservices.
        return Policy.TimeoutAsync<HttpResponseMessage>(5);
    }
}
