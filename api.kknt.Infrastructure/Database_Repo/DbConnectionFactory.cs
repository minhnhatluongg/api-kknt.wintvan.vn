using api.kknt.Application.Options;
using api.kknt.Domain.Interfaces.DatabaseConfig;
using api.kknt.Infrastructure.Options;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace api.kknt.Infrastructure.Database
{
    public class DbConnectionFactory : IDbConnectionFactory
    {
        private readonly IServerResolver _serverResolver;
        private readonly MasterDbOptions _masterOpts;
        private readonly DefaultWinInvoiceServerOptions _defaultServer;
        private readonly DemoDbOptions _defaultDemo;
        private readonly ErpDbOptions _erpDbOptions;

        /// <summary>Connect timeout mặc định cho tất cả connection (giây).</summary>
        private const int DefaultConnectTimeout = 10;

        public DbConnectionFactory(
            IServerResolver serverResolver,
            IOptions<MasterDbOptions> masterOpts,
            IOptions<DefaultWinInvoiceServerOptions> defaultServer,
            IOptions<ErpDbOptions> erpDbOptions,
            IOptions<DemoDbOptions> defaultDemo)
        {
            _serverResolver = serverResolver;
            _masterOpts = masterOpts.Value;
            _defaultServer = defaultServer.Value;
            _defaultDemo = defaultDemo.Value;
            _erpDbOptions = erpDbOptions.Value;
        }
        public async Task<SqlConnection> CreateAsync(string taxCode, CancellationToken ct = default)
        {
            var mapping = await _serverResolver.ResolveAsync(taxCode, ct);
            if(mapping == null)
            {
                throw new Exception($"No database mapping found for tax code: {taxCode}");
            }

            //Build connection string
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = ForceTcp(mapping.ServerHost),
                InitialCatalog = mapping.Catalog,
                UserID = mapping.User,
                Password = mapping.Password,
                TrustServerCertificate = true,
                ConnectTimeout = DefaultConnectTimeout
            };
            var connection =  new SqlConnection(builder.ConnectionString);
            await connection.OpenAsync(ct);
            return connection;
        }

        //Connection 108 -> Catatlog BosEVATbizzi
        public async Task<SqlConnection> CreateDefault_108_Async(string? dbName = null, CancellationToken ct = default)
        {
            var builder = new SqlConnectionStringBuilder(_masterOpts.ConnectionString);
            builder.InitialCatalog = dbName ?? _masterOpts.DefaultDatabaseName ?? "BosEVATbizzi";
            builder.ConnectTimeout = DefaultConnectTimeout;
            builder.DataSource = ForceTcp(builder.DataSource);
            var connection = new SqlConnection(builder.ConnectionString);
            await connection.OpenAsync(ct);
            return connection;
        }

        //Connection Demo -> Catatlog BosEVATbizzi
        public async Task<SqlConnection> CreateDefault_Demo_Async(string? dbName = null, CancellationToken ct = default)
        {
            var builder = new SqlConnectionStringBuilder(_defaultDemo.ConnectionString);
            builder.InitialCatalog = dbName ?? _defaultDemo.DefaultDatabaseName ?? "BosEVATbizzi";
            builder.ConnectTimeout = DefaultConnectTimeout;
            builder.DataSource = ForceTcp(builder.DataSource);
            var connection = new SqlConnection(builder.ConnectionString);
            await connection.OpenAsync(ct);
            return connection;
        }

        //Connection Erp -> Catatlog BosOnline
        public async Task<SqlConnection> CreateDefault_ERP_Async(string? dbName = null, CancellationToken ct = default)
        {
            var builder = new SqlConnectionStringBuilder(_erpDbOptions.ConnectionString);
            builder.InitialCatalog = dbName ?? _erpDbOptions.DefaultDatabaseName ?? "BosOnline";
            builder.ConnectTimeout = DefaultConnectTimeout;
            builder.DataSource = ForceTcp(builder.DataSource);
            var connection = new SqlConnection(builder.ConnectionString);
            await connection.OpenAsync(ct);
            return connection;
        }

        //Build Connection động từ targetHost, Catalog 
        public async Task<SqlConnection> CreateDynamicConnection(string serverHost, string? database = null, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(serverHost))
                throw new ArgumentException("Thiếu serverHost", nameof(serverHost));

            var b = new SqlConnectionStringBuilder
            {
                DataSource = ForceTcp(serverHost),
                InitialCatalog = database ?? _defaultServer.Catalog ?? "BosEVATbizzi",
                UserID = _defaultServer.User,
                Password = _defaultServer.Password,
                TrustServerCertificate = true,
                Encrypt = _defaultServer.Encrypt,
                ConnectTimeout = DefaultConnectTimeout,
                MultipleActiveResultSets = true
            };

            var conn = new SqlConnection(b.ConnectionString);
            await conn.OpenAsync(ct);
            return conn;
        }

        /// <summary>
        /// Force TCP protocol để tránh Named Pipes fallback (tốn ~20s nếu server unreachable).
        /// </summary>
        private static string ForceTcp(string dataSource)
            => dataSource.StartsWith("tcp:", StringComparison.OrdinalIgnoreCase)
                ? dataSource
                : $"tcp:{dataSource}";
    }
}
