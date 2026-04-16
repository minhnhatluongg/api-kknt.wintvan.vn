using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace api.kknt.Application.DTOs.SolverServerDTOs
{
    public class WinInvoiceAuthResponse
    {
        public bool IsSuccess { get; set; }
        public WinInvoiceData? Data { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
