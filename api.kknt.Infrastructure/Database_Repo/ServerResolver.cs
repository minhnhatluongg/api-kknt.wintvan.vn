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
using api.kknt.Infrastructure.AesEncryptionService;
using api.kknt.Domain.Common;
using api.kknt.Infrastructure.Options;

namespace api.kknt.Infrastructure.Database
{
    public class ServerResolver : IServerResolver
    {
        private readonly MasterDbOptions _masterOpts;
        private readonly DemoDbOptions _demoOpts;
        private readonly IEncryptionService _encryption;
        private readonly ILogger<ServerResolver> _logger;

        private readonly ConcurrentDictionary<string, (TaxServerMapping? Value, DateTime Expiry)> _cache = new();
        private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(30);

        public ServerResolver(
            IOptions<MasterDbOptions> masterOpts,
            IOptions<DemoDbOptions> demoOpts,
            IEncryptionService encryption,
            ILogger<ServerResolver> logger)
        {
            _masterOpts = masterOpts.Value;
            _demoOpts = demoOpts.Value;
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
            if (string.IsNullOrEmpty(taxCode)) return null;

            // 1. Kiểm tra Cache
            if (_cache.TryGetValue(taxCode, out var cached) && cached.Expiry > DateTime.UtcNow)
            {
                _logger.LogDebug("[ServerResolver] Cache HIT for {TaxCode}", taxCode);
                return cached.Value;
            }

            // 2. Cache miss -> Truy vấn trực tiếp bằng SqlConnection
            _logger.LogInformation("[ServerResolver] Cache MISS – querying Master DB for {TaxCode}", taxCode);

            var result = await FetchMappingFromDbAsync(taxCode, ct);

            if (result != null && !string.IsNullOrEmpty(result.Password))
            {
                result.Password = _encryption.Decrypt(result.Password);
            }

            _cache[taxCode] = (result, DateTime.UtcNow.Add(CacheTtl));
            return result;
        }

        public async Task<ServerScanResult> CheckIPServerWithReport(string taxCode)
        {
            if (string.IsNullOrEmpty(taxCode))
                throw new ArgumentNullException(nameof(taxCode), "MST không được để trống");

            taxCode = taxCode.Replace(" ", "");

            // Thay vì dùng Factory, chúng ta tự tạo connection tới Master DB
            using var conn = CreateDemoConnection();

            var servers = await conn.QueryAsync<TaxServerMapping>(
                new CommandDefinition(
                    commandText: "sp_GetServerMapping",
                    commandType: System.Data.CommandType.StoredProcedure));

            return await TaxServiceHelper.ScanTaxServerAsync(taxCode, servers);
        }

        // Hàm bổ trợ để tạo Connection tới Demo DB dựa trên Options
        private SqlConnection CreateDemoConnection()
        {
            var builder = new SqlConnectionStringBuilder(_demoOpts.ConnectionString)
            {
                InitialCatalog = _demoOpts.DefaultDatabaseName
            };
            return new SqlConnection(builder.ConnectionString);
        }

        private async Task<TaxServerMapping?> FetchMappingFromDbAsync(string taxCode, CancellationToken ct)
        {
            try
            {
                using var conn = CreateDemoConnection();
                return await conn.QueryFirstOrDefaultAsync<TaxServerMapping>(
                    new CommandDefinition(
                        commandText: "sp_GetServerMapping",
                        commandType: System.Data.CommandType.StoredProcedure,
                        cancellationToken: ct));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ServerResolver] Failed to query Master DB for {TaxCode}", taxCode);
                throw;
            }
        }
    }
}
