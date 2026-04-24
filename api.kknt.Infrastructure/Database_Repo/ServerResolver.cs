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

            var servers = await QueryServerMappingWithFallbackAsync("CheckIPServerWithReport");

            return await TaxServiceHelper.ScanTaxServerAsync(taxCode, servers);
        }

        /// <summary>
        /// Truy vấn sp_GetServerMapping: thử DemoDb trước, nếu fail thì fallback sang MasterDb.
        /// Force TCP protocol để tránh Named Pipes timeout.
        /// </summary>
        private async Task<IEnumerable<TaxServerMapping>> QueryServerMappingWithFallbackAsync(string caller)
        {
            // 1. Thử DemoDb trước
            try
            {
                _logger.LogInformation("[ServerResolver][{Caller}] Connecting to DemoDb (tcp)...", caller);
                using var demoConn = CreateConnection(_demoOpts.ConnectionString, _demoOpts.DefaultDatabaseName, connectTimeout: 5);
                var result = await demoConn.QueryAsync<TaxServerMapping>(
                    new CommandDefinition(
                        commandText: "sp_GetServerMapping",
                        commandType: System.Data.CommandType.StoredProcedure,
                        commandTimeout: 10));
                _logger.LogInformation("[ServerResolver][{Caller}] DemoDb OK", caller);
                return result;
            }
            catch (SqlException ex)
            {
                _logger.LogWarning(ex, "[ServerResolver][{Caller}] DemoDb FAILED (Error {ErrorNumber}), falling back to MasterDb...",
                    caller, ex.Number);
            }

            // 2. Fallback sang MasterDb
            _logger.LogInformation("[ServerResolver][{Caller}] Connecting to MasterDb (tcp)...", caller);
            using var masterConn = CreateConnection(_masterOpts.ConnectionString, _masterOpts.DefaultDatabaseName, connectTimeout: 5);
            var fallbackResult = await masterConn.QueryAsync<TaxServerMapping>(
                new CommandDefinition(
                    commandText: "sp_GetServerMapping",
                    commandType: System.Data.CommandType.StoredProcedure,
                    commandTimeout: 10));
            _logger.LogInformation("[ServerResolver][{Caller}] MasterDb OK (fallback)", caller);
            return fallbackResult;
        }

        /// <summary>
        /// Tạo SqlConnection với TCP forced, override ConnectTimeout, và Encrypt=true.
        /// </summary>
        private static SqlConnection CreateConnection(string baseConnectionString, string databaseName, int connectTimeout)
        {
            var builder = new SqlConnectionStringBuilder(baseConnectionString)
            {
                InitialCatalog = databaseName,
                ConnectTimeout = connectTimeout,
            };

            // Force TCP protocol — tránh Named Pipes fallback (tốn thêm ~20s nếu server unreachable)
            var dataSource = builder.DataSource;
            if (!dataSource.StartsWith("tcp:", StringComparison.OrdinalIgnoreCase))
            {
                builder.DataSource = $"tcp:{dataSource}";
            }

            return new SqlConnection(builder.ConnectionString);
        }

        private async Task<TaxServerMapping?> FetchMappingFromDbAsync(string taxCode, CancellationToken ct)
        {
            // 1. Thử DemoDb trước
            try
            {
                using var demoConn = CreateConnection(_demoOpts.ConnectionString, _demoOpts.DefaultDatabaseName, connectTimeout: 5);
                return await demoConn.QueryFirstOrDefaultAsync<TaxServerMapping>(
                    new CommandDefinition(
                        commandText: "sp_GetServerMapping",
                        commandType: System.Data.CommandType.StoredProcedure,
                        commandTimeout: 10,
                        cancellationToken: ct));
            }
            catch (SqlException ex)
            {
                _logger.LogWarning(ex, "[ServerResolver] DemoDb failed for FetchMapping({TaxCode}), trying MasterDb...", taxCode);
            }

            // 2. Fallback sang MasterDb
            try
            {
                using var masterConn = CreateConnection(_masterOpts.ConnectionString, _masterOpts.DefaultDatabaseName, connectTimeout: 10);
                return await masterConn.QueryFirstOrDefaultAsync<TaxServerMapping>(
                    new CommandDefinition(
                        commandText: "sp_GetServerMapping",
                        commandType: System.Data.CommandType.StoredProcedure,
                        commandTimeout: 10,
                        cancellationToken: ct));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ServerResolver] BOTH DemoDb AND MasterDb failed for {TaxCode}", taxCode);
                throw;
            }
        }
    }
}
