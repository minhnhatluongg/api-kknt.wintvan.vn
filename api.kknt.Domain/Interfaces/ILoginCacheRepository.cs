namespace api.kknt.Domain.Interfaces
{
    /// <summary>
    /// Cache MST → ServerHost trên Master DB.
    /// Tránh scan toàn bộ servers mỗi lần login.
    /// </summary>
    public interface ILoginCacheRepository
    {
        /// <summary>
        /// Lấy ServerHost đã cache cho MST này.
        /// Trả về null nếu chưa cache.
        /// </summary>
        Task<string?> GetCachedServerAsync(string taxCode, CancellationToken ct = default);

        /// <summary>
        /// Upsert cache: lưu hoặc cập nhật MST → ServerHost + LastLoginAt.
        /// </summary>
        Task UpsertAsync(string taxCode, string serverHost, CancellationToken ct = default);
    }
}
