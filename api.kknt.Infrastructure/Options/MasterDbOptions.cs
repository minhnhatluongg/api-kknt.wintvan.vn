using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace api.kknt.Infrastructure.Database
{
    public sealed class MasterDbOptions
    {
        public const string Section = "MasterDb";
        public string ConnectionString { get; set; } = null!;
        public string DefaultDatabaseName { get; set; } = null!;
        public int CommandTimeout { get; set; } = 30;
    }
}
