using api.kknt.Application.DTOs.Dashboard;
using api.kknt.Domain.Entities.Dashboard;

namespace api.kknt.Application.InterfaceServices
{
    /// <summary>
    /// Service xử lý business logic cho Dashboard.
    /// </summary>
    public interface IDashboardService
    {
        /// <summary>API #1: Lấy toàn bộ dữ liệu dashboard.</summary>
        Task<DashboardResponse> GetDashboardAsync(string loginName, string serverHost, DateTime dateActiveCmpn, CancellationToken ct = default);

        /// <summary>API #2: Lấy tổng tiền hóa đơn.</summary>
        Task<TotalInvoiceMoneyResponse> GetTotalInvoiceMoneyAsync(string dateFrom, string dateTo, string mstCompany, CancellationToken ct = default);

        /// <summary>API #3: Lấy DS công ty cho combobox.</summary>
        Task<object> GetUserComboboxAsync(string? term, int page, int pageSize, CancellationToken ct = default);

        /// <summary>API #4: Lấy data khi chọn công ty.</summary>
        Task<CompanyInfoResponse> GetCompanyInfoAsync(string loginName, string mstCompany, string serverName, CancellationToken ct = default);

        /// <summary>API #5: Chuyển đổi loại ngày → dateFrom/dateTo.</summary>
        Task<ChooseDateResponse> ChooseDateAsync(string typeChoose, CancellationToken ct = default);

        /// <summary>API #6: Lấy PieChart data.</summary>
        Task<PieChartResponse> GetPieChartAsync(string loginName, string mstCompany, string dateFrom, string dateTo, string serverName, CancellationToken ct = default);

        /// <summary>API #7: Thêm sản phẩm vào giỏ hàng.</summary>
        Task<AddCartResponseDto> AddCartAsync(string loginName, AddCartRequestDto request, CancellationToken ct = default);
    }
}
