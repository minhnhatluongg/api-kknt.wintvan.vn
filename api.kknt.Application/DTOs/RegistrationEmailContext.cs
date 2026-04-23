using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace api.kknt.Application.DTOs
{
    public class RegistrationEmailContext
    {
        // Khách hàng
        public string TaxCode { get; set; } = "";
        public string CompanyName { get; set; } = "";
        public string ContactName { get; set; } = "";
        public string CustomerEmail { get; set; } = "";
        public string CustomerPhone { get; set; } = "";
        public string CustomerAddress { get; set; } = "";

        // Hạ tầng cấp
        public string ServerHost { get; set; } = "";
        public bool IsNewServer { get; set; }

        // Sale phụ trách (optional)
        public string? SaleName { get; set; }
        public string? SaleEmail { get; set; }
        public string? SalePhone { get; set; }

        // Thời điểm đăng ký
        public DateTime RegisterAt { get; set; } = DateTime.Now;
    }
}
