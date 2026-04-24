using api.kknt.Application.InterfaceServices;
using api.kknt.Domain.Interfaces.DatabaseConfig;
using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static api.kknt.Domain.Entities.StoredTokenEntity;

namespace api.kknt.Infrastructure.Repositories
{
    public class DbRefreshTokenStore : IRefreshTokenStore
    {
        private readonly IDbConnectionFactory _db;
        private const string BosEVATbizzi = "BosEVATbizzi";

        public DbRefreshTokenStore(IDbConnectionFactory db) => _db = db;

        public async Task<string> CreateAsync(string taxCode, DateTime expiresAt, CancellationToken ct)
        {
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

            using var conn = await _db.CreateDefault_Demo_Async(BosEVATbizzi, ct);
            await conn.ExecuteAsync(
                "INSERT INTO RefreshTokens (Token, TaxCode, ExpiresAt, IsRevoked) VALUES (@token, @taxCode, @expiresAt, 0)",
                new { token, taxCode, expiresAt });

            return token;
        }

        public async Task<StoredToken?> GetAsync(string token, CancellationToken ct)
        {
            using var conn = await _db.CreateDefault_Demo_Async(BosEVATbizzi,ct);
            return await conn.QuerySingleOrDefaultAsync<StoredToken>(
                "SELECT TaxCode, ExpiresAt FROM RefreshTokens WHERE Token = @token AND IsRevoked = 0",
                new { token });
        }

        public async Task RevokeAsync(string token, CancellationToken ct)
        {
            using var conn = await _db.CreateDefault_Demo_Async(BosEVATbizzi,ct);
            await conn.ExecuteAsync(
                "UPDATE RefreshTokens SET IsRevoked = 1 WHERE Token = @token",
                new { token });
        }

        public async Task<int> DeleteExpiredAsync(CancellationToken ct)
        {
            using var conn = await _db.CreateDefault_Demo_Async(BosEVATbizzi, ct);

            // Xoá theo batch (TOP 5000) tránh lock table quá lâu.
            // Xoá token: hết hạn HOẶC đã revoke.
            const string sql = @"
                DELETE TOP (5000) FROM RefreshTokens
                 WHERE ExpiresAt < GETUTCDATE()
                    OR IsRevoked = 1";

            int totalDeleted = 0;
            int batchDeleted;

            do
            {
                batchDeleted = await conn.ExecuteAsync(
                    new CommandDefinition(sql, cancellationToken: ct));
                totalDeleted += batchDeleted;
            }
            while (batchDeleted == 5000 && !ct.IsCancellationRequested);

            return totalDeleted;
        }
    }
}
