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
        private readonly ILoginCacheRepository _loginCache;
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
            ILoginCacheRepository loginCache,
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
            _loginCache = loginCache;
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
            // ── Step 1: Tìm server — cache-first, scan fallback ──
            var serverHost = await _loginCache.GetCachedServerAsync(request.TaxCode, ct);
            bool fromCache = serverHost != null;

            if (serverHost == null)
            {
                // Cache miss → scan toàn bộ servers
                var scanResult = await _serverTaxService.GetServerLocationAsync(request.TaxCode);
                if (!scanResult.IsFound)
                {
                    _logger.LogWarning(
                        "[Login] MST {TaxCode} not found on any server. Unreachable={Unreachable}",
                        request.TaxCode,
                        string.Join(", ", scanResult.UnreachableServers));
                    return null;
                }

                serverHost = scanResult.FoundServers.First().ServerHost;
                _logger.LogInformation(
                    "[Login] MST {TaxCode} found via SCAN on {ServerHost}",
                    request.TaxCode, serverHost);
            }

            // ── Step 2: Build connection → query tblServerUser (inline SQL) ──
            var serverUser = await _registrationRepo.FindServerUserAsync(
                request.TaxCode, serverHost, ct);

            if (serverUser == null)
            {
                // Nếu cache hit nhưng user không còn trên server đó → cache stale
                // Thử scan lại 1 lần
                if (fromCache)
                {
                    _logger.LogWarning(
                        "[Login] Cache stale for {TaxCode} on {ServerHost}. Re-scanning...",
                        request.TaxCode, serverHost);

                    var reScan = await _serverTaxService.GetServerLocationAsync(request.TaxCode);
                    if (reScan.IsFound)
                    {
                        serverHost = reScan.FoundServers.First().ServerHost;
                        serverUser = await _registrationRepo.FindServerUserAsync(
                            request.TaxCode, serverHost, ct);
                    }
                }

                if (serverUser == null)
                {
                    _logger.LogWarning(
                        "[Login] TaxCode {TaxCode} not found in tblServerUser",
                        request.TaxCode);
                    return null;
                }
            }

            // ── Step 3: Verify password ──
            var inputHash = _passwordHasher.Hash(request.Password ?? "");
            if (!string.Equals(inputHash, serverUser.Password, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning(
                    "[Login] Password mismatch for {TaxCode} on {ServerHost}",
                    request.TaxCode, serverHost);
                return null;
            }

            // ── Step 4: Upsert cache (fire-and-forget, không block login) ──
            _ = _loginCache.UpsertAsync(request.TaxCode, serverHost, ct);

            // ── Step 5: Build JWT claims + tokens ──
            var claims = BuildClaims(request.TaxCode, serverHost, serverUser);
            var accessToken = GenerateAccessToken(claims);
            var refreshExpiry = DateTime.UtcNow.AddDays(_jwt.RefreshExpiryDays);
            var refreshToken = await _tokenStore.CreateAsync(request.TaxCode, refreshExpiry, ct);

            _logger.LogInformation(
                "[Login] SUCCESS TaxCode={TaxCode} Server={ServerHost} FromCache={FromCache}",
                request.TaxCode, serverHost, fromCache);

            return new AuthResponse(
                AccessToken: accessToken,
                RefreshToken: refreshToken,
                ExpiresIn: _jwt.ExpiresInSeconds,
                TaxCode: request.TaxCode,
                ServerHost: serverHost,
                CompanyName: serverUser.FullName,
                BosUserCode: serverUser.MerchantID);
        }

        public Task<AuthResponse?> RefreshAsync(string refreshToken, CancellationToken ct)
            => throw new NotImplementedException();

        public Task RevokeAsync(string refreshToken, CancellationToken ct)
            => _tokenStore.RevokeAsync(refreshToken, ct);

        public async Task<RegisterResult> RegisterAsync(RegisterRequest request, CancellationToken ct)
        {
            var steps = new List<RegisterStepLog>();
            var sw = System.Diagnostics.Stopwatch.StartNew();

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

            // ── STEP 1: Scan MST ──
            sw.Restart();
            ServerScanResultDto scanResult;
            try
            {
                scanResult = await _serverTaxService.GetServerLocationAsync(request.TaxCode);
                steps.Add(new RegisterStepLog
                {
                    Step = "ScanServer",
                    Status = scanResult.IsFound ? "OK" : "NOT_FOUND",
                    Detail = scanResult.IsFound
                        ? $"Found at {scanResult.FoundServers.First().ServerHost}"
                        : "MST chưa tồn tại → dùng default server",
                    ElapsedMs = sw.ElapsedMilliseconds
                });
            }
            catch (Exception ex)
            {
                steps.Add(new RegisterStepLog
                {
                    Step = "ScanServer",
                    Status = "FAIL",
                    Detail = ex.Message,
                    ElapsedMs = sw.ElapsedMilliseconds
                });
                _logger.LogError(ex, "[Register] ScanServer FAIL Tax={Tax}", request.TaxCode);
                return RegisterResult.Fail(RegisterErrorCode.UnknownError,
                    "Không thể quét hệ thống server. Vui lòng thử lại.", steps);
            }

            string targetServerHost;
            bool isNewServer;

            if (scanResult.IsFound)
            {
                // ============ KỊCH BẢN A ============
                targetServerHost = scanResult.FoundServers.First().ServerHost;
                isNewServer = false;

                _logger.LogInformation(
                    "[Register][A] MST {Tax} found at {Host}", request.TaxCode, targetServerHost);

                // ── STEP 2A: Update Password ──
                sw.Restart();
                var updateRs = await _registrationRepo.UpdatePasswordTCT_UserLogin_bosUser(
                user.TaxNumber, user.Password, targetServerHost, ct);

                if (updateRs == 0)
                {
                    steps.Add(new RegisterStepLog
                    {
                        Step = "UpdatePassword",
                        Status = "FAIL",
                        Detail = $"Host={targetServerHost}, affected=0",
                        ElapsedMs = sw.ElapsedMilliseconds
                    });
                    _logger.LogWarning(
                        "[Register][A] UpdatePasswordTCT FAIL Tax={Tax} Host={Host}",
                        request.TaxCode, targetServerHost);

                    return RegisterResult.Fail(
                        RegisterErrorCode.UpdatePasswordFailed,
                        "Không cập nhật được mật khẩu TCT trên server. Vui lòng liên hệ hỗ trợ.", steps);
                }
                steps.Add(new RegisterStepLog
                {
                    Step = "UpdatePassword",
                    Status = "OK",
                    Detail = $"Host={targetServerHost}",
                    ElapsedMs = sw.ElapsedMilliseconds
                });
            }
            else
            {
                // ============ KỊCH BẢN B ============
                targetServerHost = _config["DefaultWinInvoiceServer:ServerHost"] ?? "10.10.101.108,5172";
                isNewServer = true;

                _logger.LogInformation(
                    "[Register][B] MST {Tax} NOT found → default {Host}",
                    request.TaxCode, targetServerHost);

                // ── STEP 2B: Create Master ──
                sw.Restart();
                var createdMaster = await _registrationRepo.CreateUserMasterAsync(user, ct);
                if (createdMaster != 1)
                {
                    steps.Add(new RegisterStepLog
                    {
                        Step = "CreateMaster",
                        Status = "FAIL",
                        Detail = "MST đã tồn tại trong master",
                        ElapsedMs = sw.ElapsedMilliseconds
                    });
                    _logger.LogWarning(
                        "[Register][B] MST {Tax} đã tồn tại ở master → huỷ.", request.TaxCode);

                    return RegisterResult.Fail(
                        RegisterErrorCode.MstAlreadyInMaster,
                        "Mã số thuế đã tồn tại trong hệ thống master.", steps);
                }
                steps.Add(new RegisterStepLog
                {
                    Step = "CreateMaster",
                    Status = "OK",
                    Detail = $"Default host={targetServerHost}",
                    ElapsedMs = sw.ElapsedMilliseconds
                });
            }

            // ── STEP 3: Insert tblServerUser ──
            sw.Restart();
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
                steps.Add(new RegisterStepLog
                {
                    Step = "CreateUserOnServer",
                    Status = "FAIL",
                    Detail = $"Code={checkRs?.ErrorCode}, Msg={checkRs?.Message}",
                    ElapsedMs = sw.ElapsedMilliseconds
                });

                return RegisterResult.Fail(
                    apiCode,
                    checkRs?.Message ?? "Không thể tạo tài khoản trên server đích.", steps);
            }
            steps.Add(new RegisterStepLog
            {
                Step = "CreateUserOnServer",
                Status = "OK",
                Detail = $"Host={targetServerHost}",
                ElapsedMs = sw.ElapsedMilliseconds
            });

            // ── STEP 4: Trial Order ──
            sw.Restart();
            try
            {
                var orderResult = await _registrationRepo.CreateOrderAsync(user, targetServerHost, ct);
                steps.Add(new RegisterStepLog
                {
                    Step = "CreateTrialOrder",
                    Status = orderResult.isSuccess == 1 ? "OK" : "FAIL",
                    Detail = orderResult.isSuccess == 1
                        ? $"OID={orderResult.ExistingOID}, Expiry={orderResult.ExistingExpiry}"
                        : $"Code={orderResult.ErrorCode}, Msg={orderResult.Message}",
                    ElapsedMs = sw.ElapsedMilliseconds
                });
            }
            catch (Exception ex)
            {
                steps.Add(new RegisterStepLog
                {
                    Step = "CreateTrialOrder",
                    Status = "FAIL",
                    Detail = ex.Message,
                    ElapsedMs = sw.ElapsedMilliseconds
                });
                _logger.LogWarning(ex,
                    "[Register] CreateOrder lỗi — bỏ qua. Tax={Tax} Host={Host}",
                    request.TaxCode, targetServerHost);
            }

            // 5) Invalidate cache
            _serverResolver.InvalidateCache(request.TaxCode);
            if (scanResult.IsFound && scanResult.FoundServers.First() is { ServerHost: { } serverKey })
                _serverResolver.InvalidateCache($"__{serverKey}");

            _logger.LogInformation("[Register] COMPLETED Tax={Tax} Host={Host} Steps={@Steps}",
                request.TaxCode, targetServerHost, steps);

            return RegisterResult.Ok(new RegisterResponse(request.TaxCode, targetServerHost, isNewServer), steps);
        }

        private static List<Claim> BuildClaims(
            string taxCode,
            string serverHost,
            ServerUserInfo serverUser) =>
        [
            new(JwtRegisteredClaimNames.Sub, taxCode),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(AppClaims.TaxCode,     taxCode),
            new(AppClaims.BosUserCode, serverUser.MerchantID ?? ""),
            new(AppClaims.CmpnID,      serverUser.MST),
            new(AppClaims.ServerHost,  serverHost),
            new(AppClaims.Catalog,     "BosEVATbizzi"),
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