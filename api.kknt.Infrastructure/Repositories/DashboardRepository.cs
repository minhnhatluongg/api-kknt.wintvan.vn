using api.kknt.Domain.Entities.Dashboard;
using api.kknt.Domain.Interfaces;
using api.kknt.Domain.Interfaces.DatabaseConfig;
using Dapper;
using Microsoft.Extensions.Logging;
using System.Data;
using static Dapper.SqlMapper;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace api.kknt.Infrastructure.Repositories
{
    /// <summary>
    /// Repository truy vấn dữ liệu Dashboard.
    /// Tất cả methods hiện tại throw NotImplementedException — tự viết nghiệp vụ sau.
    /// </summary>
    public sealed class DashboardRepository : IDashboardRepository
    {
        private readonly IDbConnectionFactory _dbFactory;
        private readonly ILogger<DashboardRepository> _logger;
        private const string BosOnlineDb = "BosOnline";
        private const string BosEVATbizziDb = "BosEVATbizzi";

        public DashboardRepository(
            IDbConnectionFactory dbFactory,
            ILogger<DashboardRepository> logger)
        {
            _dbFactory = dbFactory;
            _logger = logger;
        }

        // ── Nhóm 1: getDashboard ────────────────────────────────────────────

        public async Task<string> GetMstLoginAsync(string loginName, string serverHost, CancellationToken ct = default)
        {
            using var conn = await _dbFactory.CreateDynamicConnection(serverHost, "BosEVATbizzi", ct);

            string sql;

            if (loginName.Contains('@'))
            {
                // LoginName là email → tìm MST từ mstParent
                sql = @"
                    SELECT TOP 1 
                        ISNULL(MST, @LoginName) AS MstLogin,
                        FullName,
                        crt_date AS CrtDate
                    FROM dbo.tblServerUser WITH (NOLOCK)
                    WHERE mstParent = @LoginName
                    ORDER BY crt_date";
            }
            else
            {
                // LoginName là MST → tìm userTCT
                sql = @"
                    SELECT TOP 1 
                        ISNULL(userTCT, @LoginName) AS MstLogin,
                        FullName,
                        crt_date AS CrtDate
                    FROM dbo.tblServerUser WITH (NOLOCK)
                    WHERE mst = @LoginName AND userTCT IS NOT NULL
                    ORDER BY crt_date DESC";
            }

            var result = await conn.QueryFirstOrDefaultAsync<MstLoginResult>(
                new CommandDefinition(
                    commandText: sql,
                    parameters: new { LoginName = loginName },
                    commandType: CommandType.Text,
                    cancellationToken: ct));

            var mstLogin = result?.MstLogin ?? loginName;

            _logger.LogInformation(
                "[Dashboard] GetMstLogin: input={Input} → resolved={Resolved} server={Server}",
                loginName, mstLogin, serverHost);

            return mstLogin;
        }

        public async Task<DataToken?> GetInfoTokenAsync(string loginName, string serverName, CancellationToken ct = default)
        {
            using var conn = await _dbFactory.CreateDynamicConnection(serverName, BosEVATbizziDb, ct);

            var result = await conn.QueryFirstOrDefaultAsync<DataToken>(
                new CommandDefinition(
                    commandText: "Getinfo_token",
                    parameters: new { nbmst = loginName },
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: ct));

            return result;
        }

        public async Task<ProductGroup> GetProductItemAsync(CancellationToken ct = default)
        {
            using var conn = await _dbFactory.CreateDefault_ERP_Async(BosOnlineDb, ct: ct);
            ProductGroup modelRs = new ProductGroup();
            string sQuery = @"BosOnline.dbo.wspProducts_Portal_Top4";
            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("@ClnID", "");
            parameters.Add("@ZoneID", "");
            parameters.Add("@RegionID", "");
            parameters.Add("@ASM", "");
            parameters.Add("@SUB", "");
            parameters.Add("@TEAM", "");
            parameters.Add("@CustomerID", "");
            parameters.Add("@MembType", "");

            var result = await conn.QueryMultipleAsync(sQuery, param: parameters, commandType: CommandType.StoredProcedure);
            modelRs.LstCKS = result.Read<ProductItem>().ToList();
            modelRs.LstTVAN = result.Read<ProductItem>().ToList();
            modelRs.LstHDDT = result.Read<ProductItem>().ToList();
            modelRs.LstVitax = result.Read<ProductItem>().ToList();

            return modelRs;
        }

        public async Task<List<FieldName>> GetFieldNameAsync(string loginName, string serverName, CancellationToken ct = default)
        {
            using var conn = await _dbFactory.CreateDynamicConnection(serverName, BosEVATbizziDb, ct);
            string sQuery = @"BosEVATBizzi..Get_CusField";

            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("@taxcode", loginName);

            var result = await conn.QueryAsync<FieldName>(sQuery, param: parameters, commandType: CommandType.StoredProcedure);
            return result.ToList();
        }

        public async Task<CountInvoice> CountInvoiceAsync(string loginName, string serverName, CancellationToken ct = default)
        {
            using var conn = await _dbFactory.CreateDynamicConnection(serverName, BosEVATbizziDb, ct);
            string sQuery = @"BosEVATbizzi..Count_Invoice";
            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("@taxcode", loginName);
            var result = await conn.QueryAsync<CountInvoice>(sQuery, param: parameters, commandType: CommandType.StoredProcedure);
            return result.FirstOrDefault();
        }

        public async Task<List<CompanyInfo>> GetTTUserAsync(string loginName, string serverName,CancellationToken ct = default)
        {
            using var conn = await _dbFactory.CreateDynamicConnection(serverName, BosEVATbizziDb, ct);
            const string sql = @"
                    SELECT *
                    FROM tblServerUser WITH (NOLOCK)
                    WHERE mstParent = @Taxcode
                      AND userTCT IS NOT NULL
                      AND (isDelete <> 1 OR isDelete IS NULL)";

            var result = await conn.QueryAsync<CompanyInfo>(
                new CommandDefinition(
                    commandText: sql,
                    parameters : new { Taxcode = loginName },
                    commandType: CommandType.Text,
                    cancellationToken: ct));
            return result.ToList();
        }

        public async Task<ListNews> GetNewsAsync(CancellationToken ct = default)
        {
            using var conn = await _dbFactory.CreateDefault_ERP_Async(BosOnlineDb, ct: ct);
            ListNews modelRs = new ListNews();
            string sQuery = @"BosOnline..get_top5_News_V1";
            DynamicParameters parameters = new DynamicParameters();
            var result = await conn.QueryMultipleAsync(sQuery, param: parameters, commandType: CommandType.StoredProcedure);
            modelRs.News = result.Read<NewsItem>().FirstOrDefault();
            modelRs.Lstnews = result.Read<NewsItem>().ToList();
            return modelRs;
        }

        public async Task<CountKHChange> GetKHChangeAsync(string loginName, string serverName, CancellationToken ct = default)
        {
            using var conn = await _dbFactory.CreateDynamicConnection(serverName, BosEVATbizziDb, ct);
            string sQuery = @"BosEVATbizzi..Get_KHChange";
            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("taxcode", loginName);
            var result = await conn.QueryAsync<CountKHChange>(sQuery, param: parameters, commandType: CommandType.StoredProcedure);
            return result.FirstOrDefault();
        }

        public async Task<List<dynamic>> GetChartPieAnalyInvoicesMVAsync(string loginName, string dateFrom, string dateTo, string serverName, CancellationToken ct = default)
        {
            using var conn = _dbFactory.CreateDynamicConnection(serverName, BosEVATbizziDb, ct).GetAwaiter().GetResult();
            string sQuery = @"BosEVATbizzi..spChartPice_AnalyInvoices";
            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("@MSTCTCQuan", loginName);
            parameters.Add("@Frm", dateFrom);
            parameters.Add("@End", dateTo);
            var result = await conn.QueryAsync(sQuery, param: parameters, commandType: CommandType.StoredProcedure);
            return result.ToList();
        }

        public async Task<List<dynamic>> GetChartPieAnalyInvoicesBRAsync(string loginName, string dateFrom, string dateTo, string serverName, CancellationToken ct = default)
        {
            using var conn = _dbFactory.CreateDynamicConnection(serverName, BosEVATbizziDb, ct).GetAwaiter().GetResult();
            string sQuery = @"BosEVATbizzi..spChartPice_AnalyInvoices_Out";
            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("@MSTCTCQuan", loginName);
            parameters.Add("@Frm", dateFrom);
            parameters.Add("@End", dateTo);
            conn.Open();
            var result = await conn.QueryAsync(sQuery, param: parameters, commandType: CommandType.StoredProcedure);
            return result.ToList();
        }

        //public async Task<List<OrderItem>> GetOrderAsync(string loginName, CancellationToken ct = default)
        //{
        //    using var connection = await _dbFactory.CreateDefault_108_Async(BosEVATbizziDb, ct: ct);
        //    string sQuery = @"Get_Order_New";
        //    DynamicParameters parameters = new DynamicParameters();
        //    parameters.Add("@taxcode", loginName); // UserCode của BosUser
        //    var result = await connection.QueryMultipleAsync(sQuery, param: parameters, commandType: CommandType.StoredProcedure);
        //    var lstOrder = result.Read<OrderItem>().ToList();
        //    var lstOrderDetail = result.Read<OrderDetail>().ToList();
        //    if (lstOrderDetail.Count > 0)
        //    {
        //        for (int i = 0; i < lstOrder.Count; i++)
        //        {
        //            for (int j = 0; j < lstOrderDetail.Count; j++)
        //            {
        //                if (lstOrderDetail[j].OID == lstOrder[i].OID)
        //                {
        //                    lstOrder[i].ItemName += lstOrderDetail[j].ItemName + "<br/>";
        //                }
        //            }
        //        }
        //    }
        //    return lstOrder;
        //}
        public async Task<List<OrderItem>> GetOrderAsync(string loginName, CancellationToken ct = default)
        {
            _logger.LogInformation("[GetOrder] START - loginName={LoginName}", loginName);

            using var connection = await _dbFactory.CreateDefault_108_Async(BosEVATbizziDb, ct: ct);
            _logger.LogInformation("[GetOrder] Connection opened - Database={Database}", connection.Database);

            string sQuery = @"Get_Order_New";
            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("@custax", loginName);

            GridReader result;
            try
            {
                result = await connection.QueryMultipleAsync(sQuery, param: parameters, commandType: CommandType.StoredProcedure);
                _logger.LogInformation("[GetOrder] QueryMultiple success");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GetOrder] QueryMultiple FAILED - loginName={LoginName}", loginName);
                throw;
            }

            List<OrderItem> lstOrder;
            List<OrderDetail> lstOrderDetail;

            try
            {
                lstOrder = result.Read<OrderItem>().ToList();
                _logger.LogInformation("[GetOrder] Read OrderItem count={Count}", lstOrder.Count);

                // Log sample để check model mapping
                if (lstOrder.Any())
                {
                    var sample = lstOrder.First();
                    _logger.LogInformation("[GetOrder] Sample OrderItem - OID={OID}, cusName={CusName}, status={Status}",
                        sample.OID, sample.CusName, sample.Status);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GetOrder] Read<OrderItem> FAILED - có thể model không match");
                throw;
            }

            try
            {
                lstOrderDetail = result.Read<OrderDetail>().ToList();
                _logger.LogInformation("[GetOrder] Read OrderDetail count={Count}", lstOrderDetail.Count);

                if (lstOrderDetail.Any())
                {
                    var sample = lstOrderDetail.First();
                    _logger.LogInformation("[GetOrder] Sample OrderDetail - OID={OID}, ItemName={ItemName}",
                        sample.OID, sample.ItemName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GetOrder] Read<OrderDetail> FAILED - có thể model không match");
                throw;
            }

            // Map detail vào order
            int mappedCount = 0;
            foreach (var order in lstOrder)
            {
                var details = lstOrderDetail.Where(d => d.OID == order.OID).ToList();
                if (!details.Any())
                {
                    _logger.LogWarning("[GetOrder] Không tìm thấy detail cho OID={OID}", order.OID);
                    continue;
                }
                order.ItemName = string.Join("<br/>", details.Select(d => d.ItemName));
                mappedCount++;
            }

            _logger.LogInformation("[GetOrder] DONE - total={Total}, mapped={Mapped}", lstOrder.Count, mappedCount);
            return lstOrder;
        }

        public async Task<List<CustomerWarning>> GetAnalyCustomersAsync(string loginName, string serverName, CancellationToken ct = default)
        {
            using var conn = _dbFactory.CreateDynamicConnection(serverName, BosEVATbizziDb, ct).GetAwaiter().GetResult();
            string sQuery = @"BosEVATbizzi..spRRVT_AnalyCustomers_Main";
            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("@MSTCTCQuan", loginName);
            var result = await conn.QueryMultipleAsync(sQuery, param: parameters, commandType: CommandType.StoredProcedure);
            var lstCustomerWarning = result.Read<CustomerWarning>().ToList();
            return lstCustomerWarning;
        }

        public async Task<List<AnalyInvoice>> AnalyInvoicesMainOutAsync(string loginName, string serverName, CancellationToken ct = default)
        {
            using var conn = _dbFactory.CreateDynamicConnection(serverName, BosEVATbizziDb, ct).GetAwaiter().GetResult();
            string sQuery = @"BosEVATbizzi..spRRVT_AnalyInvoices_Main_OUT";
            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("@MSTCTCQuan", loginName);
            var result = await conn.QueryAsync<AnalyInvoice>(sQuery, param: parameters, commandType: CommandType.StoredProcedure);
            return result.ToList();
        }

        public async Task<List<AnalyInvoice>> AnalyInvoicesMainAsync(string loginName, string serverName, CancellationToken ct = default)
        {
            using var conn = _dbFactory.CreateDynamicConnection(serverName, BosEVATbizziDb, ct).GetAwaiter().GetResult();
            string sQuery = @"BosEVATbizzi..spRRVT_AnalyInvoices_Main";
            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("@MSTCTCQuan", loginName);
            var result = await conn.QueryAsync<AnalyInvoice>(sQuery, param: parameters, commandType: CommandType.StoredProcedure);
            return result.ToList();
        }

        public async Task<DataLoginAgain?> GetDataLoginAgainAsync(string loginName, string serverName, CancellationToken ct = default)
        {
            using var connection = await _dbFactory.CreateDynamicConnection(serverName, BosEVATbizziDb, ct);
            const string query = @"
                        SELECT TOP (1) 
                            MST, 
                            mstParent, 
                            userTCT 
                        FROM tblServerUser WITH (NOLOCK) 
                        WHERE mstParent = @loginName 
                        ORDER BY crt_date DESC";
            var result = await connection.QueryFirstOrDefaultAsync<DataLoginAgain>(
                    new CommandDefinition(query, new { loginName }, cancellationToken: ct));
            return result;
        }

        public async Task<UnitPerSubCaseInfo> GetUnitPerSubCaseByTaxCodeAsync(string loginName, CancellationToken ct = default)
        {
            using var conn = await _dbFactory.CreateDefault_108_Async(BosEVATbizziDb, ct: ct);
            string sQuery = @"BosEVATbizzi..Get_UnitPerSubCase_ByTaxCode";
            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("@taxcode", loginName);
            var result = conn.QueryAsync<UnitPerSubCaseInfo>(sQuery, param: parameters, commandType: CommandType.StoredProcedure).GetAwaiter().GetResult();
            return result.FirstOrDefault();
        }

        // ── Nhóm 2: getTongTienInv ──────────────────────────────────────────

        public Task<TotalInvoiceMoney> GetTongTienInvAsync(string searchDate, string mstCompany, CancellationToken ct = default)
            => throw new NotImplementedException("TODO: Gọi SP getTongTienInv");

        // ── Nhóm 3: getuserCombobox ─────────────────────────────────────────

        public Task<object> GetUserComboboxAsync(string? term, int page, int pageSize, CancellationToken ct = default)
            => throw new NotImplementedException("TODO: Gọi SP getuserCombobox");

        // ── Nhóm 5: ChooseDate ──────────────────────────────────────────────

        public Task<DateRange> ChooseDateAsync(string typeChoose, CancellationToken ct = default)
            => throw new NotImplementedException("TODO: Logic tính dateFrom/dateTo theo typeChoose");

        // ── Nhóm 7: addCart ─────────────────────────────────────────────────

        public Task<AddCartResult> AddCartAsync(string loginName, AddCartRequest request, CancellationToken ct = default)
            => throw new NotImplementedException("TODO: Gọi SP addCart");
    }
}
