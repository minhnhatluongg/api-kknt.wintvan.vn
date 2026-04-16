using api.kknt.Domain.Interfaces.DatabaseConfig;
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

        public DbConnectionFactory(
            IServerResolver serverResolver,
            IOptions<MasterDbOptions> masterOpts)
        {
            _serverResolver = serverResolver;
            _masterOpts = masterOpts.Value;
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
                DataSource = mapping.ServerHost,
                InitialCatalog = mapping.Catalog,
                UserID = mapping.User,
                Password = mapping.Password,
                TrustServerCertificate = true,
                ConnectTimeout = 30
            };
            var connection =  new SqlConnection(builder.ConnectionString);
            return connection;
        }

        //Lấy thẳng con server 234. Default là Server 234 - Database Name BosEVATbizzi -> Có thể config lại.
        public async Task<SqlConnection> CreateMasterAsync(string? dbName = null, CancellationToken ct = default)
        {
            var builder = new SqlConnectionStringBuilder(_masterOpts.ConnectionString);
            builder.InitialCatalog = dbName ?? _masterOpts.DefaultDatabaseName ?? "BosEVATbizzi";
            var connection = new SqlConnection(builder.ConnectionString);
            return connection;
        }
    }
}
