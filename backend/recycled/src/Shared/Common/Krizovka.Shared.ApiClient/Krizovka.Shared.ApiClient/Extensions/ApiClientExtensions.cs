using Krizovka.Shared.ApiClient.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Refit;

namespace Krizovka.Shared.ApiClient.Extensions;

public static class ApiClientExtensions
{
    public static IServiceCollection AddKosApiClients(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = new ApiClientOptions();
        configuration.Bind("ApiClients", options);

        // Override with environment variables if present
        options.SrsUrl = Environment.GetEnvironmentVariable("SRS_URL") ?? options.SrsUrl;
        options.CpsUrl = Environment.GetEnvironmentVariable("CPS_URL") ?? options.CpsUrl;
        options.TsUrl = Environment.GetEnvironmentVariable("TS_URL") ?? options.TsUrl;
        options.PcsUrl = Environment.GetEnvironmentVariable("PCS_URL") ?? options.PcsUrl;
        options.PspUrl = Environment.GetEnvironmentVariable("PSP_URL") ?? options.PspUrl;
        options.NfsUrl = Environment.GetEnvironmentVariable("NFS_URL") ?? options.NfsUrl;
        options.TksUrl = Environment.GetEnvironmentVariable("TKS_URL") ?? options.TksUrl;
        options.AajUrl = Environment.GetEnvironmentVariable("AAJ_URL") ?? options.AajUrl;
        options.UsUrl = Environment.GetEnvironmentVariable("US_URL") ?? options.UsUrl;
        options.WvsUrl = Environment.GetEnvironmentVariable("WVS_URL") ?? options.WvsUrl;
        options.ApcUrl = Environment.GetEnvironmentVariable("APC_URL") ?? options.ApcUrl;
        options.DkmUrl = Environment.GetEnvironmentVariable("DKM_URL") ?? options.DkmUrl;
        options.AlsUrl = Environment.GetEnvironmentVariable("ALS_URL") ?? options.AlsUrl;
        options.PsUrl = Environment.GetEnvironmentVariable("PS_URL") ?? options.PsUrl;
        options.DsUrl = Environment.GetEnvironmentVariable("DS_URL") ?? options.DsUrl;

        services.AddSingleton(options);
        services.AddTransient<Krizovka.Shared.ApiClient.Handlers.InternalApiKeyHandler>();

        return services;
    }

    // Generic Refit registration helper
    public static IHttpClientBuilder AddRefitClient<T>(
        this IServiceCollection services,
        Func<ApiClientOptions, string> urlSelector)
        where T : class
    {
        return services.AddRefitClient<T>()
            .ConfigureHttpClient((sp, client) =>
            {
                var options = sp.GetRequiredService<ApiClientOptions>();
                client.BaseAddress = new Uri(urlSelector(options));
                client.Timeout = TimeSpan.FromSeconds(options.DefaultTimeoutSeconds);
            }).AddHttpMessageHandler<Krizovka.Shared.ApiClient.Handlers.InternalApiKeyHandler>();
    }

    // SRS Client
    public static IHttpClientBuilder AddSrsClient<TClient, TImplementation>(
        this IServiceCollection services)
        where TClient : class
        where TImplementation : class, TClient
    {
        return services.AddHttpClient<TClient, TImplementation>((sp, client) =>
        {
            var options = sp.GetRequiredService<ApiClientOptions>();
            client.BaseAddress = new Uri(options.SrsUrl);
            client.Timeout = TimeSpan.FromSeconds(options.DefaultTimeoutSeconds);
        });
    }

    public static IHttpClientBuilder AddSrsRefitClient<T>(this IServiceCollection services) where T : class
        => services.AddRefitClient<T>(opt => opt.SrsUrl);

    // CPS Client
    public static IHttpClientBuilder AddCpsClient<TClient, TImplementation>(
        this IServiceCollection services)
        where TClient : class
        where TImplementation : class, TClient
    {
        return services.AddHttpClient<TClient, TImplementation>((sp, client) =>
        {
            var options = sp.GetRequiredService<ApiClientOptions>();
            client.BaseAddress = new Uri(options.CpsUrl);
            client.Timeout = TimeSpan.FromSeconds(options.DefaultTimeoutSeconds);
        });
    }

    public static IHttpClientBuilder AddCpsRefitClient<T>(this IServiceCollection services) where T : class
        => services.AddRefitClient<T>(opt => opt.CpsUrl);

    // TS Client
    public static IHttpClientBuilder AddTsClient<TClient, TImplementation>(
        this IServiceCollection services)
        where TClient : class
        where TImplementation : class, TClient
    {
        return services.AddHttpClient<TClient, TImplementation>((sp, client) =>
        {
            var options = sp.GetRequiredService<ApiClientOptions>();
            client.BaseAddress = new Uri(options.TsUrl);
            client.Timeout = TimeSpan.FromSeconds(options.DefaultTimeoutSeconds);
        });
    }

    public static IHttpClientBuilder AddTsRefitClient<T>(this IServiceCollection services) where T : class
        => services.AddRefitClient<T>(opt => opt.TsUrl);

    // PCS Client
    public static IHttpClientBuilder AddPcsClient<TClient, TImplementation>(
        this IServiceCollection services)
        where TClient : class
        where TImplementation : class, TClient
    {
        return services.AddHttpClient<TClient, TImplementation>((sp, client) =>
        {
            var options = sp.GetRequiredService<ApiClientOptions>();
            client.BaseAddress = new Uri(options.PcsUrl);
            client.Timeout = TimeSpan.FromSeconds(options.DefaultTimeoutSeconds);
        });
    }

    public static IHttpClientBuilder AddPcsRefitClient<T>(this IServiceCollection services) where T : class
        => services.AddRefitClient<T>(opt => opt.PcsUrl);

    // PSP Client
    public static IHttpClientBuilder AddPspClient<TClient, TImplementation>(
        this IServiceCollection services)
        where TClient : class
        where TImplementation : class, TClient
    {
        return services.AddHttpClient<TClient, TImplementation>((sp, client) =>
        {
            var options = sp.GetRequiredService<ApiClientOptions>();
            client.BaseAddress = new Uri(options.PspUrl);
            client.Timeout = TimeSpan.FromSeconds(options.DefaultTimeoutSeconds);
        });
    }

    public static IHttpClientBuilder AddPspRefitClient<T>(this IServiceCollection services) where T : class
        => services.AddRefitClient<T>(opt => opt.PspUrl);

    // NFS Client
    public static IHttpClientBuilder AddNfsClient<TClient, TImplementation>(
        this IServiceCollection services)
        where TClient : class
        where TImplementation : class, TClient
    {
        return services.AddHttpClient<TClient, TImplementation>((sp, client) =>
        {
            var options = sp.GetRequiredService<ApiClientOptions>();
            client.BaseAddress = new Uri(options.NfsUrl);
            client.Timeout = TimeSpan.FromSeconds(options.DefaultTimeoutSeconds);
        });
    }

    public static IHttpClientBuilder AddNfsRefitClient<T>(this IServiceCollection services) where T : class
        => services.AddRefitClient<T>(opt => opt.NfsUrl);

    // TKS Client
    public static IHttpClientBuilder AddTksClient<TClient, TImplementation>(
        this IServiceCollection services)
        where TClient : class
        where TImplementation : class, TClient
    {
        return services.AddHttpClient<TClient, TImplementation>((sp, client) =>
        {
            var options = sp.GetRequiredService<ApiClientOptions>();
            client.BaseAddress = new Uri(options.TksUrl);
            client.Timeout = TimeSpan.FromSeconds(options.DefaultTimeoutSeconds);
        });
    }

    public static IHttpClientBuilder AddTksRefitClient<T>(this IServiceCollection services) where T : class
        => services.AddRefitClient<T>(opt => opt.TksUrl);

    // AAJ Client
    public static IHttpClientBuilder AddAajClient<TClient, TImplementation>(
        this IServiceCollection services)
        where TClient : class
        where TImplementation : class, TClient
    {
        return services.AddHttpClient<TClient, TImplementation>((sp, client) =>
        {
            var options = sp.GetRequiredService<ApiClientOptions>();
            client.BaseAddress = new Uri(options.AajUrl);
            client.Timeout = TimeSpan.FromSeconds(options.DefaultTimeoutSeconds);
        });
    }

    public static IHttpClientBuilder AddAajRefitClient<T>(this IServiceCollection services) where T : class
        => services.AddRefitClient<T>(opt => opt.AajUrl);

    // US Client (User Service)
    public static IHttpClientBuilder AddUsClient<TClient, TImplementation>(
        this IServiceCollection services)
        where TClient : class
        where TImplementation : class, TClient
    {
        return services.AddHttpClient<TClient, TImplementation>((sp, client) =>
        {
            var options = sp.GetRequiredService<ApiClientOptions>();
            client.BaseAddress = new Uri(options.UsUrl);
            client.Timeout = TimeSpan.FromSeconds(options.DefaultTimeoutSeconds);
        });
    }

    public static IHttpClientBuilder AddUsRefitClient<T>(this IServiceCollection services) where T : class
        => services.AddRefitClient<T>(opt => opt.UsUrl);

    // APC Client
    public static IHttpClientBuilder AddApcRefitClient<T>(this IServiceCollection services) where T : class
        => services.AddRefitClient<T>(opt => opt.ApcUrl);

    // WVS Client
    public static IHttpClientBuilder AddWvsRefitClient<T>(this IServiceCollection services) where T : class
        => services.AddRefitClient<T>(opt => opt.WvsUrl);
}
