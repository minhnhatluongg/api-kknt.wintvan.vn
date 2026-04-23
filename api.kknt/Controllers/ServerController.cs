using api.kknt.Application.DTOs;
using api.kknt.Application.InterfaceServices;
using api.kknt.Domain.Common;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.kknt.API.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/servers")]
    // [Authorize] -> Nào authen xong xui gỡ ra.
    public class ServerController : ControllerBase
    {
        private readonly IServerTaxCode _serverService;

        public ServerController(IServerTaxCode serverService)
        {
            _serverService = serverService;
        }

        /// <summary>
        /// Quét và tìm vị trí MST trên toàn bộ cụm server.
        /// </summary>
        /// <param name="taxCode">Mã số thuế cần kiểm tra</param>
        [HttpGet("locate/{taxCode}")]
        [ProducesResponseType(typeof(ApiResponse<ServerScanResultDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> LocateTaxCode(string taxCode)
        {
            if (string.IsNullOrWhiteSpace(taxCode))
                return BadRequest(ApiResponse.Fail(ApiErrorCodes.InvalidInput, "Mã số thuế không được để trống."));

            var result = await _serverService.GetServerLocationAsync(taxCode);

            if (!result.IsFound)
            {
                return NotFound(ApiResponse.Fail(
                    ApiErrorCodes.NotFound,
                    "Mã số thuế chưa tồn tại trên bất kỳ server nào."
                ));
            }

            if (result.HasConflict)
            {
                return Ok(ApiResponse<ServerScanResultDto>.Ok(result, "Cảnh báo: MST tồn tại trên nhiều server!"));
            }

            return Ok(ApiResponse<ServerScanResultDto>.Ok(result, "Đã tìm thấy vị trí server."));
        }

        /// <summary>
        /// Lấy danh sách nhật ký quét gần nhất (Nếu Nhật có lưu Log vào DB/Cache)
        /// </summary>
        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet("scan-logs/{taxCode}")]
        public async Task<IActionResult> GetScanLogs(string taxCode)
        {
            return Ok();
        }
    }
}
