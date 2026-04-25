namespace api.kknt.Domain.Entities.Dashboard
{
    /// <summary>
    /// Một sản phẩm (Vitax, HDDT, CKS).
    /// </summary>
    public class ProductItem
    {
        public string ItemID { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string ItemPrice { get; set; } = string.Empty;
        public string? ItemUnitName { get; set; }
        public decimal Cur_Price { get; set; }
        public int UnitPerSubCase { get; set; }
        public string? UnitPerSubCase_Value { get; set; }
        public int UnitPerCase { get; set; }
    }

    /// <summary>
    /// Danh sách sản phẩm theo nhóm.
    /// </summary>
    public class ProductGroup
    {
        public List<ProductItem> LstVitax { get; set; } = new();
        public List<ProductItem> LstHDDT { get; set; } = new();
        public List<ProductItem> LstCKS { get; set; } = new();
        public List<ProductItem> LstTVAN { get; set; } = new();
    }
}
