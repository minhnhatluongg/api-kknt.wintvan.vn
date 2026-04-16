using api.kknt.Application.DTOs;
using api.kknt.Application.InterfaceServices;
using api.kknt.Domain.Common;
using api.kknt.Domain.Interfaces.DatabaseConfig;
using Microsoft.AspNetCore.Mvc;

namespace api.kknt.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class TestMappingController : ControllerBase   // ← ControllerBase, không phải Controller
{
    private readonly IWinInvoiceService _winInvoiceService;
    private readonly IServerResolver _serverResolver;
    private readonly ILogger<TestMappingController> _logger;

    public TestMappingController(
        IWinInvoiceService winInvoiceService,
        IServerResolver serverResolver,
        ILogger<TestMappingController> logger)
    {
        _winInvoiceService = winInvoiceService;
        _serverResolver    = serverResolver;
        _logger            = logger;
    }

    /// <summary>
    /// Kiểm tra toàn bộ luồng: WinInvoice Auth ➜ ServerKey ➜ Master DB Mapping.
    /// GET /api/TestMapping/lookup/{taxCode}?password=xxx
    /// </summary>
    [HttpGet("lookup/{taxCode}")]
    [ProducesResponseType(typeof(ApiResponse<LookupResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse),               StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse),               StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<LookupResult>>> LookupAsync(
        string taxCode,
        [FromQuery] string? password,
        CancellationToken ct)                             
    {
        var winInfo = await _winInvoiceService.GetUserInfoAsync(
            taxCode,
            password ?? taxCode,   
            ct);

        if (winInfo is null)
        {
            _logger.LogWarning("WinInvoice lookup failed for taxCode={TaxCode}", taxCode);
            return BadRequest(
                ApiResponse.Fail(
                    ApiErrorCodes.WinInvoiceError,
                    "Không tìm thấy thông tin trên WinInvoice hoặc sai mật khẩu."));
        }

        var mappingKey = $"__{winInfo.ServerKey}";
        var dbMapping  = await _serverResolver.ResolveAsync(mappingKey, ct);

        if (dbMapping is null)
        {
            _logger.LogWarning(
                "No DB mapping found for serverKey={ServerKey} (taxCode={TaxCode})",
                winInfo.ServerKey, taxCode);

            return NotFound(
                ApiResponse.Fail(
                    ApiErrorCodes.ServerMappingNotFound,
                    $"Tìm thấy serverKey '{winInfo.ServerKey}' nhưng không có mapping trong DB."));
        }

        var result = new LookupResult(
            WinInvoice: new WinInvoiceInfo(winInfo.ServerKey, winInfo.CmpnID, winInfo.BosUserCode),
            DbMapping:  new DbMappingInfo(dbMapping.ServerHost, dbMapping.Catalog, dbMapping.User, dbMapping.IsActive));

        return Ok(ApiResponse<LookupResult>.Ok(result));
    }
}
