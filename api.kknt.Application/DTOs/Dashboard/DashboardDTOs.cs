using api.kknt.Domain.Entities.Dashboard;

namespace api.kknt.Application.DTOs.Dashboard
{
    /// <summary>
    /// Response cho GET /api/v1/dashboard
    /// </summary>
    public class DashboardResponse
    {
        // ── Token / CKS
        public DataToken TokenInfo { get; set; } = new();

        // ── Sản phẩm
        public ProductGroup Product { get; set; } = new();

        // ── Đếm hóa đơn
        public CountInvoice Countinv { get; set; } = new();

        // ── Báo cáo thuế
        public ReportTax ReportTax { get; set; } = new();

        // ── Cảnh báo hóa đơn
        public List<AnalyInvoice> AnalyInvoices_Main { get; set; } = new();
        public List<AnalyInvoice> AnalyInvoices_Main_OUT { get; set; } = new();

        // ── Cảnh báo KH/NCC
        public List<CustomerWarning> LstCustomer { get; set; } = new();

        // ── Biểu đồ
        public List<string> LabelHDDR { get; set; } = new();
        public List<decimal> DataHDDR { get; set; } = new();
        public List<string> LabelHDDV { get; set; } = new();
        public List<decimal> DataHDDV { get; set; } = new();

        // ── Đơn hàng
        public List<OrderItem> LstOrder { get; set; } = new();

        // ── Tin tức
        public ListNews ListNews { get; set; } = new();

        // ── Thống kê khác
        public int CountCompany { get; set; }
        public int CheckMSTLogin { get; set; }
        public int CheckHetHan { get; set; }
        public string MessageHoaDon { get; set; } = string.Empty;
        public string MessageNgaySuDung { get; set; } = string.Empty;
        public string MessageHetHan { get; set; } = string.Empty;
        public string DateEnd { get; set; } = string.Empty;
        public bool IsEnd { get; set; }
        public int DayRS { get; set; }

        // ── KH thay đổi
        public int Count_KHChange { get; set; }

        // ── Field list
        public List<FieldName> Listfield { get; set; } = new();

        // ── MST gốc
        public string CmpnTax { get; set; } = string.Empty;
    }

    /// <summary>
    /// Response cho GET /api/v1/dashboard/total-invoice-money
    /// </summary>
    public class TotalInvoiceMoneyResponse
    {
        public string Sum_TgtcthueHDDV { get; set; } = "0";
        public string Sum_TgtthueHDDV { get; set; } = "0";
        public string Sum_TgtttbsoHDDV { get; set; } = "0";
        public string Sum_TgtcthueHDDR { get; set; } = "0";
        public string Sum_TgtthueHDDR { get; set; } = "0";
        public string Sum_TgtttbsoHDDR { get; set; } = "0";
    }

    /// <summary>
    /// Response cho GET /api/v1/dashboard/pie-chart
    /// </summary>
    public class PieChartResponse
    {
        public List<string> LabelHDDR { get; set; } = new();
        public List<decimal> DataHDDR { get; set; } = new();
        public List<string> LabelHDDV { get; set; } = new();
        public List<decimal> DataHDDV { get; set; } = new();
    }

    /// <summary>
    /// Response cho GET /api/v1/dashboard/company-info (khi chọn công ty)
    /// </summary>
    public class CompanyInfoResponse
    {
        public DataToken TokenInfo { get; set; } = new();
        public List<AnalyInvoice> AnalyInvoices_Main { get; set; } = new();
        public List<AnalyInvoice> AnalyInvoices_Main_OUT { get; set; } = new();
        public List<CustomerWarning> LstCustomer { get; set; } = new();
        public int Count_KHChange { get; set; }
        public List<string> LabelHDDR { get; set; } = new();
        public List<decimal> DataHDDR { get; set; } = new();
        public List<string> LabelHDDV { get; set; } = new();
        public List<decimal> DataHDDV { get; set; } = new();
    }

    /// <summary>
    /// Response cho GET /api/v1/dashboard/choose-date
    /// </summary>
    public class ChooseDateResponse
    {
        public string DateFrom { get; set; } = string.Empty;
        public string DateTo { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request cho POST /api/v1/dashboard/add-cart
    /// </summary>
    public class AddCartRequestDto
    {
        public string ItemID { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public decimal Cur_Price { get; set; }
        public string? ItemUnitName { get; set; }
    }

    /// <summary>
    /// Response cho POST /api/v1/dashboard/add-cart
    /// </summary>
    public class AddCartResponseDto
    {
        public string Status { get; set; } = string.Empty;
    }
}
