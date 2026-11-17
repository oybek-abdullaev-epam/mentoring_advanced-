using Dapper;
using JobScheduler.Dal.Connections;

namespace JobScheduler.Dal.IntegrationTests;

// Direct reads on the primary, so assertions on stored rows never depend on replica lag or on the DAL under test.
public static class PrimaryDb
{
    public static async Task<IReadOnlyList<T>> QueryAsync<TDb, T>(ISqlConnectionFactory<TDb> factory, string sql, object param, CancellationToken ct)
        where TDb : IDatabaseMarker
    {
        await using var connection = await factory.OpenAsync(DbRole.ReadWrite, ct);
        return (await connection.QueryAsync<T>(new CommandDefinition(sql, param, cancellationToken: ct))).AsList();
    }

    public static async Task<T> ScalarAsync<TDb, T>(ISqlConnectionFactory<TDb> factory, string sql, object param, CancellationToken ct)
        where TDb : IDatabaseMarker
    {
        await using var connection = await factory.OpenAsync(DbRole.ReadWrite, ct);
        return await connection.ExecuteScalarAsync<T>(new CommandDefinition(sql, param, cancellationToken: ct)) ?? throw new InvalidOperationException("Query returned NULL.");
    }
}
