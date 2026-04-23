namespace api.kknt.Application.Options;

public sealed class WinInvoiceOptions
{
    public const string Section = "WinInvoiceApi";
    public string BaseUrl { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
}
