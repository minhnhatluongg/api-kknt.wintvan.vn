namespace api.kknt.Domain.Entities.Dashboard
{
    /// <summary>
    /// Kết quả trả về từ getMST_Login (inline SQL thay cho SP cũ).
    /// </summary>
    public class MstLoginResult
    {
        public string MstLogin { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public DateTime? CrtDate { get; set; }
    }
}
