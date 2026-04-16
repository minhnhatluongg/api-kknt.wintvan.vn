using api.kknt.Domain.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace api.kknt.API.Middlewares;

/// <summary>
/// Middleware bắt toàn bộ unhandled exception và trả về <see cref="ApiResponse"/>
/// thay vì stack trace thô. KHÔNG bao giờ lộ thông tin nội bộ ra môi trường Production.
/// </summary>
public sealed class GlobalExceptionMiddleware
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly RequestDelegate  _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IHostEnvironment env)
    {
        _next   = next;
        _logger = logger;
        _env    = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    // ── Private ──────────────────────────────────────────────────────────────

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var (statusCode, errorCode, message) = ClassifyException(ex);

        // Log chi tiết ở phía server
        _logger.LogError(ex,
            "[{ErrorCode}] {Message} | Path={Path} | TraceId={TraceId}",
            errorCode, message,
            context.Request.Path,
            context.TraceIdentifier);

        var response = ApiResponse.Fail(errorCode, message);

        // Ở Development: đính kèm detail để debug nhanh hơn
        // Ở Production: TUYỆT ĐỐI không trả ex.Message hay StackTrace
        object payload = _env.IsDevelopment()
            ? new
            {
                response.Success,
                response.ErrorCode,
                response.Message,
                detail    = ex.Message,
                exception = ex.GetType().Name,
                traceId   = context.TraceIdentifier
            }
            : (object)new
            {
                response.Success,
                response.ErrorCode,
                response.Message,
                traceId = context.TraceIdentifier   // dùng traceId để tra log server
            };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode  = (int)statusCode;

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(payload, _jsonOptions));
    }

    /// <summary>
    /// Phân loại exception → (HTTP status, errorCode, thông báo thân thiện).
    /// Thêm case mới tại đây khi có exception type mới.
    /// </summary>
    private static (HttpStatusCode status, string errorCode, string message)
        ClassifyException(Exception ex)
    {
        var typeName = ex.GetType().Name;

        // ── SQL / Database ────────────────────────────────────────────────────
        // Dùng type name để tránh phụ thuộc package SqlClient ở tầng API
        if (typeName is "SqlException")
        {
            if (ex.Message.Contains("Could not find stored procedure"))
                return (HttpStatusCode.InternalServerError,
                        ApiErrorCodes.StoredProcedureError,
                        "Lỗi stored procedure: stored procedure không tồn tại hoặc bị lỗi cấu hình.");

            return (HttpStatusCode.InternalServerError,
                    ApiErrorCodes.DatabaseError,
                    "Lỗi truy vấn cơ sở dữ liệu. Vui lòng thử lại sau.");
        }

        return ex switch
        {
            // HTTP / External
            HttpRequestException
                => (HttpStatusCode.BadGateway,
                    ApiErrorCodes.ExternalServiceError,
                    "Không thể kết nối đến dịch vụ bên ngoài. Vui lòng thử lại sau."),

            TaskCanceledException or OperationCanceledException
                => (HttpStatusCode.RequestTimeout,
                    ApiErrorCodes.RequestTimeout,
                    "Yêu cầu bị timeout. Vui lòng thử lại."),

            // Validation / Business
            ArgumentNullException or ArgumentException
                => (HttpStatusCode.BadRequest,
                    ApiErrorCodes.InvalidInput,
                    "Dữ liệu đầu vào không hợp lệ."),

            UnauthorizedAccessException
                => (HttpStatusCode.Unauthorized,
                    ApiErrorCodes.Unauthorized,
                    "Không có quyền thực hiện thao tác này."),

            NotImplementedException
                => (HttpStatusCode.NotImplemented,
                    ApiErrorCodes.NotImplemented,
                    "Tính năng chưa được triển khai."),

            // Fallback
            _   => (HttpStatusCode.InternalServerError,
                    ApiErrorCodes.InternalServerError,
                    "Đã có lỗi xảy ra phía server. Vui lòng liên hệ quản trị viên.")
        };
    }
}
