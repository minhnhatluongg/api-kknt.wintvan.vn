using api.kknt.Domain.Interfaces;
using api.kknt.Domain.Interfaces.DatabaseConfig;
using Dapper;
using Microsoft.Extensions.Logging;
using System.Data;

namespace api.kknt.Infrastructure.Repositories
{
    /// <summary>
    /// Đọc/ghi cache MST → ServerHost trên Master DB (BosEVATbizzi..tblLoginServerCache).
    /// Inline SQL, không dùng stored procedure.
    /// </summary>
    public sealed class LoginCacheRepository : ILoginCacheRepository
    {
        private readonly IDbConnectionFactory _dbFactory;
        private readonly ILogger<LoginCacheRepository> _logger;

        public LoginCacheRepository(
            IDbConnectionFactory dbFactory,
            ILogger<LoginCacheRepository> logger)
        {
            _dbFactory = dbFactory;
            _logger = logger;
        }

        public async Task<string?> GetCachedServerAsync(string taxCode, CancellationToken ct = default)
        {
           using var conn = await _dbFactory.CreateDefault_108_Async(ct: ct);

            const string sql = @"
                SELECT ServerHost
                  FROM dbo.tblLoginServerCache WITH (NOLOCK)
                 WHERE MST = @TaxCode";

            try
            {
                var serverHost = await conn.QueryFirstOrDefaultAsync<string>(
                    new CommandDefinition(
                        commandText: sql,
                        parameters: new { TaxCode = taxCode },
                        commandType: CommandType.Text,
                        cancellationToken: ct));

                if (serverHost != null)
                    _logger.LogInformation("[LoginCache] HIT MST={MST} → {Server}", taxCode, serverHost);
                else
                    _logger.LogInformation("[LoginCache] MISS MST={MST}", taxCode);

                return serverHost;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[LoginCache] GetCachedServer FAIL MST={MST}", taxCode);
                return null; // cache fail = fallback to scan, không block login
            }
        }

        public async Task UpsertAsync(string taxCode, string serverHost, CancellationToken ct = default)
        {
            await using var conn = await _dbFactory.CreateDefault_108_Async(ct: ct);

            const string sql = @"
                MERGE dbo.tblLoginServerCache AS target
                USING (SELECT @TaxCode AS MST, @ServerHost AS ServerHost) AS source
                   ON target.MST = source.MST
                 WHEN MATCHED THEN
                      UPDATE SET ServerHost  = source.ServerHost,
                                 LastLoginAt = SYSUTCDATETIME()
                 WHEN NOT MATCHED THEN
                      INSERT (MST, ServerHost, CachedAt, LastLoginAt)
                      VALUES (source.MST, source.ServerHost, SYSUTCDATETIME(), SYSUTCDATETIME());";

            try
            {
                await conn.ExecuteAsync(
                    new CommandDefinition(
                        commandText: sql,
                        parameters: new { TaxCode = taxCode, ServerHost = serverHost },
                        commandType: CommandType.Text,
                        cancellationToken: ct));

                _logger.LogInformation(
                    "[LoginCache] UPSERT MST={MST} → {Server}", taxCode, serverHost);
            }
            catch (Exception ex)
            {
                // Cache write fail = không block login, chỉ log warning
                _logger.LogWarning(ex,
                    "[LoginCache] Upsert FAIL MST={MST} Server={Server}", taxCode, serverHost);
            }
        }
    }
}
