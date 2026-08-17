using System.Data.Common;
using Npgsql;

namespace FitProgress.Infrastructure.Database;

public sealed class ConnectionFactory(NpgsqlDataSource dataSource)
{
    private readonly NpgsqlDataSource _dataSource = dataSource;

    public async Task<DbConnection> CreateOpenConnectionAsync(
        CancellationToken cancellationToken)
    {
        return await _dataSource.OpenConnectionAsync(cancellationToken);
    }
}
