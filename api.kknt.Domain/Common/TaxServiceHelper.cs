using api.kknt.Domain.Entities;
using Dapper;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace api.kknt.Domain.Common
{
    public static class TaxServiceHelper
    {
        /// <summary>
        /// Quét song song tất cả server để tìm MST.
        /// Overall timeout 15s — tránh Cloudflare 524.
        /// </summary>
        private static readonly TimeSpan OverallTimeout = TimeSpan.FromSeconds(15);

        public static async Task<ServerScanResult> ScanTaxServerAsync(string taxCode, IEnumerable<TaxServerMapping> servers)
        {
            var foundServers = new ConcurrentBag<TaxServerMapping>();
            var unreachableServers = new ConcurrentBag<string>();
            var scanLogs = new ConcurrentBag<(DateTime Time, string Message)>();

            scanLogs.Add((DateTime.Now, $"--- Bắt đầu quét MST: {taxCode} vào lúc {DateTime.Now:HH:mm:ss} ---"));

            // Overall timeout: hủy tất cả nếu quá 15s
            using var cts = new CancellationTokenSource(OverallTimeout);

            var tasks = servers.Select(server => ScanSingleServerAsync(
                server, taxCode, foundServers, unreachableServers, scanLogs, cts.Token));

            // Chạy song song tất cả server cùng lúc
            await Task.WhenAll(tasks);

            // Build kết quả từ thread-safe collections
            var scanResult = new ServerScanResult();
            scanResult.FoundServers.AddRange(foundServers);
            scanResult.UnreachableServers.AddRange(unreachableServers);

            // Sắp xếp log theo thời gian
            foreach (var log in scanLogs.OrderBy(l => l.Time))
                scanResult.ScanLogs.Add(log.Message);

            if (scanResult.HasConflict)
            {
                var conflictIps = string.Join(", ", scanResult.FoundServers.Select(s => s.ServerHost));
                scanResult.ScanLogs.Add($"[WARNING] CẢNH BÁO: MST {taxCode} đang bị trùng lặp trên các server: {conflictIps}");
            }

            if (scanResult.UnreachableServers.Any() && !scanResult.IsFound)
            {
                scanResult.ScanLogs.Add($"[CAUTION] Lưu ý: Không tìm thấy MST, nhưng có {scanResult.UnreachableServers.Count} server không phản hồi. Kết quả có thể không chính xác.");
            }

            scanResult.ScanLogs.Add($"--- Kết thúc quét vào lúc {DateTime.Now:HH:mm:ss} ---");
            return scanResult;
        }

        private static async Task ScanSingleServerAsync(
            TaxServerMapping server,
            string taxCode,
            ConcurrentBag<TaxServerMapping> foundServers,
            ConcurrentBag<string> unreachableServers,
            ConcurrentBag<(DateTime Time, string Message)> scanLogs,
            CancellationToken ct)
        {
            try
            {
                var targetBuilder = new SqlConnectionStringBuilder
                {
                    DataSource = server.ServerHost,
                    InitialCatalog = "BosEVAT",
                    UserID = server.User,
                    Password = Sha1.Decrypt(server.Password),
                    ConnectTimeout = 3,
                    Encrypt = true,
                    TrustServerCertificate = true
                };

                using var targetConn = new SqlConnection(targetBuilder.ConnectionString);
                var result = await targetConn.QueryFirstOrDefaultAsync<dynamic>(
                    new CommandDefinition(
                        "BosEVAT..Check_Company_id",
                        parameters: new { taxcode = taxCode },
                        commandType: System.Data.CommandType.StoredProcedure,
                        commandTimeout: 5,
                        cancellationToken: ct
                    )
                );

                if (result != null)
                {
                    foundServers.Add(server);
                    scanLogs.Add((DateTime.Now, $"[FOUND] Tìm thấy tại Server: {server.ServerHost}"));
                }
            }
            catch (OperationCanceledException)
            {
                unreachableServers.Add(server.ServerHost);
                scanLogs.Add((DateTime.Now, $"[TIMEOUT] Server {server.ServerHost} bị hủy do vượt quá thời gian cho phép."));
            }
            catch (Exception ex)
            {
                unreachableServers.Add(server.ServerHost);
                scanLogs.Add((DateTime.Now, $"[ERROR] Không thể kết nối Server: {server.ServerHost}. Chi tiết: {ex.Message}"));
            }
        }
    }
}
