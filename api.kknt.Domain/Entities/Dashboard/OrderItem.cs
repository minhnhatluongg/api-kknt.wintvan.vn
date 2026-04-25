namespace api.kknt.Domain.Entities.Dashboard
{
    /// <summary>
    /// Một đơn hàng hiển thị trên dashboard.
    /// </summary>
    public class OrderItem
    {
        public string? ID { get; set; }
        public string? OID { get; set; }
        public string? CusTax { get; set; }
        public string? CusName { get; set; }
        public string? CusAddress { get; set; }
        public string? CusEmail { get; set; }
        public string? CusPhone { get; set; }
        public string? Crt_date { get; set; }
        public string? SaleID { get; set; }
        public string? TotalAmount { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? ItemName { get; set; }
        public int? Status { get; set; }
    }
    public class OrderDetail
    {
        public string? OID { get; set; }
        public string? ItemID { get; set; }
        public string? ItemName { get; set; }   
        public float? ItemPrice { get; set; }
        public int ItemQtty { get; set; }
        public string? ItemUnitName { get; set; }
    }
}
