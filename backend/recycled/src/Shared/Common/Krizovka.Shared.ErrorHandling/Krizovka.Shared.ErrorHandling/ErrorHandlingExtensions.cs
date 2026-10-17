using Krizovka.Shared.ErrorHandling.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Krizovka.Shared.ErrorHandling;

public static class ErrorHandlingExtensions
{
    public static IServiceCollection AddMestoPlusErrorHandling(this IServiceCollection services)
    {
        // Registers MediatR Validation pipeline behavior
        services.AddTransient(typeof(MediatR.IPipelineBehavior<,>), typeof(Krizovka.Shared.ErrorHandling.Behaviors.ValidationBehavior<,>));

        return services;
    }

    public static IApplicationBuilder UseMestoPlusErrorHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
    }
}
