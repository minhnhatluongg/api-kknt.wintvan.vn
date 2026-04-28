namespace api.kknt.Domain.Enums
{
    /// <summary>
    /// Enum trạng thái đơn hàng — đồng bộ với <c>BosEVATbizzi.dbo.tblOrderStatus</c>.
    /// Đếm tăng dần theo tiến độ vòng đời, 9 = terminal/exception.
    /// <para>
    /// Lifecycle thường: <c>Trial (0) → Reviewing (1) → Draft (2) → Approved (3)</c>.
    /// </para>
    /// <para>
    /// Branch:
    /// <list type="bullet">
    ///   <item><description><c>Trial (0) → Expired (4)</c> — job batch tự chuyển khi date_end &lt; GETDATE.</description></item>
    ///   <item><description><c>Expired (4) → Reviewing (1)</c> — Sale follow-up khách trial hết hạn.</description></item>
    ///   <item><description><c>* → Cancelled (9)</c> — terminal, trừ Approved và Cancelled.</description></item>
    /// </list>
    /// </para>
    /// </summary>
    public enum OrderStatus
    {
        /// <summary>Đang dùng thử — đơn trial vừa tạo từ Form Register, saleID='kknt'.</summary>
        Trial      = 0,

        /// <summary>Sale đã claim đơn, đang liên hệ KH.</summary>
        Reviewing  = 1,

        /// <summary>Sale đã nhập gói chính thức, chờ duyệt.</summary>
        Draft      = 2,

        /// <summary>Đã duyệt — chính thức.</summary>
        Approved   = 3,

        /// <summary>Trial hết hạn — job batch chuyển từ Trial khi date_end qua.</summary>
        Expired    = 4,

        /// <summary>Đã huỷ — terminal.</summary>
        Cancelled  = 9
    }
}
