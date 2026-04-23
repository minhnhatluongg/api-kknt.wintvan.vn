using api.kknt.Application.DTOs.SolverServerDTOs;

namespace api.kknt.Application.InterfaceServices;

public interface IWinInvoiceService
{
    /// <summary>
    /// Lấy thông tin tài khoản từ WinInvoice bằng taxCode + password.
    /// Dùng cho luồng LOGIN.
    /// </summary>
    Task<WinInvoiceData?> GetUserInfoAsync(
        string taxCode,
        string password,
        CancellationToken ct = default);

    /// <summary>
    /// Tra cứu MST trên hệ thống WinInvoice (không cần password).
    /// Dùng cho luồng REGISTER để biết MST đã tồn tại ở con server nào chưa.
    /// - Trả về <see cref="WinInvoiceData"/> (với <c>ServerKey</c>) nếu MST đã có.
    /// - Trả về <c>null</c> nếu MST chưa sử dụng WinInvoice ở bất kỳ server nào.
    /// </summary>
    Task<WinInvoiceData?> LookupTaxCodeAsync(
        string taxCode,
        CancellationToken ct = default);
}
