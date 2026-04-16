using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static api.kknt.Application.DTOs.LoginModel;

namespace api.kknt.Application.InterfaceServices
{
    public interface IAuthService
    {
        Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken ct);
        Task<AuthResponse?> RefreshAsync(string refreshToken, CancellationToken ct);
        Task RevokeAsync(string refreshToken, CancellationToken ct);
    }
}
