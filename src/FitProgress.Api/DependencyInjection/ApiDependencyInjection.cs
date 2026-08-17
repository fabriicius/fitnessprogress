using FitProgress.Api.Middleware;

namespace FitProgress.Api.DependencyInjection;

public static class ApiDependencyInjection
{
    public static IServiceCollection AddApiDependencies(
        this IServiceCollection services)
    {
        services.AddControllers();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        return services;
    }
}
