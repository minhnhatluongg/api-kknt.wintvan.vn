using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace api.kknt.Domain.Entities
{
    public class ServerScanResult
    {
        public List<TaxServerMapping> FoundServers { get; set; } = new List<TaxServerMapping>();
        public List<string> UnreachableServers { get; set; } = new List<string>();
        public bool IsFound => FoundServers.Any();
        public bool HasConflict => FoundServers.Count > 1;
        public List<string> ScanLogs { get; set; } = new List<string>();
    }
}
