using api.kknt.Application.InterfaceServices;

namespace api.kknt.API.BackgroundServices
{
    /// <summary>
    /// Background Service dọn dẹp RefreshTokens đã hết hạn / đã revoke.
    /// Chạy định kỳ mỗi 6 giờ, xoá theo batch để tránh phình to table.
    /// </summary>
    public sealed class RefreshTokenCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<RefreshTokenCleanupService> _logger;

        /// <summary>Khoảng cách giữa các lần dọn dẹp.</summary>
        private static readonly TimeSpan Interval = TimeSpan.FromHours(6);

        /// <summary>Delay khi khởi động — chờ app warm up xong.</summary>
        private static readonly TimeSpan InitialDelay = TimeSpan.FromMinutes(2);

        public RefreshTokenCleanupService(
            IServiceScopeFactory scopeFactory,
            ILogger<RefreshTokenCleanupService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "[TokenCleanup] Started. Interval={Interval}h, InitialDelay={Delay}m",
                Interval.TotalHours, InitialDelay.TotalMinutes);

            // Chờ app khởi động xong
            await Task.Delay(InitialDelay, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // App đang shutdown — thoát bình thường
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[TokenCleanup] Unexpected error. Will retry next cycle.");
                }

                await Task.Delay(Interval, stoppingToken);
            }

            _logger.LogInformation("[TokenCleanup] Stopped.");
        }

        private async Task CleanupAsync(CancellationToken ct)
        {
            _logger.LogInformation("[TokenCleanup] Running cleanup...");

            using var scope = _scopeFactory.CreateScope();
            var tokenStore = scope.ServiceProvider.GetRequiredService<IRefreshTokenStore>();

            var deleted = await tokenStore.DeleteExpiredAsync(ct);

            if (deleted > 0)
                _logger.LogInformation("[TokenCleanup] Deleted {Count} expired/revoked tokens.", deleted);
            else
                _logger.LogDebug("[TokenCleanup] No expired tokens to clean.");
        }
    }
}
