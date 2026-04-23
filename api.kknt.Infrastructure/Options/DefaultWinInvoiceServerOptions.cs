namespace api.kknt.Application.Options;

/// <summary>
/// Thông tin server WinInvoice mặc định dành cho MST đăng ký MỚI
/// (chưa có trong bất kỳ server WinInvoice nào).
/// Hiện tại trỏ sang con server WinInvoice mới nhất: 10.10.101.108,5172
/// </summary>
public sealed class DefaultWinInvoiceServerOptions
{
    public const string Section = "DefaultWinInvoiceServer";

    public string ServerHost { get; set; } = null!;
    public string Catalog { get; set; } = "BosEVATbizzi";
    public string User { get; set; } = null!;
    public string Password { get; set; } = null!;
    public bool Encrypt { get; set; } = true;
    public int ConnectTimeout { get; set; } = 30;
}
