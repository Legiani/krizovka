using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Krizovka.Shared.AspNetCore;

public static class KosControllersExtensions
{
    public static IMvcBuilder AddKosControllers(this IServiceCollection services, string? routePrefix = null)
    {
        var builder = services.AddControllers(options =>
            {
                if (!string.IsNullOrEmpty(routePrefix))
                {
                    options.Conventions.Add(new RoutePrefixConvention(new Microsoft.AspNetCore.Mvc.RouteAttribute(routePrefix)));
                }
            })
            .AddJsonOptions(options =>
            {
                // Use camelCase (standard for web APIs)
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;

                // Common converters
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());

                // Performance settings
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            });

        // Configure Microsoft.AspNetCore.Http.Json.JsonOptions which is used by Microsoft.AspNetCore.OpenApi (.NET 9+)
        // to generate schema definitions for enums, since it relies on the minimal API options by default.
        services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.SerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
            options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        });

        services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var errors = context.ModelState
                    .Where(e => e.Value != null && e.Value.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
                    );

                var traceId = System.Diagnostics.Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;

                var problemDetails = new Krizovka.Shared.ErrorHandling.Models.ErrorResponse
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Title = "Validation Failed",
                    Status = Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest,
                    Detail = "One or more validation errors occurred.",
                    Instance = context.HttpContext.Request.Path,
                    TraceId = traceId,
                    Errors = errors
                };

                return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(problemDetails)
                {
                    ContentTypes = { "application/problem+json", "application/json" }
                };
            };
        });

        return builder;
    }
}
