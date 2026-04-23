using api.kknt.Application.DTOs;
using api.kknt.Application.InterfaceServices;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace api.kknt.Application.ImplementService
{
    public class TctLoginClient : ITctLoginClient
    {
        private const string Endpoint = "api/partner/Invoices/login_tct_client";
        private readonly HttpClient _http;
        private readonly ILogger<TctLoginClient> _logger;
        public TctLoginClient(HttpClient http, ILogger<TctLoginClient> logger)
        {
            _http = http;
            _logger = logger;
        }

        #region Model
        private sealed class TctLoginDto
        {
            public int Code { get; set; }
            public string? Status { get; set; }
            public string? Message { get; set; }
            public object? Data { get; set; }
        }

        #endregion
        public async Task<TctLoginResult> LoginAsync(string taxCode, string password, CancellationToken ct = default)
        {
            try
            {
                var body = new { username = taxCode, password };
                using var res = await _http.PostAsJsonAsync(Endpoint, body, ct);
                var raw = await res.Content.ReadAsStringAsync(ct);

                if (!res.IsSuccessStatusCode)
                {
                    _logger.LogWarning("[TCT] Login HTTP {Status} TaxCode={Tax} body={Body}",
                        (int)res.StatusCode, taxCode, raw);
                    return new TctLoginResult(false, (int)res.StatusCode, "HttpError", raw);
                }

                var dto = JsonSerializer.Deserialize<TctLoginDto>(raw, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                var ok = dto is not null
                         && dto.Code == 200
                         && string.Equals(dto.Status, "Success", StringComparison.OrdinalIgnoreCase);

                return new TctLoginResult(ok, dto?.Code ?? 0, dto?.Status ?? "", dto?.Message ?? "");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TCT] Login exception TaxCode={Tax}", taxCode);
                return new TctLoginResult(false, 500, "Exception", ex.Message);
            }
        }
    }
}
