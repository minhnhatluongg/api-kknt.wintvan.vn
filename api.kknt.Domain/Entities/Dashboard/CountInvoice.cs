namespace api.kknt.Domain.Entities.Dashboard
{
    /// <summary>
    /// Số lượng hóa đơn mua vào / bán ra / khách hàng.
    /// </summary>
    public class CountInvoice
    {
        public int CountHDDV { get; set; }
        public int CountHDDR { get; set; }
        public int CountCus { get; set; }
    }
}
