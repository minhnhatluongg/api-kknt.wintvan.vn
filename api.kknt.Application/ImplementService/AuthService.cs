using api.kknt.Application.DTOs;
using api.kknt.Application.DTOs.SolverServerDTOs;
using api.kknt.Application.InterfaceServices;
using api.kknt.Application.Options;
using api.kknt.Domain.Entities;
using api.kknt.Domain.Interfaces;
using api.kknt.Domain.Interfaces.DatabaseConfig;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Text;
using static api.kknt.Application.DTOs.LoginModel;
using static api.kknt.Application.DTOs.RegisterModel;

namespace api.kknt.Application.ImplementService
{
    public class AuthService : IAuthService
    {
        private readonly IWinInvoiceService _winInvoice;
        private readonly IServerResolver _serverResolver;
        private readonly IRefreshTokenStore _tokenStore;
        private readonly IUserRegistrationRepository _registrationRepo;
        private readonly IPasswordHasher _passwordHasher;
        private readonly JwtSettings _jwt;
        private readonly DefaultWinInvoiceServerOptions _defaultServer;
        private readonly RegistrationNotificationOptions _notify;
        private readonly IServerTaxCode _serverTaxService;
        private readonly ILogger<AuthService> _logger;
        private readonly ITctLoginClient _tctLoginClient;
        private readonly IConfiguration _config;
        private readonly IRegistrationEmailService _emailService;

        public AuthService(
            IWinInvoiceService winInvoice,
            IServerResolver serverResolver,
            IRefreshTokenStore tokenStore,
            IUserRegistrationRepository registrationRepo,
            IPasswordHasher passwordHasher,
            IOptions<JwtSettings> jwt,
            IRegistrationEmailService emailService,
            IOptions<DefaultWinInvoiceServerOptions> defaultServer,
            IOptions<RegistrationNotificationOptions> notify,
            IServerTaxCode serverTaxService,
            IConfiguration configuration,
            ITctLoginClient tctLoginClient,
            ILogger<AuthService> logger)
        {
            _winInvoice = winInvoice;
            _serverResolver = serverResolver;
            _tokenStore = tokenStore;
            _registrationRepo = registrationRepo;
            _passwordHasher = passwordHasher;
            _jwt = jwt.Value;
            _defaultServer = defaultServer.Value;
            _notify = notify.Value;
            _serverTaxService = serverTaxService;
            _logger = logger;
            _tctLoginClient = tctLoginClient;
            _config = configuration;
            _emailService = emailService;
        }
        
        public async Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken ct)
        {
            // step 1 - xác thực login bằng api WinInvoice
            var winInfo = await _winInvoice.GetUserInfoAsync(request.TaxCode, request.Password, ct);
            if (winInfo == null)
            {
                _logger.LogWarning("WinInvoice auth failed for {TaxCode}", request.TaxCode);
                return null;
            }

            // step 2 - mò ip Db server
            var dbMapping = await _serverResolver.ResolveAsync($"__{winInfo.ServerKey}", ct);
            if (dbMapping == null)
            {
                _logger.LogWarning("No DB mapping for serverKey={ServerKey}", winInfo.ServerKey);
                return null;
            }

            // step 3 - build token
            var claims = BuildClaims(winInfo, dbMapping);
            var accessToken = GenerateAccessToken(claims);
            var existingRefreshToken = DateTime.UtcNow.AddDays(3);
            var refreshToken = await _tokenStore.CreateAsync(request.TaxCode, existingRefreshToken, ct);

            return new AuthResponse(accessToken, refreshToken, _jwt.ExpiresInSeconds);
        }

        public Task<AuthResponse?> RefreshAsync(string refreshToken, CancellationToken ct)
            => throw new NotImplementedException();

        public Task RevokeAsync(string refreshToken, CancellationToken ct)
            => _tokenStore.RevokeAsync(refreshToken, ct);

        public async Task<RegisterResult> RegisterAsync(RegisterRequest request, CancellationToken ct)
        {
            // 1) Chuẩn hoá ApplicationUser
            var user = new ApplicationUser
            {
                LoginName = request.TaxCode,
                FullName = request.CompanyName,
                Address = request.CmpnAddress,
                Password = _passwordHasher.Hash(request.Password),
                CmpnMail = request.Email,
                TaxNumber = request.TaxCode,
                Tel = request.CmpnPhone,
                ContactName = request.ContactName,
                OptionFirstJob = request.ChooseVal,
                MerchantID = request.TaxCode,
                IsWT = false,
                ServerWT = null
            };

            //Tắt để giảm độ trễ và tránh lỗi phát sinh từ TCT.
            //// 1.1) TCT login
            //var tct = await _tctLoginClient.LoginAsync(request.TaxCode, request.Password, ct);
            //if (!tct.IsSuccess)
            //{
            //    _logger.LogWarning(
            //        "[Register] TCT AUTH FAIL Tax={Tax} Code={Code} Status={Status} Msg={Msg}",
            //        request.TaxCode, tct.Code, tct.Status, tct.Message);

            //    return RegisterResult.Fail(
            //        RegisterErrorCode.TctAuthFailed,
            //        string.IsNullOrWhiteSpace(tct.Message)
            //            ? "Mã số thuế hoặc mật khẩu TCT không đúng."
            //            : $"Xác thực TCT thất bại: {tct.Message}");
            //}
            //_logger.LogInformation("[Register] TCT login OK Tax={Tax}", request.TaxCode);

            // 2) Scan MST trên hệ thống WinInvoice
            var scanResult = await _serverTaxService.GetServerLocationAsync(request.TaxCode);

            string targetServerHost;
            bool isNewServer;

            if (scanResult.IsFound)
            {
                // ============ KỊCH BẢN A ============
                targetServerHost = scanResult.FoundServers.First().ServerHost;
                isNewServer = false;

                _logger.LogInformation(
                    "[Register][A] MST {Tax} found at {Host}", request.TaxCode, targetServerHost);

                var updateRs = await _registrationRepo.UpdatePasswordTCT_UserLogin_bosUser(
                user.TaxNumber, user.Password, targetServerHost, ct);

                if (updateRs == 0)
                {
                    _logger.LogWarning(
                        "[Register][A] UpdatePasswordTCT FAIL Tax={Tax} Host={Host}",
                        request.TaxCode, targetServerHost);

                    return RegisterResult.Fail(
                        RegisterErrorCode.UpdatePasswordFailed,
                        "Không cập nhật được mật khẩu TCT trên server. Vui lòng liên hệ hỗ trợ.");
                }
            }
            else
            {
                // ============ KỊCH BẢN B ============
                targetServerHost = _config["DefaultWinInvoiceServer:ServerHost"] ?? "10.10.101.108,5172";
                isNewServer = true;

                _logger.LogInformation(
                    "[Register][B] MST {Tax} NOT found → default {Host}",
                    request.TaxCode, targetServerHost);

                var createdMaster = await _registrationRepo.CreateUserMasterAsync(user, ct);
                if (createdMaster != 1)
                {
                    _logger.LogWarning(
                        "[Register][B] MST {Tax} đã tồn tại ở master → huỷ.", request.TaxCode);

                    return RegisterResult.Fail(
                        RegisterErrorCode.MstAlreadyInMaster,
                        "Mã số thuế đã tồn tại trong hệ thống master.");
                }
            }

            // 3) Insert tblServerUser
            var checkRs = await _registrationRepo.CreateUserOnServerAsync(user, targetServerHost, ct);
            if (checkRs is null || checkRs.isSuccess != 1)
            {
                var apiCode = checkRs?.ErrorCode switch
                {
                    "DUPLICATE_MST" => RegisterErrorCode.MstAlreadyInServer,
                    "INVALID_MST" or
                    "INVALID_PASSWORD" or
                    "INVALID_EMAIL" => RegisterErrorCode.InvalidInput,
                    "SP_NO_RESULT" or
                    "CLIENT_EXCEPTION" or
                    _ => RegisterErrorCode.InsertServerFailed
                };

                return RegisterResult.Fail(
                    apiCode,
                    checkRs?.Message ?? "Không thể tạo tài khoản trên server đích.");
            }

            // 4) Trial
            try { await _registrationRepo.CreateOrderAsync(user, ct); }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "[Register] CreateOrder lỗi — bỏ qua. Tax={Tax} Host={Host}",
                    request.TaxCode, targetServerHost);
            }

            // 5) Invalidate cache
            _serverResolver.InvalidateCache(request.TaxCode);
            if (scanResult.IsFound && scanResult.FoundServers.First() is { ServerHost: { } serverKey })
                _serverResolver.InvalidateCache($"__{serverKey}");

            //// 6) Email — fire & forget
            //var emailCtx = new RegistrationEmailContext
            //{
            //    TaxCode = request.TaxCode,
            //    CompanyName = request.CompanyName,
            //    ContactName = request.ContactName,
            //    CustomerEmail = request.Email,
            //    CustomerPhone = request.CmpnPhone,
            //    CustomerAddress = request.CmpnAddress,
            //    ServerHost = targetServerHost,
            //    IsNewServer = isNewServer,
            //    RegisterAt = DateTime.Now
            //};
            //_ = Task.Run(() => _emailService.SendAllSafeAsync(emailCtx, CancellationToken.None),
            //             CancellationToken.None);

            return RegisterResult.Ok(new RegisterResponse(request.TaxCode, targetServerHost, isNewServer));
        }

        private static List<Claim> BuildClaims(WinInvoiceData winInfo, TaxServerMapping dbMapping) =>
        [
            new(JwtRegisteredClaimNames.Sub, winInfo.Taxcode!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(AppClaims.TaxCode,     winInfo.Taxcode     ?? ""),
            new(AppClaims.BosUserCode, winInfo.BosUserCode ?? ""),
            new(AppClaims.CmpnID,      winInfo.CmpnID      ?? ""),
            new(AppClaims.ServerKey,   winInfo.ServerKey   ?? ""),
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
    }
}