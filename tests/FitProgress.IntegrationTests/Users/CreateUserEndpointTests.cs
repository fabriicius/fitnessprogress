using System.Net;
using System.Net.Http.Json;
using FitProgress.Domain.Contracts.V1.Users.Requests;
using FitProgress.Domain.Contracts.V1.Users.Responses;
using Microsoft.AspNetCore.Http;
using Npgsql;

namespace FitProgress.IntegrationTests.Users;

public sealed class CreateUserEndpointTests(PostgresApiFactory factory) : IClassFixture<PostgresApiFactory>
{
    private readonly PostgresApiFactory _factory = factory;

    [Fact]
    public async Task PostUsers_DadosValidos_DeveRetornar201EPersistirSenhaComoHash()
    {
        var client = _factory.CreateClient();
        var request = new CreateUserRequest("Maria Silva", "maria.valida@example.com", "Senha123");

        var response = await client.PostAsJsonAsync("/api/v1/users", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.NotNull(body);
        Assert.Equal("Maria Silva", body!.Name);
        Assert.Equal("maria.valida@example.com", body.Email);

        var storedPasswordHash = await GetStoredPasswordHashAsync(body.Email);
        Assert.NotNull(storedPasswordHash);
        Assert.NotEqual("Senha123", storedPasswordHash);
    }

    [Fact]
    public async Task PostUsers_NomeEmailESenhaInvalidosSimultaneamente_DeveRetornar400ComTodasAsViolacoes()
    {
        var client = _factory.CreateClient();
        var request = new CreateUserRequest(string.Empty, "nao-e-email", "123");

        var response = await client.PostAsJsonAsync("/api/v1/users", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains("name", problem!.Errors.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("email", problem.Errors.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("password", problem.Errors.Keys, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PostUsers_EmailDuplicadoComCapitalizacaoDiferente_DeveRetornar409NaSegundaTentativa()
    {
        var client = _factory.CreateClient();

        var primeiraTentativa = await client.PostAsJsonAsync(
            "/api/v1/users",
            new CreateUserRequest("Maria Silva", "duplicado@example.com", "Senha123"));
        Assert.Equal(HttpStatusCode.Created, primeiraTentativa.StatusCode);

        var segundaTentativa = await client.PostAsJsonAsync(
            "/api/v1/users",
            new CreateUserRequest("Outra Pessoa", "Duplicado@Example.com", "OutraSenha1"));

        Assert.Equal(HttpStatusCode.Conflict, segundaTentativa.StatusCode);
    }

    private async Task<string?> GetStoredPasswordHashAsync(string email)
    {
        await using var connection = new NpgsqlConnection(_factory.ConnectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(
            "SELECT password_hash FROM users WHERE email = @Email",
            connection);
        command.Parameters.AddWithValue("Email", email);

        var result = await command.ExecuteScalarAsync();
        return result as string;
    }
}
