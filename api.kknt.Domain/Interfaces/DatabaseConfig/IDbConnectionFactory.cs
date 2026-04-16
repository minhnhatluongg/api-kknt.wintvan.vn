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
        Task<SqlConnection> CreateMasterAsync(string? dbName =null ,CancellationToken ct = default);
    }
}
