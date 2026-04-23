using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace api.kknt.Domain.Interfaces.DatabaseConfig
{
    public interface IDbConnectionFactory
    {
        Task<SqlConnection> CreateAsync(string taxCode,
                                        CancellationToken ct = default);
        Task<SqlConnection> CreateDefault_108_Async(string? dbName =null ,CancellationToken ct = default);
        Task<SqlConnection> CreateDefault_ERP_Async(string? dbName =null ,CancellationToken ct = default);
        Task<SqlConnection> CreateDefault_Demo_Async(string? dbName =null ,CancellationToken ct = default);
        Task<SqlConnection> CreateDynamicConnection(string serverHost, string? database = null, CancellationToken ct = default);
    }
}
