using api.kknt.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace api.kknt.Domain.Interfaces.DatabaseConfig
{
    public interface IServerResolver
    {
        Task<TaxServerMapping?> ResolveAsync(string taxCode,
                                             CancellationToken ct = default);
        void InvalidateCache(string taxCode);

        Task<ServerScanResult> CheckIPServerWithReport(string taxCode);
    }
}
