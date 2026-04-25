namespace api.kknt.Domain.Entities.Dashboard
{
    /// <summary>
    /// Tổng tiền hóa đơn (mua vào + bán ra).
    /// </summary>
    public class TotalInvoiceMoney
    {
        public string Sum_TgtcthueHDDV { get; set; } = "0";
        public string Sum_TgtthueHDDV { get; set; } = "0";
        public string Sum_TgtttbsoHDDV { get; set; } = "0";
        public string Sum_TgtcthueHDDR { get; set; } = "0";
        public string Sum_TgtthueHDDR { get; set; } = "0";
        public string Sum_TgtttbsoHDDR { get; set; } = "0";
    }

    /// <summary>
    /// Khoảng ngày (trả về từ ChooseDate).
    /// </summary>
    public class DateRange
    {
        public string DateFrom { get; set; } = string.Empty;
        public string DateTo { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request thêm giỏ hàng.
    /// </summary>
    public class AddCartRequest
    {
        public string ItemID { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public decimal Cur_Price { get; set; }
        public string? ItemUnitName { get; set; }
    }

    /// <summary>
    /// Kết quả thêm giỏ hàng.
    /// </summary>
    public class AddCartResult
    {
        public string Status { get; set; } = string.Empty;
    }
}
