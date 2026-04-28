using api.kknt.Application.DTOs;
using api.kknt.Application.InterfaceServices;
using api.kknt.Application.Options;
using api.kknt.Domain.Interfaces.DatabaseConfig;
using api.kknt.Infrastructure.AesEncryptionService;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static api.kknt.Application.DTOs.LoginModel;

namespace api.kknt.Application.ImplementService
{
    public class SaleService : ISaleService
    {
        private readonly IDbConnectionFactory _dbFactory;
        private readonly IConfiguration _configuration;
        private readonly JwtSettings _jwt;
        private readonly IRefreshTokenStore _tokenStore;

        public SaleService(IDbConnectionFactory dbFactory, IConfiguration configuration, JwtSettings jwt, IRefreshTokenStore tokenStore)
        {
            _dbFactory = dbFactory;
            _configuration = configuration;
            _jwt = jwt;
            _tokenStore = tokenStore;
        }

        public async Task<SaleAuthResponse?> LoginAsync(SaleLoginRequest req, CancellationToken ct)
        {
            await using var conn = await _dbFactory.CreateDefault_108_Async("BosEVATbizzi", ct);
            var sale = await conn.QuerySingleOrDefaultAsync<dynamic>(
                new CommandDefinition("dbo.Auth_Sale_Login",
                    new { username = req.Username },
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: ct));

            if (sale is null || sale.SaleID is null) return null;

            var encrypted = Sha1.Encrypt(req.Password);
            if (!string.Equals((string)sale.PasswordEnc, encrypted, StringComparison.Ordinal))
                return null;

            var claims = new[]
            {
            new Claim(JwtRegisteredClaimNames.Sub, (string)sale.SaleID),
            new Claim(AppClaims.SaleID,   (string)sale.SaleID),
            new Claim(AppClaims.SaleName, (string)(sale.FullName ?? sale.Username ?? "")),
            new Claim(AppClaims.Role,     "Sale"),
            new Claim(ClaimTypes.Role,    "Sale")
        };
            var access = GenerateAccessToken(claims);
            var refresh = await _tokenStore.CreateAsync((string)sale.SaleID,
                DateTime.UtcNow.AddDays(_jwt.RefreshExpiryDays), ct);

            return new SaleAuthResponse(access, refresh, _jwt.ExpiresInSeconds,
                (string)sale.SaleID, (string)sale.Username, (string)(sale.FullName ?? ""));
        }

        public async Task<SaleOrderListResponse> ListOrdersAsync(string saleId, SaleOrderQuery q, CancellationToken ct)
        {
            // Default range: nếu cả From và To đều null → lấy 3 tháng gần nhất
            DateTime? fromDate = q.From;
            DateTime? toDate   = q.To;
            if (fromDate is null && toDate is null)
            {
                fromDate = DateTime.Now.Date.AddMonths(-3);
                // toDate giữ null (đến hiện tại)
            }

            var pageIndex = q.Page <= 0 ? 1 : q.Page;
            var pageSize  = q.Size <= 0 ? 50 : Math.Min(q.Size, 200);

            var statusList = string.IsNullOrWhiteSpace(q.Status)  ? null : q.Status.Trim();
            var keyword    = string.IsNullOrWhiteSpace(q.Keyword) ? null : q.Keyword.Trim();

            await using var conn = await _dbFactory.CreateDefault_108_Async("BosEVATbizzi", ct);
            var p = new DynamicParameters();
            p.Add("@saleID",        saleId);
            p.Add("@defaultSaleID", _configuration["Sale:DefaultSaleId"]);
            p.Add("@statusList",    statusList);
            p.Add("@fromDate",      fromDate);
            p.Add("@toDate",        toDate);
            p.Add("@keyword",       keyword);
            p.Add("@pageIndex",     pageIndex);
            p.Add("@pageSize",      pageSize);

            using var multi = await conn.QueryMultipleAsync(
                new CommandDefinition("dbo.Get_OrdersBySale", p,
                    commandType: CommandType.StoredProcedure, cancellationToken: ct));

            var total = await multi.ReadFirstAsync<int>();
            var items = (await multi.ReadAsync<SaleOrderListItem>()).ToList();

            return new SaleOrderListResponse(total, pageIndex, pageSize, items);
        }

        public async Task<SaleOrderDetailResponse?> GetOrderDetailAsync(string oid, CancellationToken ct)
        {
            await using var conn = await _dbFactory.CreateDefault_108_Async("BosEVATbizzi", ct);
            using var multi = await conn.QueryMultipleAsync(
                new CommandDefinition("dbo.Get_OrderDetailByOID",
                    new { OID = oid },
                    commandType: CommandType.StoredProcedure, cancellationToken: ct));

            var master = await multi.ReadFirstOrDefaultAsync<SaleOrderListItem>();
            if (master is null) return null;
            var lines = (await multi.ReadAsync<OrderLineItem>()).ToList();
            return new SaleOrderDetailResponse(master, lines);
        }

        public async Task<bool> UpdateStatusAsync(string oid, int newStatus, string saleId, string? reason, CancellationToken ct)
        {
            await using var conn = await _dbFactory.CreateDefault_108_Async("BosEVATbizzi", ct);
            var rs = await conn.QuerySingleOrDefaultAsync<dynamic>(
                new CommandDefinition("dbo.Upd_OrderStatus",
                    new { OID = oid, newStatus, saleID = saleId, actor = saleId, reason },
                    commandType: CommandType.StoredProcedure, cancellationToken: ct));
            return rs?.isSuccess == 1;
        }

        #region Helpers
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
