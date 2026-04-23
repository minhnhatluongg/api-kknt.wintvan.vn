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

    public sealed class RegisterResult
    {
        public bool IsSuccess { get; init; }
        public RegisterResponse? Data { get; init; }
        public RegisterErrorCode ErrorCode { get; init; }
        public string? ErrorMessage { get; init; }

        public static RegisterResult Ok(RegisterResponse data) => new()
        {
            IsSuccess = true,
            Data = data,
            ErrorCode = RegisterErrorCode.None
        };

        public static RegisterResult Fail(RegisterErrorCode code, string message) => new()
        {
            IsSuccess = false,
            ErrorCode = code,
            ErrorMessage = message
        };
    }
}
