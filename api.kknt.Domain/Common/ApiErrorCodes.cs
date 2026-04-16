namespace api.kknt.Domain.Common;

/// <summary>
/// Danh sách mã lỗi chuẩn của hệ thống.
/// Dùng làm <c>errorCode</c> trong <see cref="ApiResponse{T}"/>.
/// Client dựa vào đây để hiển thị thông báo tương ứng.
/// </summary>
public static class ApiErrorCodes
{
    // ── Validation ───────────────────────────────────────────────────────────
    /// <summary>Dữ liệu đầu vào không hợp lệ.</summary>
    public const string InvalidInput        = "INVALID_INPUT";

    /// <summary>Thiếu tham số bắt buộc.</summary>
    public const string MissingParameter    = "MISSING_PARAMETER";

    // ── Authentication / Authorization ───────────────────────────────────────
    /// <summary>Sai thông tin đăng nhập (taxCode / password).</summary>
    public const string InvalidCredentials  = "INVALID_CREDENTIALS";

    /// <summary>Token hết hạn hoặc không hợp lệ.</summary>
    public const string Unauthorized        = "UNAUTHORIZED";

    /// <summary>Không có quyền thực hiện thao tác.</summary>
    public const string Forbidden           = "FORBIDDEN";

    // ── Resource ─────────────────────────────────────────────────────────────
    /// <summary>Không tìm thấy tài nguyên yêu cầu.</summary>
    public const string NotFound            = "NOT_FOUND";

    /// <summary>Tài nguyên đã tồn tại (conflict).</summary>
    public const string AlreadyExists       = "ALREADY_EXISTS";

    // ── External Services ────────────────────────────────────────────────────
    /// <summary>WinInvoice không trả về dữ liệu hoặc xác thực thất bại.</summary>
    public const string WinInvoiceError     = "WIN_INVOICE_ERROR";

    /// <summary>Không tìm thấy server mapping trong database.</summary>
    public const string ServerMappingNotFound = "SERVER_MAPPING_NOT_FOUND";

    /// <summary>Lỗi kết nối đến dịch vụ bên ngoài.</summary>
    public const string ExternalServiceError = "EXTERNAL_SERVICE_ERROR";

    // ── Database ─────────────────────────────────────────────────────────────
    /// <summary>Lỗi truy vấn / kết nối database.</summary>
    public const string DatabaseError       = "DATABASE_ERROR";

    /// <summary>Stored procedure không tồn tại hoặc bị lỗi.</summary>
    public const string StoredProcedureError = "STORED_PROCEDURE_ERROR";

    // ── Server ───────────────────────────────────────────────────────────────
    /// <summary>Lỗi không xác định phía server.</summary>
    public const string InternalServerError = "INTERNAL_SERVER_ERROR";

    /// <summary>Tính năng chưa được triển khai.</summary>
    public const string NotImplemented      = "NOT_IMPLEMENTED";

    /// <summary>Yêu cầu bị timeout.</summary>
    public const string RequestTimeout      = "REQUEST_TIMEOUT";
    
    /// <summary>Token không hợp lệ hoặc đã hết hạn.</summary>
    public const string InvalidToken = "INVALID_TOKEN";
}
