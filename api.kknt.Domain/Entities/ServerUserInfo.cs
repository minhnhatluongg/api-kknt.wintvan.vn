namespace api.kknt.Domain.Entities
{
    /// <summary>
    /// Kết quả query tblServerUser — dùng cho Login verify.
    /// Map trực tiếp từ SELECT trên BosEVATbizzi..tblServerUser.
    /// </summary>
    public record ServerUserInfo(
        string MST,
        string? FullName,
        string? Server,
        string? MerchantID,
        string? Email,
        string? contactName,
        string? Password,
        bool? isDelete);
}
