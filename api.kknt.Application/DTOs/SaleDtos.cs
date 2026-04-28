using System;
using System.Collections.Generic;

namespace api.kknt.Application.DTOs
{
    public sealed record SaleLoginRequest(string Username, string Password);

    public sealed record SaleAuthResponse(
        string AccessToken,
        string RefreshToken,
        int ExpiresIn,
        string SaleId,
        string Username,
        string FullName,
        string TokenType = "Bearer");

    public sealed record SaleOrderQuery
    {
        public string? Status { get; init; }   // "0,1,2,4"
        public DateTime? From { get; init; }
        public DateTime? To { get; init; }
        public string? Keyword { get; init; }
        public int Page { get; init; } = 1;
        public int Size { get; init; } = 50;
    }

    /// <summary>
    /// DTO cho từng đơn trong list. Dùng init-only properties (không record positional)
    /// để Dapper map theo tên column (case-insensitive). Vì vậy SP phải alias các
    /// cột có underscore/snake_case về PascalCase: CrtDate, DateEnd, CusTax,...
    /// </summary>
    public sealed class SaleOrderListItem
    {
        public string OID         { get; init; } = default!;
        public string CusTax      { get; init; } = default!;
        public string CusName     { get; init; } = default!;
        public string? CusEmail   { get; init; }
        public string? CusPhone   { get; init; }
        public string? CusAddress { get; init; }
        public DateTime CrtDate   { get; init; }
        public DateTime? DateEnd  { get; init; }
        public string? SaleID     { get; init; }
        public double? TotalAmount{ get; init; }   // SQL float → double
        public int Status         { get; init; }
        public string? StatusLabel{ get; init; }
        public bool IsUnclaimed   { get; init; }   // SQL INT 0/1 → Dapper convert
    }

    public sealed record SaleOrderListResponse(
        int TotalRows,
        int PageIndex,
        int PageSize,
        IReadOnlyList<SaleOrderListItem> Items);

    public sealed record SaleOrderDetailResponse(
        SaleOrderListItem Master,
        IReadOnlyList<OrderLineItem> Lines);

    public sealed class OrderLineItem
    {
        public int ID                { get; init; }
        public string OID            { get; init; } = default!;
        public string ItemID         { get; init; } = default!;
        public string ItemName       { get; init; } = default!;
        public double ItemPrice      { get; init; }   // SQL float
        public int ItemQtty          { get; init; }
        public string ItemUnitName   { get; init; } = default!;
        public int? UnitPerCase      { get; init; }
        public int? UnitPerSubCase   { get; init; }
    }

    public sealed record UpdateStatusRequest(int NewStatus, string? Reason);
}
