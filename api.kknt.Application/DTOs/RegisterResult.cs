using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static api.kknt.Application.DTOs.RegisterModel;

namespace api.kknt.Application.DTOs
{

    public enum RegisterErrorCode
    {
        None = 0,
        TctAuthFailed = 1,  // Sai MST/MK với Tổng cục Thuế
        MstAlreadyInMaster = 2,  // MST mới nhưng master đã có record trùng
        UpdatePasswordFailed = 3,  // Kịch bản A: update PasswordTCT fail
        InsertServerFailed = 4,  // Insert tblServerUser fail (SP trả 0 hoặc null)
        UnknownError = 99,
        MstAlreadyInServer = 5,
        InvalidInput = 6,
    }

    /// <summary>
    /// Trạng thái từng bước trong flow đăng ký.
    /// </summary>
    public sealed class RegisterStepLog
    {
        public string Step { get; init; } = null!;
        public string Status { get; init; } = null!; // OK, FAIL, SKIP
        public string? Detail { get; init; }
        public long ElapsedMs { get; init; }
    }

    public sealed class RegisterResult
    {
        public bool IsSuccess { get; init; }
        public RegisterResponse? Data { get; init; }
        public RegisterErrorCode ErrorCode { get; init; }
        public string? ErrorMessage { get; init; }

        /// <summary>
        /// Chi tiết từng bước đăng ký: ScanServer, CreateUser, CreateOrder, v.v.
        /// Luôn trả về trong response để debug mà không cần check DB.
        /// </summary>
        public List<RegisterStepLog> Steps { get; init; } = new();

        public static RegisterResult Ok(RegisterResponse data, List<RegisterStepLog>? steps = null) => new()
        {
            IsSuccess = true,
            Data = data,
            ErrorCode = RegisterErrorCode.None,
            Steps = steps ?? new()
        };

        public static RegisterResult Fail(RegisterErrorCode code, string message, List<RegisterStepLog>? steps = null) => new()
        {
            IsSuccess = false,
            ErrorCode = code,
            ErrorMessage = message,
            Steps = steps ?? new()
        };
    }
}
