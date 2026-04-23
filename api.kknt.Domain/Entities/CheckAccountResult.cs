namespace api.kknt.Domain.Entities
{
    /// <summary>
    /// Kết quả trả về từ SP <c>BosOnline..ins_ServerUser_NEW_WT</c>.
    /// isSuccess = 1 nếu insert thành công, 0 nếu MST đã tồn tại / thất bại.
    /// Giữ tên field (lowercase isSuccess) để Dapper tự map từ kết quả SP.
    /// </summary>
    public class CheckAccountResult
    {
        public int     isSuccess  { get; set; }
        public string? Message    { get; set; }
        public string? ErrorCode  { get; set; } = string.Empty;
    }
}
