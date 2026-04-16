using api.kknt.Application.DTOs;
using api.kknt.Application.DTOs.SolverServerDTOs;
using api.kknt.Application.InterfaceServices;
using api.kknt.Application.Options;
using api.kknt.Domain.Entities;
using api.kknt.Domain.Interfaces.DatabaseConfig;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using static api.kknt.Application.DTOs.LoginModel;

namespace api.kknt.Application.ImplementService
{
    public class AuthService : IAuthService
    {
        private readonly IWinInvoiceService _winInvoice;
        private readonly IServerResolver _serverResolver;
        private readonly IRefreshTokenStore _tokenStore;
        private readonly JwtSettings _jwt;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IWinInvoiceService winInvoice,
            IServerResolver serverResolver,
            IRefreshTokenStore tokenStore,
            IOptions<JwtSettings> jwt,
            ILogger<AuthService> logger)
        {
            _winInvoice = winInvoice;
            _serverResolver = serverResolver;
            _tokenStore = tokenStore;
            _jwt = jwt.Value;
            _logger = logger;
        }
        public async Task<LoginModel.AuthResponse?> LoginAsync(LoginModel.LoginRequest request, CancellationToken ct)
        {
            //step 1 - xác thực login bằng api win invoice
            var winInfo = await _winInvoice.GetUserInfoAsync(request.TaxCode, request.Password, ct);

            if (winInfo == null)
            {
                _logger.LogWarning("WinInvoice auth failed for {TaxCode}", request.TaxCode);
                return null;
            }
            //step 2 - mò ip Db server
            var dbMapping = await _serverResolver.ResolveAsync($"__{winInfo.ServerKey}", ct);
            if (dbMapping == null)
            {
                _logger.LogWarning("No DB mapping for serverKey={ServerKey}", winInfo.ServerKey);
                return null;
            }

            //step 3 - build token (lấy cái nào FE cần, input vào)
            var claims = BuildClaims(winInfo, dbMapping);
            var accessToken = GenerateAccessToken(claims);
            var existingRefreshToken = DateTime.UtcNow.AddDays(3);

            var refreshToken = await _tokenStore.CreateAsync(request.TaxCode, existingRefreshToken, ct);

            return new AuthResponse(accessToken, refreshToken, _jwt.ExpiresInSeconds);

        }

        public Task<LoginModel.AuthResponse?> RefreshAsync(string refreshToken, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public Task RevokeAsync(string refreshToken, CancellationToken ct)
        {
            return _tokenStore.RevokeAsync(refreshToken, ct);
        }

        #region Helpers 
        private static List<Claim> BuildClaims(WinInvoiceData winInfo, TaxServerMapping dbMapping) =>
    [
        new(JwtRegisteredClaimNames.Sub, winInfo.Taxcode),
        new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new(AppClaims.TaxCode,     winInfo.Taxcode),
        new(AppClaims.BosUserCode, winInfo.BosUserCode),
        new(AppClaims.CmpnID,      winInfo.CmpnID),
        new(AppClaims.ServerKey,   winInfo.ServerKey),
        new(AppClaims.ServerHost,  dbMapping.ServerHost),
        new(AppClaims.Catalog,     dbMapping.Catalog),
    ];

        private string GenerateAccessToken(IEnumerable<Claim> claims)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.SecretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwt.Issuer,
                audience: _jwt.Audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddSeconds(_jwt.ExpiresInSeconds),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        #endregion
    }
}
