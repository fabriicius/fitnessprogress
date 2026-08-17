using DbUp;
using FitProgress.Domain.Abstractions;
using FitProgress.Infrastructure.Database;
using FitProgress.Infrastructure.Repositories.Users;
using FitProgress.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FitProgress.Infrastructure.DependencyInjection;

public static class InfrastructureDependencyInjection
{
    public static IServiceCollection AddInfrastructureDependencies(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = GetConnectionString(configuration);

        services.AddNpgsqlDataSource(connectionString);
        services.AddScoped<ConnectionFactory>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();

        return services;
    }

    public static void ApplyDatabaseMigrations(IConfiguration configuration)
    {
        var connectionString = GetConnectionString(configuration);

        var upgrader = DeployChanges.To
            .PostgresqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(typeof(InfrastructureDependencyInjection).Assembly)
            .LogToConsole()
            .Build();

        var result = upgrader.PerformUpgrade();

        if (!result.Successful)
        {
            throw new InvalidOperationException(
                "Falha ao aplicar migrações de banco de dados.",
                result.Error);
        }
    }

    private static string GetConnectionString(IConfiguration configuration)
    {
        return configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException(
                "Connection string 'Postgres' não configurada.");
    }
}
