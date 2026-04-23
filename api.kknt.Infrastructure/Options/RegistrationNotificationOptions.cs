namespace api.kknt.Application.Options;

/// <summary>
/// Cấu hình SMTP để gửi email thông báo khi có khách đăng ký mới.
/// </summary>
public sealed class RegistrationNotificationOptions
{
    public const string Section = "RegistrationNotification";

    public bool Enabled { get; set; } = true;

    public string SmtpHost { get; set; } = null!;
    public int SmtpPort { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string From { get; set; } = null!;

    // Có thể để nhiều email, phân cách bằng ";"
    public string SaleTo { get; set; } = null!;
    public string SupportTo { get; set; } = null!;

    // Thông tin doanh nghiệp + hỗ trợ (dùng để thay placeholder)
    public string TenDonVi { get; set; } = "KKNT-WinTech";
    public string LoginUrl { get; set; } = "https://kknt.wintvan.vn/";
    public string SupportPhone { get; set; } = "1900-xxxx";
    public string SupportEmail { get; set; } = "minhnhatluongwork@gmail.com";
    public int TrialDays { get; set; } = 3;

    // Thư mục chứa template
    public string TemplateDir { get; set; } = "fileout/Template";

    // Tên file từng loại mail
    public string CustomerTemplate { get; set; } = "emailCustomer.html";
    public string SaleTemplate { get; set; } = "emailSale.html";
    public string SupportTemplate { get; set; } = "emailSupport.html";
}
