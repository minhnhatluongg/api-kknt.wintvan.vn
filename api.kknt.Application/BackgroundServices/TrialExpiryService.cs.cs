using api.kknt.Domain.Interfaces.DatabaseConfig;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace api.kknt.Application.BackgroundServices
{
    public sealed class TrialExpiryService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<TrialExpiryService> _logger;
        private static readonly TimeSpan Interval = TimeSpan.FromHours(24);
        private static readonly TimeSpan InitialDelay = TimeSpan.FromMinutes(2);

        public TrialExpiryService(
            IServiceScopeFactory scopeFactory,
            ILogger<TrialExpiryService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "[ExpireTrial] Started. Interval={Interval}h, InitialDelay={Delay}m",
                Interval.TotalHours, InitialDelay.TotalMinutes);

            try
            {
                await Task.Delay(InitialDelay, stoppingToken);
            }
            catch (OperationCanceledException) { return; }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RunOnceAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[ExpireTrial] Unexpected error. Will retry next cycle.");
                }

                try
                {
                    await Task.Delay(Interval, stoppingToken);
                }
                catch (OperationCanceledException) { break; }
            }

            _logger.LogInformation("[ExpireTrial] Stopped.");
        }

        private async Task RunOnceAsync(CancellationToken ct)
        {
            // Tạo scope mỗi lần chạy → resolve scoped service an toàn
            using var scope = _scopeFactory.CreateScope();
            var dbFactory = scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();

            await using var conn = await dbFactory.CreateDefault_108_Async("BosEVATbizzi", ct);

            var p = new DynamicParameters();
            p.Add("@batchSize", 1000);
            p.Add("@returnAffected", dbType: DbType.Int32, direction: ParameterDirection.ReturnValue);

            // SP trả result set: isSuccess, Message, AffectedRows
            var result = await conn.QuerySingleOrDefaultAsync<ExpireTrialResult>(
                new CommandDefinition(
                    "BosEVATbizzi..Job_ExpireTrial",
                    new { batchSize = 1000 },
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: ct));

            if (result is null)
            {
                _logger.LogWarning("[ExpireTrial] SP returned NO_RESULT");
                return;
            }

            if (result.isSuccess == 1)
                _logger.LogInformation(
                    "[ExpireTrial] OK Affected={Rows} Message={Msg}",
                    result.AffectedRows, result.Message);
            else
                _logger.LogWarning(
                    "[ExpireTrial] FAIL Message={Msg}", result.Message);
        }
    }
}
