using api.kknt.Application.DTOs.Dashboard;
using api.kknt.Application.InterfaceServices;
using api.kknt.Domain.Common;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static api.kknt.Application.DTOs.LoginModel;

namespace api.kknt.API.Controllers
{
    /// <summary>
    /// Dashboard — dữ liệu trang chính sau khi đăng nhập.
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/dashboard")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(
            IDashboardService dashboardService,
            ILogger<DashboardController> logger)
        {
            _dashboardService = dashboardService;
            _logger = logger;
        }

        /// <summary>
        /// API #1: Lấy toàn bộ dữ liệu dashboard khi vào trang.
        /// </summary>
        [HttpGet]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<DashboardResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetDashboard(CancellationToken ct)
        {
            try
            {
                // Lấy user info từ JWT claims 
                var loginName = User.FindFirst(AppClaims.TaxCode)?.Value ?? "";
                var serverHost = User.FindFirst(AppClaims.ServerHost)?.Value ?? "";
                var dateActiveCmpn = DateTime.Now.AddMonths(1); 

                var result = await _dashboardService.GetDashboardAsync(loginName, serverHost, dateActiveCmpn, ct);
                return Ok(ApiResponse<DashboardResponse>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Dashboard] GetDashboard error");
                return StatusCode(500, ApiResponse.Fail(
                    ApiErrorCodes.InternalServerError,
                    "Lỗi khi tải dữ liệu dashboard."));
            }
        }

        /// <summary>
        /// API #2: Lấy tổng tiền hóa đơn (chịu thuế, thuế, thanh toán).
        /// </summary>
        [HttpGet("total-invoice-money")]
        [ProducesResponseType(typeof(ApiResponse<TotalInvoiceMoneyResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTotalInvoiceMoney(
            [FromQuery] string? dateFrom,
            [FromQuery] string? dateTo,
            [FromQuery] string? mstCompany,
            CancellationToken ct)
        {
            try
            {
                var result = await _dashboardService.GetTotalInvoiceMoneyAsync(
                    dateFrom ?? "", dateTo ?? "", mstCompany ?? "", ct);
                return Ok(ApiResponse<TotalInvoiceMoneyResponse>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Dashboard] GetTotalInvoiceMoney error");
                return StatusCode(500, ApiResponse.Fail(
                    ApiErrorCodes.InternalServerError,
                    "Lỗi khi tải tổng tiền hóa đơn."));
            }
        }

        /// <summary>
        /// API #3: Lấy DS công ty cho combobox (Select3).
        /// </summary>
        [HttpGet("companies")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUserCombobox(
            [FromQuery] string? term,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            CancellationToken ct = default)
        {
            try
            {
                var result = await _dashboardService.GetUserComboboxAsync(term, page, pageSize, ct);
                return Ok(ApiResponse<object>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Dashboard] GetUserCombobox error");
                return StatusCode(500, ApiResponse.Fail(
                    ApiErrorCodes.InternalServerError,
                    "Lỗi khi tải danh sách công ty."));
            }
        }

        /// <summary>
        /// API #4: Lấy data khi chọn một công ty cụ thể.
        /// </summary>
        [HttpGet("company-info")]
        [ProducesResponseType(typeof(ApiResponse<CompanyInfoResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCompanyInfo(
            [FromQuery] string mstCompany,
            CancellationToken ct)
        {
            try
            {
                var loginName = User.FindFirst(AppClaims.TaxCode)?.Value ?? "";
                var serverHost = User.FindFirst(AppClaims.ServerHost)?.Value ?? "";

                var result = await _dashboardService.GetCompanyInfoAsync(loginName, mstCompany, serverHost, ct);
                return Ok(ApiResponse<CompanyInfoResponse>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Dashboard] GetCompanyInfo error");
                return StatusCode(500, ApiResponse.Fail(
                    ApiErrorCodes.InternalServerError,
                    "Lỗi khi tải thông tin công ty."));
            }
        }

        /// <summary>
        /// API #5: Chuyển đổi loại ngày → dateFrom/dateTo.
        /// </summary>
        [HttpGet("choose-date")]
        [ProducesResponseType(typeof(ApiResponse<ChooseDateResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ChooseDate(
            [FromQuery] string typeChoose,
            CancellationToken ct)
        {
            try
            {
                var result = await _dashboardService.ChooseDateAsync(typeChoose, ct);
                return Ok(ApiResponse<ChooseDateResponse>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Dashboard] ChooseDate error");
                return StatusCode(500, ApiResponse.Fail(
                    ApiErrorCodes.InternalServerError,
                    "Lỗi khi chọn ngày."));
            }
        }

        /// <summary>
        /// API #6: Lấy PieChart data (hóa đơn mua vào / bán ra).
        /// </summary>
        [HttpGet("pie-chart")]
        [ProducesResponseType(typeof(ApiResponse<PieChartResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPieChart(
            [FromQuery] string? mstCompany,
            [FromQuery] string? dateFrom,
            [FromQuery] string? dateTo,
            CancellationToken ct)
        {
            try
            {
                var loginName = User.FindFirst(AppClaims.TaxCode)?.Value ?? "";
                var serverHost = User.FindFirst(AppClaims.ServerHost)?.Value ?? "";

                var result = await _dashboardService.GetPieChartAsync(
                    loginName, mstCompany ?? "", dateFrom ?? "", dateTo ?? "", serverHost, ct);
                return Ok(ApiResponse<PieChartResponse>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Dashboard] GetPieChart error");
                return StatusCode(500, ApiResponse.Fail(
                    ApiErrorCodes.InternalServerError,
                    "Lỗi khi tải biểu đồ."));
            }
        }

        /// <summary>
        /// API #7: Thêm sản phẩm vào giỏ hàng.
        /// </summary>
        [HttpPost("add-cart")]
        [ProducesResponseType(typeof(ApiResponse<AddCartResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddCart(
            [FromBody] AddCartRequestDto request,
            CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse.Fail(
                    ApiErrorCodes.InvalidInput,
                    "Dữ liệu giỏ hàng không hợp lệ."));

            try
            {
                var loginName = User.FindFirst(AppClaims.TaxCode)?.Value ?? "";
                var result = await _dashboardService.AddCartAsync(loginName, request, ct);
                return Ok(ApiResponse<AddCartResponseDto>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Dashboard] AddCart error");
                return StatusCode(500, ApiResponse.Fail(
                    ApiErrorCodes.InternalServerError,
                    "Lỗi khi thêm giỏ hàng."));
            }
        }
    }
}
