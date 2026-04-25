namespace api.kknt.Domain.Entities.Dashboard
{
    /// <summary>
    /// Thông tin CKS (chữ ký số) parse từ JSON nbcks.
    /// </summary>
    public class TokenDetail
    {
        public string NotBefore { get; set; } = string.Empty;
        public string NotAfter { get; set; } = string.Empty;
    }

    /// <summary>
    /// Thông tin token đã xử lý (tính ngày còn lại, % progress).
    /// Map từ bảng DB + tính toán thêm.
    /// </summary>
    public class DataToken
    {
        public string? Nbcks { get; set; }
        public string NotBefore { get; set; } = string.Empty;
        public string NotAfter { get; set; } = string.Empty;
        public double RsVal { get; set; }
        public int RsDay { get; set; }
    }
}
