using Dapper;
using FitProgress.Domain.Abstractions;
using FitProgress.Domain.Models.Users;
using FitProgress.Domain.ValueObjects;
using FitProgress.Infrastructure.Database;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace FitProgress.Infrastructure.Repositories.Users;

public sealed class UserRepository(
    ConnectionFactory connectionFactory,
    ILogger<UserRepository> logger) : IUserRepository
{
    private readonly ConnectionFactory _connectionFactory = connectionFactory;
    private readonly ILogger<UserRepository> _logger = logger;

    public async Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

            const string sql = """
                SELECT EXISTS (
                    SELECT 1 FROM users WHERE email = @Email
                );
                """;

            var command = new CommandDefinition(
                commandText: sql,
                parameters: new { Email = email.Value },
                cancellationToken: cancellationToken);

            return await connection.ExecuteScalarAsync<bool>(command);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao verificar existência de usuário por e-mail.");

            throw;
        }
    }

    public async Task<bool> AddAsync(User user, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            const string sql = """
                INSERT INTO users (id, name, email, password_hash, created_at)
                VALUES (@Id, @Name, @Email, @PasswordHash, @CreatedAt);
                """;

            var command = new CommandDefinition(
                commandText: sql,
                parameters: new
                {
                    user.Id,
                    Name = user.Name.Value,
                    Email = user.Email.Value,
                    PasswordHash = user.PasswordHash.Value,
                    user.CreatedAt,
                },
                transaction: transaction,
                cancellationToken: cancellationToken);

            await connection.ExecuteAsync(command);

            await transaction.CommitAsync(cancellationToken);

            return true;
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            await transaction.RollbackAsync(cancellationToken);

            _logger.LogWarning(
                ex,
                "Tentativa de cadastrar usuário com e-mail já existente.");

            return false;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);

            _logger.LogError(ex, "Erro ao persistir usuário.");

            throw;
        }
    }
}
