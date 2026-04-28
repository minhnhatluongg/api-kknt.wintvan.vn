using api.kknt.Application.DTOs;
using api.kknt.Application.InterfaceServices;
using api.kknt.Domain.Common;
using api.kknt.Domain.Enums;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static api.kknt.Application.DTOs.LoginModel;

namespace api.kknt.API.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/sale")]
    public class SaleController : ControllerBase
    {
        private readonly ISaleService _sale;
        public SaleController(ISaleService sale) => _sale = sale;

        [HttpPost("login"), AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] SaleLoginRequest req, CancellationToken ct)
        {
            var rs = await _sale.LoginAsync(req, ct);
            return rs is null
                ? Unauthorized(ApiResponse.Fail(ApiErrorCodes.InvalidCredentials, "Sai username hoặc mật khẩu."))
                : Ok(ApiResponse<SaleAuthResponse>.Ok(rs));
        }

        [HttpGet("orders"), Authorize(Roles = "Sale")]
        public async Task<IActionResult> List([FromQuery] SaleOrderQuery q, CancellationToken ct)
        {
            var saleId = User.FindFirst(AppClaims.SaleID)!.Value;
            return Ok(ApiResponse<SaleOrderListResponse>.Ok(await _sale.ListOrdersAsync(saleId, q, ct)));
        }

        [HttpGet("orders/{oid}"), Authorize(Roles = "Sale")]
        public async Task<IActionResult> Detail(string oid, CancellationToken ct)
        {
            var rs = await _sale.GetOrderDetailAsync(oid, ct);
            return rs is null ? NotFound() : Ok(ApiResponse<SaleOrderDetailResponse>.Ok(rs));
        }

        [HttpPut("orders/{oid}/claim"), Authorize(Roles = "Sale")]
        public async Task<IActionResult> Claim(string oid, CancellationToken ct)
        {
            var saleId = User.FindFirst(AppClaims.SaleID)!.Value;
            var ok = await _sale.UpdateStatusAsync(oid, (int)OrderStatus.Reviewing, saleId, null, ct);
            return ok ? NoContent() : Conflict();
        }

        [HttpPut("orders/{oid}/status"), Authorize(Roles = "Sale")]
        public async Task<IActionResult> UpdateStatus(string oid, [FromBody] UpdateStatusRequest req, CancellationToken ct)
        {
            var saleId = User.FindFirst(AppClaims.SaleID)!.Value;
            var ok = await _sale.UpdateStatusAsync(oid, req.NewStatus, saleId, req.Reason, ct);
            return ok ? NoContent() : Conflict();
        }
    }
}
