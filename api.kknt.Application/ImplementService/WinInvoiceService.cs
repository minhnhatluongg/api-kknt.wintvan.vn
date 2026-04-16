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

    public async Task<WinInvoiceData?> GetUserInfoAsync(
    string taxCode,
    string password,
    CancellationToken ct = default)
    {
        var requestBody = new { taxcode = taxCode, password };

        _logger.LogInformation("Calling WinInvoice API: {Url} with Body: {@Body}",
            _httpClient.BaseAddress + "api/bos_user/user", requestBody);

        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                "api/bos_user/user",
                requestBody,
                ct);

            var responseContent = await response.Content.ReadAsStringAsync(ct);

            _logger.LogInformation("WinInvoice Response Status: {StatusCode}", response.StatusCode);
            _logger.LogInformation("WinInvoice Response Raw Content: {Content}", responseContent);

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
            {
                return result.Data;
            }

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
