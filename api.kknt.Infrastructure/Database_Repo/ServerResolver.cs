using api.kknt.Domain.Entities;
using api.kknt.Domain.Interfaces.DatabaseConfig;
using Dapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace api.kknt.Infrastructure.Database
{
    public class ServerResolver : IServerResolver
    {
        private readonly MasterDbOptions _masterOpts;
        private readonly IEncryptionService _encryption;
        private readonly ILogger<ServerResolver> _logger;

        private readonly ConcurrentDictionary<string, (TaxServerMapping? Value, DateTime Expiry)> _cache = new();
        private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(30);

        public ServerResolver(IOptions<MasterDbOptions> opts, IEncryptionService encryption, ILogger<ServerResolver> logger)
        {
            _masterOpts = opts.Value;
            _encryption = encryption;
            _logger = logger;
        }
        public void InvalidateCache(string taxCode)
        {
            if (_cache.TryRemove(taxCode, out _))
            {
                _logger.LogInformation("[ServerResolver] Cache EVICTED for {TaxCode}", taxCode);
            }
        }

        public async Task<TaxServerMapping?> ResolveAsync(string taxCode, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(taxCode))
                return null;
            //1. Check cache first 
            if (_cache.TryGetValue(taxCode, out var cached) && cached.Expiry > DateTime.UtcNow)
            {
                _logger.LogDebug("[ServerResolver] Cache HIT for {TaxCode}", taxCode);
                return cached.Value;
            }
            //2. Cache miss or expired, fetch from DB
            _logger.LogInformation("[ServerResolver] Cache MISS – querying Master DB for {TaxCode}", taxCode);
            TaxServerMapping? result = null;
            try
            {
                var builder = new SqlConnectionStringBuilder(_masterOpts.ConnectionString)
                {
                    InitialCatalog = _masterOpts.DefaultDatabaseName // -> Lấy databaseName, flexible hơn là hardcode vào connection string
                };

                using var conn = new SqlConnection(builder.ConnectionString);
                result = await conn.QuerySingleOrDefaultAsync<TaxServerMapping>(
                    new CommandDefinition(
                        commandText: "sp_GetServerMapping",
                        parameters: new { TaxCode = taxCode },
                        commandType: System.Data.CommandType.StoredProcedure,
                        cancellationToken: ct));
                if (result != null && !string.IsNullOrEmpty(result.Password))
                {
                    result.Password = _encryption.Decrypt(result.Password);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ServerResolver] Failed to query Master DB for {TaxCode}", taxCode);
                throw;
            }

            _cache[taxCode] = (result, DateTime.UtcNow.Add(CacheTtl));
            return result;
        }
    }
}
