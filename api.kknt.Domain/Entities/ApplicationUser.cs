using System.ComponentModel.DataAnnotations;

namespace api.kknt.Domain.Entities
{
    /// <summary>
    /// Port nguyên từ ApplicationUser cũ (IdentityUser + custom fields) — chỉ giữ
    /// những field được repo / SP sử dụng trong luồng đăng ký.
    /// Password lưu ở đây đã được hash Sha1 trước khi đưa vào.
    /// </summary>
    public class ApplicationUser
    {
        [Required, StringLength(50)]
        public string LoginName     { get; set; } = null!;

        [Required, StringLength(200)]
        public string FullName      { get; set; } = null!;

        [StringLength(500)]
        public string? Address      { get; set; }

        [Required, StringLength(200)]
        public string Password      { get; set; } = null!;

        [EmailAddress, StringLength(100)]
        public string? CmpnMail     { get; set; }

        [Required, StringLength(20)]
        public string TaxNumber     { get; set; } = null!;

        [StringLength(20)]
        public string? Tel          { get; set; }

        [StringLength(100)]
        public string? ContactName  { get; set; }

        /// <summary>optionFirstJob — giữ tên field trùng với FE cũ.</summary>
        public string? OptionFirstJob { get; set; }

        // ── Các field phục vụ SP BosOnline..ins_ServerUser_NEW_WT ─────────────
        [StringLength(50)]
        public string? MerchantID   { get; set; }

        /// <summary>Khách WT (hay không) — mặc định false, chỉ bật cho kênh WT.</summary>
        public bool IsWT            { get; set; }

        /// <summary>Server WT (nếu có) — null nếu khách thường.</summary>
        [StringLength(100)]
        public string? ServerWT     { get; set; }

        // ── Alias dùng cho email template (legacy Identity: Email / PhoneNumber) ─
        /// <summary>Alias cho CmpnMail — lấy cho email template.</summary>
        public string? Email        => CmpnMail;

        /// <summary>Alias cho Tel — lấy cho email template.</summary>
        public string? PhoneNumber  => Tel;
    }
}
