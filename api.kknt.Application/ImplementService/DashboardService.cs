using api.kknt.Application.DTOs.Dashboard;
using api.kknt.Application.InterfaceServices;
using api.kknt.Domain.Entities.Dashboard;
using api.kknt.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace api.kknt.Application.ImplementService
{
    public sealed class DashboardService : IDashboardService
    {
        private readonly IDashboardRepository _repo;
        private readonly ILogger<DashboardService> _logger;

        public DashboardService(
            IDashboardRepository repo,
            ILogger<DashboardService> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        // ══════════════════════════════════════════════════════════════════════
        //  API #1: getDashboard
        // ══════════════════════════════════════════════════════════════════════
        public async Task<DashboardResponse> GetDashboardAsync(
            string loginName, string serverHost, DateTime dateActiveCmpn,
            CancellationToken ct = default)
        {
            var response = new DashboardResponse();

            // 1. MST Login — kết nối động đến serverHost
            var mstLogin = await _repo.GetMstLoginAsync(loginName, serverHost, ct);
            loginName = mstLogin; 
            response.CmpnTax = loginName;

            // 2. Token info (CKS)
            var tokenInfo = await _repo.GetInfoTokenAsync(loginName, serverHost, ct);
            if (tokenInfo != null)
            {
                tokenInfo = CalculateTokenInfo(tokenInfo);
            }
            response.TokenInfo = tokenInfo ?? new DataToken();

            // 3. Sản phẩm
            var product = await _repo.GetProductItemAsync(ct);
            FormatProductUnitSubCase(product);
            response.Product = product;

            // 4. Field name
            response.Listfield = await _repo.GetFieldNameAsync(loginName, serverHost, ct);

            // 5. Kiểm tra hạn báo cáo thuế
            TimeSpan dateEnd = dateActiveCmpn - DateTime.Now;
            response.IsEnd = dateEnd.TotalDays < 5;
            response.DateEnd = dateActiveCmpn.ToString("dd/MM/yyyy");

            // 6. Đếm hóa đơn
            response.Countinv = await _repo.CountInvoiceAsync(loginName, serverHost, ct);

            // 7. Đếm công ty hết hạn login
            var lstCompany = await _repo.GetTTUserAsync(loginName, serverHost, ct);
            int countCompany = 0;
            foreach (var c in lstCompany)
            {
                TimeSpan diff = DateTime.Now - c.Date_Crt_Token;
                if (diff.TotalHours >= 24) countCompany++;
            }
            response.CountCompany = countCompany;

            // 8. Tin tức
            response.ListNews = await _repo.GetNewsAsync(ct);

            // 9. Hạn báo cáo thuế
            response.ReportTax = CalculateReportTax();

            // 10. KH thay đổi
            var khChange = await _repo.GetKHChangeAsync(loginName, serverHost, ct);
            response.Count_KHChange = khChange.CountKH;

            // 11. PieChart data
            var (labelHDDV, dataHDDV) = await ParsePieChartAsync(
                () => _repo.GetChartPieAnalyInvoicesMVAsync(loginName, "2021/01/01", DateTime.Now.ToString("yyyy/MM/dd"), serverHost, ct));
            var (labelHDDR, dataHDDR) = await ParsePieChartAsync(
                () => _repo.GetChartPieAnalyInvoicesBRAsync(loginName, "2021/01/01", DateTime.Now.ToString("yyyy/MM/dd"), serverHost, ct));

            response.LabelHDDV = labelHDDV;
            response.DataHDDV = dataHDDV;
            response.LabelHDDR = labelHDDR;
            response.DataHDDR = dataHDDR;

            // 12. Đơn hàng
            response.LstOrder = await _repo.GetOrderAsync(loginName, ct);

            // 13. Cảnh báo KH/NCC
            response.LstCustomer = await _repo.GetAnalyCustomersAsync(loginName, serverHost, ct);

            // 14. Cảnh báo hóa đơn
            response.AnalyInvoices_Main_OUT = await _repo.AnalyInvoicesMainOutAsync(loginName, serverHost, ct);
            response.AnalyInvoices_Main = await _repo.AnalyInvoicesMainAsync(loginName, serverHost, ct);

            // 15. Kiểm tra login lại TCT
            var dataLogin = await _repo.GetDataLoginAgainAsync(loginName, serverHost, ct);
            response.CheckMSTLogin = (dataLogin != null && dataLogin.UserTCT == null) ? 1 : 0;

            // 16. Thông tin gói (hóa đơn còn lại + hạn)
            var unitInfo = await _repo.GetUnitPerSubCaseByTaxCodeAsync(loginName, ct);
            CalculateSubscriptionStatus(response, unitInfo);

            return response;
        }

        // ══════════════════════════════════════════════════════════════════════
        //  API #2: getTongTienInv
        // ══════════════════════════════════════════════════════════════════════
        public async Task<TotalInvoiceMoneyResponse> GetTotalInvoiceMoneyAsync(
            string dateFrom, string dateTo, string mstCompany, string serverName,
            CancellationToken ct = default)
        {
            var data = await _repo.GetTongTienInvAsync(dateFrom, dateTo, mstCompany, serverName , ct);

            return new TotalInvoiceMoneyResponse
            {
                Sum_TgtcthueHDDV = ConvertToMoney(data.Sum_TgtcthueHDDV),
                Sum_TgtthueHDDV = ConvertToMoney(data.Sum_TgtthueHDDV),
                Sum_TgtttbsoHDDV = ConvertToMoney(data.Sum_TgtttbsoHDDV),
                Sum_TgtcthueHDDR = ConvertToMoney(data.Sum_TgtcthueHDDR),
                Sum_TgtthueHDDR = ConvertToMoney(data.Sum_TgtthueHDDR),
                Sum_TgtttbsoHDDR = ConvertToMoney(data.Sum_TgtttbsoHDDR),
            };
        }

        // ══════════════════════════════════════════════════════════════════════
        //  API #3: getuserCombobox
        // ══════════════════════════════════════════════════════════════════════
        public async Task<object> GetUserComboboxAsync(
            string? term, int page, int pageSize,
            CancellationToken ct = default)
        {
            return await _repo.GetUserComboboxAsync(term, page, pageSize, ct);
        }

        // ══════════════════════════════════════════════════════════════════════
        //  API #4: get_info_token (khi chọn công ty)
        // ══════════════════════════════════════════════════════════════════════
        public async Task<CompanyInfoResponse> GetCompanyInfoAsync(
            string loginName, string mstCompany, string serverName,
            CancellationToken ct = default)
        {
            var result = new CompanyInfoResponse();

            // Token info
            var tokenInfo = await _repo.GetInfoTokenAsync(mstCompany, serverName, ct);
            result.TokenInfo = tokenInfo != null ? CalculateTokenInfo(tokenInfo) : new DataToken();

            // Cảnh báo
            result.LstCustomer = await _repo.GetAnalyCustomersAsync(mstCompany, serverName, ct);
            result.AnalyInvoices_Main_OUT = await _repo.AnalyInvoicesMainOutAsync(mstCompany, serverName, ct);
            result.AnalyInvoices_Main = await _repo.AnalyInvoicesMainAsync(mstCompany, serverName, ct);

            var khChange = await _repo.GetKHChangeAsync(mstCompany, serverName, ct);
            result.Count_KHChange = khChange.CountKH;

            // PieChart
            var (labelHDDV, dataHDDV) = await ParsePieChartAsync(
                () => _repo.GetChartPieAnalyInvoicesMVAsync(mstCompany, "2021/01/01", DateTime.Now.ToString("yyyy/MM/dd"), serverName, ct));
            var (labelHDDR, dataHDDR) = await ParsePieChartAsync(
                () => _repo.GetChartPieAnalyInvoicesBRAsync(mstCompany, "2021/01/01", DateTime.Now.ToString("yyyy/MM/dd"), serverName, ct));

            result.LabelHDDV = labelHDDV;
            result.DataHDDV = dataHDDV;
            result.LabelHDDR = labelHDDR;
            result.DataHDDR = dataHDDR;

            return result;
        }

        // ══════════════════════════════════════════════════════════════════════
        //  API #5: ChooseDate
        // ══════════════════════════════════════════════════════════════════════
        public async Task<ChooseDateResponse> ChooseDateAsync(
            string typeChoose,
            CancellationToken ct = default)
        {
            var range = await _repo.ChooseDateAsync(typeChoose, ct);
            return new ChooseDateResponse
            {
                DateFrom = range.DateFrom,
                DateTo = range.DateTo,
            };
        }

        // ══════════════════════════════════════════════════════════════════════
        //  API #6: getPieChart
        // ══════════════════════════════════════════════════════════════════════
        public async Task<PieChartResponse> GetPieChartAsync(
            string loginName, string mstCompany, string dateFrom, string dateTo, string serverName,
            CancellationToken ct = default)
        {
            var effectiveMst = string.IsNullOrEmpty(mstCompany) ? loginName : mstCompany;

            var (labelHDDV, dataHDDV) = await ParsePieChartAsync(
                () => _repo.GetChartPieAnalyInvoicesMVAsync(effectiveMst, dateFrom, dateTo, serverName, ct));
            var (labelHDDR, dataHDDR) = await ParsePieChartAsync(
                () => _repo.GetChartPieAnalyInvoicesBRAsync(effectiveMst, dateFrom, dateTo, serverName, ct));

            return new PieChartResponse
            {
                LabelHDDV = labelHDDV,
                DataHDDV = dataHDDV,
                LabelHDDR = labelHDDR,
                DataHDDR = dataHDDR,
            };
        }

        // ══════════════════════════════════════════════════════════════════════
        //  API #7: addCart
        // ══════════════════════════════════════════════════════════════════════
        public async Task<AddCartResponseDto> AddCartAsync(
            string loginName, AddCartRequestDto request,
            CancellationToken ct = default)
        {
            var cartReq = new AddCartRequest
            {
                ItemID = request.ItemID,
                ItemName = request.ItemName,
                Cur_Price = request.Cur_Price,
                ItemUnitName = request.ItemUnitName,
            };
            var result = await _repo.AddCartAsync(loginName, cartReq, ct);
            return new AddCartResponseDto { Status = result.Status };
        }

        // ══════════════════════════════════════════════════════════════════════
        //  Private helpers
        // ══════════════════════════════════════════════════════════════════════

        /// <summary>Tính ngày còn lại + % progress cho CKS token.</summary>
        private DataToken CalculateTokenInfo(DataToken tokenInfo)
        {
            try
            {
                var jsonRs = JsonSerializer.Deserialize<TokenDetail>(tokenInfo.Nbcks!);
                if (jsonRs == null) return tokenInfo;

                TimeSpan totalSpan = DateTime.Parse(jsonRs.NotAfter) - DateTime.Parse(jsonRs.NotBefore);
                int totalDays = totalSpan.Days;

                TimeSpan usedSpan = DateTime.Now - DateTime.Parse(jsonRs.NotBefore);
                int usedDays = usedSpan.Days;

                int remaining = totalDays - usedDays;
                var ratio = Math.Round((float)usedDays / totalDays, 2);

                tokenInfo.RsVal = ratio < 1 ? Math.Round(ratio * 100, 2) : 100.00;
                tokenInfo.RsDay = remaining > 0 ? remaining : 0;
                tokenInfo.NotBefore = DateTime.Parse(jsonRs.NotBefore).ToString("dd/MM/yyyy");
                tokenInfo.NotAfter = DateTime.Parse(jsonRs.NotAfter).ToString("dd/MM/yyyy");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Dashboard] CalculateTokenInfo error");
            }
            return tokenInfo;
        }

        /// <summary>Tính hạn nộp báo cáo thuế tháng tiếp theo.</summary>
        private static ReportTax CalculateReportTax()
        {
            var rb = new ReportTax();
            var now = DateTime.Now;
            DateTime dateBC;

            if (now.Day <= 20)
            {
                dateBC = new DateTime(now.Year, now.Month, 20);
                int dayRS = (int)(dateBC - now).TotalDays;
                if ((dateBC - now).TotalDays > dayRS) dayRS++;
                rb.DayRS = dayRS;

                var prev = dateBC.AddMonths(-1);
                int totalDays = (dateBC - prev).Days;
                var ratio = Math.Round((float)rb.DayRS / totalDays, 2);
                rb.RsVal = ratio < 1 ? 100 - Math.Round(ratio * 100, 2) : 100.00;
            }
            else
            {
                var nextMonth = now.AddMonths(1);
                dateBC = new DateTime(nextMonth.Year, nextMonth.Month, 20);
                int dayRS = (int)(dateBC - now).TotalDays;
                if ((dateBC - now).TotalDays > dayRS) dayRS++;
                rb.DayRS = dayRS;

                var nextNext = dateBC.AddMonths(1);
                int totalDays = (nextNext - dateBC).Days;
                var ratio = Math.Round((float)rb.DayRS / totalDays, 2);
                rb.RsVal = ratio < 1 ? 100 - Math.Round(ratio * 100, 2) : 100.00;
            }
            rb.DateBC = dateBC.ToString("dd/MM/yyyy");
            return rb;
        }

        /// <summary>Format UnitPerSubCase_Value cho sản phẩm Vitax.</summary>
        private static void FormatProductUnitSubCase(ProductGroup product)
        {
            foreach (var item in product.LstVitax)
            {
                try
                {
                    item.UnitPerSubCase_Value = string.Format("{0:0,0}", item.UnitPerSubCase);
                }
                catch { /* bỏ qua lỗi format */ }
            }
        }

        /// <summary>Tính trạng thái gói sử dụng (hóa đơn còn lại, hạn).</summary>
        private static void CalculateSubscriptionStatus(DashboardResponse response, UnitPerSubCaseInfo unitInfo)
        {
            var totalUsed = response.Countinv.CountHDDV + response.Countinv.CountHDDR;
            var remaining = unitInfo.UnitPerSubCase - totalUsed;
            var checkHetHan = 0;
            var messageHoaDon = "";
            var messageNgaySuDung = "";
            var messageHetHan = "";

            if (remaining > 30)
            {
                messageHoaDon = remaining.ToString();
            }
            else
            {
                checkHetHan = 1;
            }

            if (unitInfo.Date_End < DateTime.Now.AddDays(15))
            {
                checkHetHan = 1;
            }
            else
            {
                messageNgaySuDung = unitInfo.Date_End.ToString("dd/MM/yyyy");
            }

            if (checkHetHan == 1 && remaining < 0)
            {
                messageHetHan = "Hết hạn và mời đăng ký";
            }
            else if (checkHetHan == 1)
            {
                var daysLeft = (unitInfo.Date_End - DateTime.Now).TotalDays;
                messageHetHan = $"Bạn chỉ còn {remaining} tờ / {daysLeft:F0} ngày";
            }

            response.CheckHetHan = checkHetHan;
            response.MessageHoaDon = messageHoaDon;
            response.MessageNgaySuDung = messageNgaySuDung;
            response.MessageHetHan = messageHetHan;
        }

        /// <summary>Parse DapperRow thành label + data cho PieChart.</summary>
        private async Task<(List<string> labels, List<decimal> data)> ParsePieChartAsync(
            Func<Task<List<dynamic>>> fetchFunc)
        {
            var labels = new List<string>();
            var data = new List<decimal>();

            try
            {
                var rows = await fetchFunc();
                if (rows.Count > 0)
                {
                    var raw = Convert.ToString(rows[0])
                        .Replace("{DapperRow, ", "")
                        .Replace("}", "")
                        .Replace("'", "");

                    var parts = raw.Split(',');
                    foreach (var part in parts)
                    {
                        try
                        {
                            var kv = part.Split('=');
                            data.Add(decimal.Parse(kv[1].Trim()));
                            labels.Add(kv[0].Trim());
                        }
                        catch { /* bỏ qua item lỗi */ }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Dashboard] ParsePieChart error");
            }

            return (labels, data);
        }

        /// <summary>Chuyển đổi số tiền thành dạng rút gọn (tỷ/triệu/đ).</summary>
        private static string ConvertToMoney(string rawValue)
        {
            var cleaned = rawValue.Replace(",", "").Replace(".", "");
            if (!long.TryParse(cleaned, out var value)) return "0 đ";

            if (value >= 1_000_000_000)
                return $"{(value / 1_000_000_000.0):F1} tỷ";
            if (value >= 1_000_000)
                return $"{(value / 1_000_000.0):F1} triệu";
            return $"{value:N0} đ";
        }
    }
}
