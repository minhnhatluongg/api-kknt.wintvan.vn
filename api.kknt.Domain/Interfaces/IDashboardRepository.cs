using api.kknt.Domain.Entities.Dashboard;

namespace api.kknt.Domain.Interfaces
{
    /// <summary>
    /// Repository truy vấn dữ liệu Dashboard.
    /// Tất cả methods kết nối DB qua IDbConnectionFactory, dùng Dapper.
    /// </summary>
    public interface IDashboardRepository
    {
        // ── Nhóm 1: getDashboard (mounted) ──────────────────────────────────────

        /// <summary>Lấy MST Login chính từ LoginName. Kết nối động đến serverHost.</summary>
        Task<string> GetMstLoginAsync(string loginName, string serverHost, CancellationToken ct = default);

        /// <summary>Lấy thông tin CKS token (nbcks JSON).</summary>
        Task<DataToken?> GetInfoTokenAsync(string loginName, string serverName, CancellationToken ct = default);

        /// <summary>Lấy danh sách sản phẩm (Vitax, HDDT, CKS, TVAN).</summary>
        Task<ProductGroup> GetProductItemAsync(CancellationToken ct = default);

        /// <summary>Lấy danh sách field name.</summary>
        Task<List<FieldName>> GetFieldNameAsync(string loginName, string serverName, CancellationToken ct = default);

        /// <summary>Đếm hóa đơn mua vào / bán ra / khách hàng.</summary>
        Task<CountInvoice> CountInvoiceAsync(string loginName, string serverName, CancellationToken ct = default);

        /// <summary>Lấy danh sách công ty (dùng để đếm hết hạn).</summary>
        Task<List<CompanyInfo>> GetTTUserAsync(string loginName, string serverName, CancellationToken ct = default);

        /// <summary>Lấy tin tức.</summary>
        Task<ListNews> GetNewsAsync(CancellationToken ct = default);

        /// <summary>Đếm KH thay đổi.</summary>
        Task<CountKHChange> GetKHChangeAsync(string loginName, string serverName, CancellationToken ct = default);

        /// <summary>Lấy dữ liệu PieChart hóa đơn mua vào.</summary>
        Task<List<dynamic>> GetChartPieAnalyInvoicesMVAsync(string loginName, string dateFrom, string dateTo, string serverName, CancellationToken ct = default);

        /// <summary>Lấy dữ liệu PieChart hóa đơn bán ra.</summary>
        Task<List<dynamic>> GetChartPieAnalyInvoicesBRAsync(string loginName, string dateFrom, string dateTo, string serverName, CancellationToken ct = default);

        /// <summary>Lấy danh sách đơn hàng.</summary>
        Task<List<OrderItem>> GetOrderAsync(string loginName, CancellationToken ct = default);

        /// <summary>Phân tích cảnh báo KH/NCC.</summary>
        Task<List<CustomerWarning>> GetAnalyCustomersAsync(string loginName, string serverName, CancellationToken ct = default);

        /// <summary>Phân tích cảnh báo hóa đơn bán ra.</summary>
        Task<List<AnalyInvoice>> AnalyInvoicesMainOutAsync(string loginName, string serverName, CancellationToken ct = default);

        /// <summary>Phân tích cảnh báo hóa đơn mua vào.</summary>
        Task<List<AnalyInvoice>> AnalyInvoicesMainAsync(string loginName, string serverName, CancellationToken ct = default);

        /// <summary>Kiểm tra trạng thái đăng nhập lại TCT.</summary>
        Task<DataLoginAgain?> GetDataLoginAgainAsync(string loginName, string serverName ,CancellationToken ct = default);

        /// <summary>Lấy thông tin gói (số hóa đơn + hạn).</summary>
        Task<UnitPerSubCaseInfo> GetUnitPerSubCaseByTaxCodeAsync(string loginName, CancellationToken ct = default);

        // ── Nhóm 2: getTongTienInv ──────────────────────────────────────────────

        /// <summary>Lấy tổng tiền hóa đơn (chịu thuế, thuế, thanh toán).</summary>
        Task<TotalInvoiceMoney> GetTongTienInvAsync(string searchDate, string mstCompany, CancellationToken ct = default);

        // ── Nhóm 3: getuserCombobox ─────────────────────────────────────────────

        /// <summary>Lấy danh sách công ty cho combobox (có phân trang).</summary>
        Task<object> GetUserComboboxAsync(string? term, int page, int pageSize, CancellationToken ct = default);

        // ── Nhóm 4: get_info_token (khi chọn công ty) ──────────────────────────
        // Dùng lại GetInfoTokenAsync + các method phân tích ở trên.

        // ── Nhóm 5: ChooseDate ──────────────────────────────────────────────────

        /// <summary>Chuyển đổi loại ngày thành dateFrom/dateTo cụ thể.</summary>
        Task<DateRange> ChooseDateAsync(string typeChoose, CancellationToken ct = default);

        // ── Nhóm 6: getPieChart ─────────────────────────────────────────────────
        // Dùng lại GetChartPieAnalyInvoicesMVAsync + BRAsync.

        // ── Nhóm 7: addCart ─────────────────────────────────────────────────────

        /// <summary>Thêm sản phẩm vào giỏ hàng.</summary>
        Task<AddCartResult> AddCartAsync(string loginName, AddCartRequest request, CancellationToken ct = default);
    }
}
