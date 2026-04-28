using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace api.kknt.Application.BackgroundServices
{
    public sealed class ExpireTrialResult
    {
        public int isSuccess { get; set; }
        public string? Message { get; set; }
        public int? AffectedRows { get; set; }
    }
}
