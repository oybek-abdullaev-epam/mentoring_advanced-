using Azure.Core;
using Microsoft.Data.SqlClient;

namespace JobScheduler.Dal.Connections;

public sealed class AzureSqlTokenProvider(TokenCredential credential) : IDisposable
{
    private static readonly string[] Scopes = ["https://database.windows.net/.default"];
    private static readonly TimeSpan RefreshSkew = TimeSpan.FromMinutes(5);

    private readonly SemaphoreSlim _gate = new(1, 1);
    private AccessToken _cached;

    public async Task<SqlAuthenticationToken> GetTokenAsync(SqlAuthenticationParameters parameters, CancellationToken ct)
    {
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_cached.Token is null || _cached.ExpiresOn - RefreshSkew <= DateTimeOffset.UtcNow)
            {
                _cached = await credential.GetTokenAsync(new TokenRequestContext(Scopes), ct).ConfigureAwait(false);
            }

            return new SqlAuthenticationToken(_cached.Token, _cached.ExpiresOn);
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose() => _gate.Dispose();
}
