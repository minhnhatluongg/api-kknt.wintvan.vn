namespace api.kknt.Domain.Common;

public sealed class ApiResponse<T>
{
    /// <summary>Kết quả thực thi có thành công hay không.</summary>
    public bool Success { get; init; }

    /// <summary>Mã lỗi định danh (xem <see cref="ApiErrorCodes"/>). Null nếu thành công.</summary>
    public string? ErrorCode { get; init; }

    /// <summary>Thông báo thân thiện trả về cho client.</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>Dữ liệu trả về. Null nếu có lỗi.</summary>
    public T? Data { get; init; }

    // ── Factory helpers ──────────────────────────────────────────────────────

    /// <summary>Tạo response thành công với dữ liệu.</summary>
    public static ApiResponse<T> Ok(T data, string message = "Thành công.")
        => new() { Success = true, Message = message, Data = data };

    /// <summary>Tạo response lỗi với mã lỗi và thông báo.</summary>
    public static ApiResponse<T> Fail(string errorCode, string message)
        => new() { Success = false, ErrorCode = errorCode, Message = message };
}

/// <summary>
/// Phiên bản không có Data — dùng cho các endpoint chỉ trả về status (DELETE, v.v.)
/// </summary>
public sealed class ApiResponse
{
    public bool    Success   { get; init; }
    public string? ErrorCode { get; init; }
    public string  Message   { get; init; } = string.Empty;

    public static ApiResponse Ok(string message = "Thành công.")
        => new() { Success = true, Message = message };

    public static ApiResponse Fail(string errorCode, string message)
        => new() { Success = false, ErrorCode = errorCode, Message = message };
}
