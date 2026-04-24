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
        /// <remarks>
        /// Luồng xử lý:
        ///
        /// 1. **Xác thực với Tổng cục Thuế (TCT)** — gọi nội bộ endpoint `login_tct_client` để chắc chắn
        ///    cặp `TaxCode` + `Password` có thể login được trên TCT. Nếu sai → trả `409`.
        ///
        /// 2. **Quét MST trên toàn hệ thống WinInvoice** (`IServerTaxService`).
        ///    - **Kịch bản A (MST đã có trên hệ thống):** lấy `ServerHost` của server đang giữ MST,
        ///      update `PasswordTCT` trong `bosConfigure.dbo.bosUser`, không tạo master mới.
        ///    - **Kịch bản B (MST mới hoàn toàn):** dùng server mặc định cấu hình trong
        ///      `DefaultWinInvoiceServer` (hiện tại `10.10.101.108,5172`), ghi master trước ở
        ///      `BosEVATbizzi.dbo.tblUserMaster`.
        ///
        /// 3. **Insert** vào `tblServerUser` của server đích qua SP `ins_ServerUser_NEW_WT`.
        ///
        /// 4. **Tạo đơn Trial 3 tháng** (nuốt lỗi nếu có — không chặn đăng ký).
        ///
        /// 5. **Invalidate cache resolver** để lần login kế tiếp trả đúng mapping mới.
        ///
        /// 6. **Fire-and-forget** 3 email: cho Khách, Kinh doanh, Hỗ trợ kỹ thuật
        ///    qua `IRegistrationEmailService.SendAllSafeAsync`.
        ///
        /// Ví dụ request:
        /// ```json
        /// {
        ///   "taxCode": "0318607075",
        ///   "companyName": "CÔNG TY TNHH ABC",
        ///   "contactName": "Nguyễn Văn A",
        ///   "cmpnAddress": "123 Lê Lợi, Q.1, TP.HCM",
        ///   "cmpnPhone": "0909123456",
        ///   "email": "a@abc.vn",
        ///   "password": "P@ssword123",
        ///   "chooseVal": "KKNT"
        /// }
        /// ```
        ///
        /// Ví dụ response thành công:
        /// ```json
        /// {
        ///   "success": true,
        ///   "message": "Đăng ký thành công.",
        ///   "data": {
        ///     "taxCode": "0318607075",
        ///     "serverHost": "10.10.101.108,5172",
        ///     "isNewServer": true
        ///   }
        /// }
        /// ```
        /// </remarks>
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

            // Map lỗi sang HTTP code
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
