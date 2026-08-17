using FitProgress.Application.IService.Users;
using FitProgress.Application.Services.Users;
using Microsoft.Extensions.DependencyInjection;

namespace FitProgress.Application.DependencyInjection;

public static class ApplicationDependencyInjection
{
    public static IServiceCollection AddApplicationDependencies(
        this IServiceCollection services)
    {
        services.AddScoped<IUserService, UserService>();

        return services;
    }
}
