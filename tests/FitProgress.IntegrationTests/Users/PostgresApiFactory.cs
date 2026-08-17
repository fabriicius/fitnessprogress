using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

namespace FitProgress.IntegrationTests.Users;

public sealed class PostgresApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    async Task IAsyncLifetime.InitializeAsync()
    {
        await _container.StartAsync();

        Environment.SetEnvironmentVariable(
            "ConnectionStrings__Postgres",
            _container.GetConnectionString());
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _container.DisposeAsync();
        await base.DisposeAsync();
    }
}
