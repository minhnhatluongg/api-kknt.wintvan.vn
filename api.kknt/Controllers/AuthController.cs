using api.kknt.Application.InterfaceServices;
using api.kknt.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static api.kknt.Application.DTOs.LoginModel;

namespace api.kknt.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<AuthResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 401)]
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

        [HttpPost("refresh")]
        [AllowAnonymous]
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

        [HttpPost("revoke")]
        [Authorize]
        public async Task<IActionResult> Revoke(
            [FromBody] RefreshRequest request,
            CancellationToken ct)
        {
            await _authService.RevokeAsync(request.RefreshToken, ct);
            return NoContent();
        }
    }
}
