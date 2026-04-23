using api.kknt.Application.DTOs;
using api.kknt.Domain.Entities;
using api.kknt.Domain.Interfaces;
using api.kknt.Domain.Interfaces.DatabaseConfig;
using api.kknt.Infrastructure.Database;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Data;

namespace api.kknt.Infrastructure.Repositories
{
    public class UserRegistrationRepository : IUserRegistrationRepository
    {
        private readonly IDbConnectionFactory _dbFactory;
        private readonly MasterDbOptions _masterOpts;
        private readonly ILogger<UserRegistrationRepository> _logger;
        private const string BosConfigureDb = "bosConfigure";
        private const string BosEVATbizzi = "BosEVATbizzi";

        public UserRegistrationRepository(
            IDbConnectionFactory dbFactory,
            IOptions<MasterDbOptions> masterOpts,
            ILogger<UserRegistrationRepository> logger)
        {
            _dbFactory  = dbFactory;
            _masterOpts = masterOpts.Value;
            _logger     = logger;
        }

        public async Task<int> UpdatePasswordTCT_UserLogin_bosUser(
            string taxCode,
            string passwordHashed,
            string serverHost,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(taxCode)) throw new ArgumentException("Thiếu taxCode", nameof(taxCode));
            if (string.IsNullOrWhiteSpace(passwordHashed)) throw new ArgumentException("Thiếu password", nameof(passwordHashed));
            if (string.IsNullOrWhiteSpace(serverHost)) throw new ArgumentException("Thiếu serverHost", nameof(serverHost));

            await using var conn = await _dbFactory.CreateDynamicConnection(serverHost, BosConfigureDb, ct);
            //Gọi store trước. Nếu lỗi (SP không tồn tại, database không tồn tại, hoặc lỗi SQL khác) thì fallback về UPDATE bình thường.
            try
            {
                var p = new DynamicParameters();
                p.Add("@taxcode", taxCode);
                p.Add("@password", passwordHashed);

                var rows = await conn.ExecuteAsync(new CommandDefinition(
                    commandText: "bosConfigure..Update_passwordTCT",
                    parameters: p,
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: ct));

                _logger.LogInformation(
                    "[Register] UpdateUserLogin via SP OK Tax={Tax} Host={Host} Rows={Rows}",
                    taxCode, serverHost, rows);

                return rows < 0 ? 1 : rows;
            }
            catch (SqlException ex) when (IsProcedureNotFound(ex) || IsDatabaseMissing(ex))
            {
                _logger.LogWarning(
                    "[Register] SP Update_passwordTCT không tồn tại trên {Host} (ErrNo={Err}). Fallback inline UPDATE.",
                    serverHost, ex.Number);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex,
                    "[Register] SP Update_passwordTCT lỗi bất thường Tax={Tax} Host={Host} ErrNo={Err}. Vẫn fallback inline UPDATE.",
                    taxCode, serverHost, ex.Number);
            }
            //Fallback: UPDATE trực tiếp
            const string sql = @"
                UPDATE [bosConfigure].[dbo].[bosUser]
                   SET PasswordTCT = @Password
                 WHERE LoginName   = @TaxCode;";
            try
            {
                var rows = await conn.ExecuteAsync(new CommandDefinition(
                    commandText: sql,
                    parameters: new { TaxCode = taxCode, Password = passwordHashed },
                    commandType: CommandType.Text,
                    cancellationToken: ct));

                if (rows == 0)
                {
                    _logger.LogWarning(
                        "[Register] UpdateUserLogin inline không match user Tax={Tax} Host={Host}",
                        taxCode, serverHost);
                    return 0;
                }

                _logger.LogInformation(
                    "[Register] UpdateUserLogin via inline SQL OK Tax={Tax} Host={Host} Rows={Rows}",
                    taxCode, serverHost, rows);
                return rows;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex,
                    "[Register] UpdateUserLogin FAIL cả SP lẫn inline SQL Tax={Tax} Host={Host} ErrNo={Err}",
                    taxCode, serverHost, ex.Number);
                return 0;
            }
        }

        // ──────────────────────────────────────────────────────────────────────
        //  2) CreateOrder  →  BosOnline..Ins_OrderTrial_New
        //     Chạy trên ERP DB
        // ──────────────────────────────────────────────────────────────────────
        public async Task<CreateTrialOrderResult> CreateOrderAsync(
            ApplicationUser user,
            CancellationToken ct = default)
        {
            user.MerchantID ??= user.LoginName;
            await using var connection = await _dbFactory.CreateDefault_ERP_Async(ct: ct);
            try
            {
                var oid = Guid.NewGuid().ToString("N");

                var p = new DynamicParameters();
                p.Add("@OID", oid);
                p.Add("@cusTax", user.TaxNumber);           
                p.Add("@cusName", user.FullName);
                p.Add("@cusAddress", user.Address);
                p.Add("@cusEmail", user.CmpnMail ?? string.Empty);
                p.Add("@cusPhone", user.Tel ?? string.Empty);
                p.Add("@saleID", "kknt");
                p.Add("@trialMonths", 3);                         

                var result = await connection.QuerySingleOrDefaultAsync<CreateTrialOrderResult>(
                    new CommandDefinition(
                        "BosOnline..Ins_OrderTrial_New",
                        p,
                        commandType: CommandType.StoredProcedure,
                        cancellationToken: ct));

                if (result is null)
                {
                    _logger.LogWarning("[Register] CreateOrder NO_RESULT Tax={Tax}", user.TaxNumber);
                    return new CreateTrialOrderResult
                    {
                        isSuccess = 0,
                        Message = "SP không trả về kết quả.",
                        ErrorCode = "SP_NO_RESULT"
                    };
                }

                if (result.isSuccess == 1)
                {
                    _logger.LogInformation(
                        "[Register] CreateOrder OK Tax={Tax} OID={OID} Expiry={Expiry}",
                        user.TaxNumber, result.ExistingOID, result.ExistingExpiry);
                }
                else
                {
                    _logger.LogWarning(
                        "[Register] CreateOrder SKIPPED Tax={Tax} Code={Code} Msg={Msg} ExistingOID={OID}",
                        user.TaxNumber, result.ErrorCode, result.Message, result.ExistingOID);
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Register] CreateOrder FAIL Tax={Tax}", user.TaxNumber);
                return new CreateTrialOrderResult
                {
                    isSuccess = 0,
                    Message = ex.Message,
                    ErrorCode = "CLIENT_EXCEPTION"
                };
            }
        }

        // ──────────────────────────────────────────────────────────────────────
        //  3) CreatedUserServer  →  BosOnline..ins_ServerUser_NEW_WT
        //     Chạy trên MASTER DB, SP tự xử lý cross-server tới serverHost.
        //     Khác legacy: truyền serverHost thật (không hardcode 103.252.1.234).
        // ──────────────────────────────────────────────────────────────────────
        public async Task<CheckAccountResult?> CreateUserOnServerAsync(
    ApplicationUser user,
    string serverHost,
    CancellationToken ct = default)
        {
            await using var connection = await _dbFactory.CreateDynamicConnection(serverHost, BosEVATbizzi, ct: ct);

            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@MST", user.TaxNumber);
                parameters.Add("@Server", serverHost);
                parameters.Add("@Password", user.Password);
                parameters.Add("@MerchantID", user.MerchantID ?? "xxx");
                parameters.Add("@FullName", user.FullName);
                parameters.Add("@Address", user.Address);
                parameters.Add("@Tel", user.Tel);
                parameters.Add("@mstParent", user.TaxNumber);
                parameters.Add("@contactName", user.ContactName);
                parameters.Add("@email", user.CmpnMail);
                parameters.Add("@isWT", user.IsWT, DbType.Boolean);   // SP param là BIT
                parameters.Add("@option_register", user.OptionFirstJob);
                parameters.Add("@serverWT", user.ServerWT);

                var result = await connection.QuerySingleOrDefaultAsync<CheckAccountResult>(
                    new CommandDefinition(
                        "ins_ServerUser_NEW_WT",
                        parameters,
                        commandType: CommandType.StoredProcedure,
                        cancellationToken: ct));

                if (result is null)
                {
                    _logger.LogWarning(
                        "[Register] CreatedUserServer NO_RESULT TaxCode={TaxCode} Server={Server}",
                        user.TaxNumber, serverHost);
                    return new CheckAccountResult
                    {
                        isSuccess = 0,
                        Message = "Stored procedure không trả về kết quả.",
                        ErrorCode = "SP_NO_RESULT"
                    };
                }

                if (result.isSuccess == 1)
                {
                    _logger.LogInformation(
                        "[Register] CreatedUserServer OK TaxCode={TaxCode} Server={Server} Msg={Msg}",
                        user.TaxNumber, serverHost, result.Message);
                }
                else
                {
                    _logger.LogWarning(
                        "[Register] CreatedUserServer REJECTED TaxCode={TaxCode} Server={Server} Code={Code} Msg={Msg}",
                        user.TaxNumber, serverHost, result.ErrorCode, result.Message);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[Register] CreatedUserServer FAIL TaxCode={TaxCode} Server={Server}",
                    user.TaxNumber, serverHost);

                return new CheckAccountResult
                {
                    isSuccess = 0,
                    Message = ex.Message,
                    ErrorCode = "CLIENT_EXCEPTION"
                };
            }
        }

        //  4) CreateUserMaster  -> tblServerUser nằm ở BosEVATbizzi -> Chọc vào Server mới nhất 108 nếu là MST chưa đăng kí.
        public async Task<int> CreateUserMasterAsync(
            ApplicationUser user,
            CancellationToken ct = default)
        {
            await using var conn = await _dbFactory.CreateDefault_108_Async(ct: ct);

            const string sp = "BosEVATbizzi..ins_UserMaster_NEW_WT";

            var p = new DynamicParameters();
            p.Add("@LoginName", user.LoginName);
            p.Add("@FullName", user.FullName);
            p.Add("@Address", user.Address);
            p.Add("@Password", user.Password);
            p.Add("@cmpnMail", user.CmpnMail);
            p.Add("@TaxNumber", user.TaxNumber);
            p.Add("@Tel", user.Tel);
            p.Add("@contactName", user.ContactName);
            p.Add("@optionFirstJob", user.OptionFirstJob);
            p.Add("@MerchantID", user.MerchantID);
            p.Add("@isWT", user.IsWT);
            p.Add("@serverWT", user.ServerWT);

            var cmd = new CommandDefinition(
                commandText: sp,
                parameters: p,
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct);

            try
            {
                var result = await conn.ExecuteScalarAsync<int>(cmd);
                _logger.LogInformation(
                    "[Register] CreateUserMaster TaxCode={TaxCode} Result={Result}",
                    user.TaxNumber, result);
                return result;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex,
                    "[Register] CreateUserMaster SQL FAIL TaxCode={TaxCode}",
                    user.TaxNumber);
                return 0;
            }
        }

        #region Helpers MeaningCode SQL
        private static bool IsProcedureNotFound(SqlException ex) =>
            // 2812: Could not find stored procedure '%s'.
            ex.Number == 2812;

        private static bool IsDatabaseMissing(SqlException ex) =>
            // 911: Database '%s' does not exist.
            // 4060: Cannot open database requested by the login.
            ex.Number is 911 or 4060;

        #endregion
    }
}
