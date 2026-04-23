using api.kknt.Domain.Entities;
using Dapper;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace api.kknt.Domain.Common
{
    public static class TaxServiceHelper
    {
        public static async Task<ServerScanResult> ScanTaxServerAsync(string taxCode, IEnumerable<TaxServerMapping> servers)
        {
            var scanResult = new ServerScanResult();
            scanResult.ScanLogs.Add($"--- Bắt đầu quét MST: {taxCode} vào lúc {DateTime.Now:HH:mm:ss} ---");

            foreach (var server in servers)
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
                            commandTimeout: 5
                        )
                    );

                    if (result != null)
                    {
                        scanResult.FoundServers.Add(server);
                        scanResult.ScanLogs.Add($"[FOUND] Tìm thấy tại Server: {server.ServerHost}");
                    }
                }
                catch (Exception ex)
                {
                    scanResult.UnreachableServers.Add(server.ServerHost);
                    scanResult.ScanLogs.Add($"[ERROR] Không thể kết nối Server: {server.ServerHost}. Chi tiết: {ex.Message}");
                    continue;
                }
            }

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
    }
}
