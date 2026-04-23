using api.kknt.Application.DTOs.SolverServerDTOs;
using api.kknt.Application.InterfaceServices;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;

namespace api.kknt.Application.ImplementService;

public sealed class WinInvoiceService : IWinInvoiceService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WinInvoiceService> _logger;

    public WinInvoiceService(HttpClient httpClient, ILogger<WinInvoiceService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public Task<WinInvoiceData?> GetUserInfoAsync(
        string taxCode,
        string password,
        CancellationToken ct = default)
    {
        var body = new { taxcode = taxCode, password };
        return CallWinInvoiceAsync("api/bos_user/user", body, taxCode, ct);
    }

    public Task<WinInvoiceData?> LookupTaxCodeAsync(
        string taxCode,
        CancellationToken ct = default)
    {
        var body = new { taxcode = taxCode };
        return CallWinInvoiceAsync("api/bos_user/lookup", body, taxCode, ct);
    }

    private async Task<WinInvoiceData?> CallWinInvoiceAsync<TBody>(
        string path,
        TBody body,
        string taxCode,
        CancellationToken ct)
    {
        _logger.LogInformation("Calling WinInvoice API: {Url} with Body: {@Body}",
            _httpClient.BaseAddress + path, body);

        try
        {
            var response = await _httpClient.PostAsJsonAsync(path, body, ct);
            var responseContent = await response.Content.ReadAsStringAsync(ct);

            _logger.LogInformation("WinInvoice Response Status: {StatusCode}", response.StatusCode);
            _logger.LogInformation("WinInvoice Response Raw Content: {Content}", responseContent);

            // 404 trên endpoint lookup = MST chưa tồn tại → không phải lỗi.
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogInformation(
                    "WinInvoice lookup returned 404 for TaxCode={TaxCode} → coi như MST chưa có.",
                    taxCode);
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("WinInvoice API returned error status. Content: {Content}", responseContent);
                return null;
            }

            var result = JsonSerializer.Deserialize<WinInvoiceAuthResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result?.IsSuccess == true)
                return result.Data;

            _logger.LogWarning("WinInvoice API Success = false. Message: {Message}", result?.ErrorMessage);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while calling WinInvoice API for TaxCode: {TaxCode}", taxCode);
            throw;
        }
    }
}
