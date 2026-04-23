using api.kknt.Application.DTOs;
using static api.kknt.Application.DTOs.LoginModel;
using static api.kknt.Application.DTOs.RegisterModel;

namespace api.kknt.Application.InterfaceServices
{
    public interface IAuthService
    {
        Task<AuthResponse?>     LoginAsync(LoginRequest request, CancellationToken ct);
        Task<AuthResponse?>     RefreshAsync(string refreshToken, CancellationToken ct);
        Task                    RevokeAsync(string refreshToken, CancellationToken ct);

        /// <summary>
        /// Đăng ký tài khoản mới.
        /// - Check MST qua WinInvoice; nếu đã có → insert tblServerUser ngay trên con server đó.
        /// - Nếu chưa có → dùng DefaultWinInvoiceServer (10.10.101.108,5172).
        /// Trả về <c>null</c> nếu MST đã tồn tại trên server đích.
        /// </summary>
        Task<RegisterResult?> RegisterAsync(RegisterRequest request, CancellationToken ct);
    }
}
