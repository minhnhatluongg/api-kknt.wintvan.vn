using api.kknt.Application.DTOs.SolverServerDTOs;

namespace api.kknt.Application.InterfaceServices;

public interface IWinInvoiceService
{
    /// <summary>Lấy thông tin tài khoản từ WinInvoice bằng taxCode + password.</summary>
    Task<WinInvoiceData?> GetUserInfoAsync(
        string taxCode,
        string password,
        CancellationToken ct = default);
}
