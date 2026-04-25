namespace api.kknt.Domain.Entities.Dashboard
{
    /// <summary>
    /// Cảnh báo hóa đơn (mua vào / bán ra).
    /// </summary>
    public class AnalyInvoice
    {
        public int TChat { get; set; }
        public string TChat_Title { get; set; } = string.Empty;
        public int Slg_RRVT { get; set; }
    }

    /// <summary>
    /// Cảnh báo KH / NCC.
    /// </summary>
    public class CustomerWarning
    {
        public int KhoanMuc_RRVT { get; set; }
        public string KhoanMuc_Title { get; set; } = string.Empty;
        public int Slg_RRVT { get; set; }
    }
}
