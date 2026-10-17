using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using System;

namespace Krizovka.Shared.AspNetCore;

public static class LoggingSetupExtensions
{
    /// <summary>
    /// Configures structured logging via Serilog for the entire service.
    /// This should be called early in Program.cs using builder.Host.UseMestoPlusLogging(serviceName).
    /// </summary>
    public static IHostBuilder UseMestoPlusLogging(this IHostBuilder hostBuilder, string serviceName)
    {
        return hostBuilder.UseSerilog((context, services, configuration) =>
        {
            configuration
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", serviceName)
                .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
                .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{Application}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.File($"logs/{serviceName.ToLower()}-.log", rollingInterval: RollingInterval.Day,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] [{Application}] [{Environment}] {Message:lj}{NewLine}{Exception}")
                .ReadFrom.Configuration(context.Configuration);
        });
    }

    /// <summary>
    /// Adds HTTP request logging to the request pipeline.
    /// Should be called early in the middleware pipeline (e.g. just after UseMestoPlusErrorHandling).
    /// </summary>
    public static IApplicationBuilder UseMestoPlusRequestLogging(this IApplicationBuilder app)
    {
        return app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";

            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value ?? string.Empty);
                diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme ?? string.Empty);
            };
        });
    }
}
