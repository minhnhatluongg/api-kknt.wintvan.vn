using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace api.kknt.Application.DTOs
{
    public class CreateTrialOrderResult
    {
        public int isSuccess { get; set; }
        public string? Message { get; set; }
        public string? ErrorCode { get; set; }
        public string? ExistingOID { get; set; }
        public DateTime? ExistingExpiry { get; set; }
        public int? Status { get; set; }
        public string? StatusLabel { get; set; }
    }
}
