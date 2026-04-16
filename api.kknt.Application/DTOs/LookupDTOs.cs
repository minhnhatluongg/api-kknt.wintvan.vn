namespace api.kknt.Application.DTOs;

/// <summary>
/// Response tổng hợp cho endpoint lookup: gồm thông tin WinInvoice + DB mapping.
/// </summary>
public sealed record LookupResult(
    WinInvoiceInfo WinInvoice,
    DbMappingInfo  DbMapping);

/// <summary>Thông tin tài khoản trả về từ WinInvoice API.</summary>
public sealed record WinInvoiceInfo(
    string? ServerKey,
    string? CmpnID,
    string? UserCode);

/// <summary>Thông tin kết nối database được resolve từ Master DB.</summary>
public sealed record DbMappingInfo(
    string? Host,
    string? Database,
    string? User,
    bool    IsActive);
