using api.kknt.Application.DTOs;
using api.kknt.Application.InterfaceServices;
using api.kknt.Domain.Common;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static api.kknt.Application.DTOs.LoginModel;
using static api.kknt.Application.DTOs.RegisterModel;

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

        /// <summary>
        /// Đăng ký tài khoản VITAX cho khách hàng mới.
        /// </summary>
        /// <param name="request">Thông tin đăng ký khách hàng. Xem <see cref="RegisterRequest"/>.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <response code="200">Đăng ký thành công, trả server đích + trạng thái server mới/cũ.</response>
        /// <response code="400">Dữ liệu đầu vào không hợp lệ (thiếu field, sai format email, mật khẩu quá ngắn...).</response>
        /// <response code="409">MST/mật khẩu không login được TCT, hoặc MST đã tồn tại trên hệ thống và fail khi update.</response>
        [HttpPost("register")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<RegisterResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Register(
            [FromBody] RegisterRequest request,
            CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse.Fail(
                    ApiErrorCodes.InvalidInput,
                    "Dữ liệu đăng ký không hợp lệ."));

            var result = await _authService.RegisterAsync(request, ct);

            if (result.IsSuccess)
                return Ok(ApiResponse<RegisterResponse>.Ok(result.Data!, "Đăng ký thành công."));

            return result.ErrorCode switch
            {
                RegisterErrorCode.TctAuthFailed => Unauthorized(ApiResponse.Fail(
                    ApiErrorCodes.TctAuthFailed,
                    result.ErrorMessage!)),

                RegisterErrorCode.MstAlreadyInMaster => Conflict(ApiResponse.Fail(
                    ApiErrorCodes.MstAlreadyInMaster,
                    result.ErrorMessage!)),

                RegisterErrorCode.InsertServerFailed => Conflict(ApiResponse.Fail(
                    ApiErrorCodes.InsertServerFailed,
                    result.ErrorMessage!)),

                RegisterErrorCode.UpdatePasswordFailed => UnprocessableEntity(ApiResponse.Fail(
                    ApiErrorCodes.UpdatePasswordFailed,
                    result.ErrorMessage!)),

                _ => StatusCode(StatusCodes.Status500InternalServerError,
                     ApiResponse.Fail(ApiErrorCodes.ServerError,
                         result.ErrorMessage ?? "Lỗi không xác định.")),
            };
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
