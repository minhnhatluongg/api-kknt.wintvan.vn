using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace api.kknt.Application.DTOs.SolverServerDTOs
{
    public class WinInvoiceData
    {
        public string? Taxcode { get; set; }
        public string? ServerKey { get; set; } 
        public string? CmpnID { get; set; }
        public string? BosUserCode { get; set; }
    }
}
