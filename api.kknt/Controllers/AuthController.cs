using api.kknt.Application.InterfaceServices;
using api.kknt.Domain.Common;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static api.kknt.Application.DTOs.LoginModel;

namespace api.kknt.API.Controllers
{
    /// <summary>
    /// Xác thực người dùng: login, refresh token, revoke token.
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>Đăng nhập bằng username/password, nhận JWT + refresh token.</summary>
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse),               StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login(
            [FromBody] LoginRequest request,
            CancellationToken ct)
        {
            var result = await _authService.LoginAsync(request, ct);
            if (result is null)
                return Unauthorized(ApiResponse.Fail(
                    ApiErrorCodes.InvalidCredentials,
                    "Thông tin đăng nhập không hợp lệ."));

            return Ok(ApiResponse<AuthResponse>.Ok(result));
        }

        /// <summary>Làm mới access token bằng refresh token.</summary>
        [HttpPost("refresh")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse),               StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Refresh(
            [FromBody] RefreshRequest request,
            CancellationToken ct)
        {
            var result = await _authService.RefreshAsync(request.RefreshToken, ct);
            if (result is null)
                return Unauthorized(ApiResponse.Fail(
                    ApiErrorCodes.InvalidToken,
                    "Refresh token không hợp lệ hoặc đã hết hạn."));

            return Ok(ApiResponse<AuthResponse>.Ok(result));
        }

        /// <summary>Thu hồi một refresh token.</summary>
        [HttpPost("revoke")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Revoke(
            [FromBody] RefreshRequest request,
            CancellationToken ct)
        {
            await _authService.RevokeAsync(request.RefreshToken, ct);
            return NoContent();
        }
    }
}
