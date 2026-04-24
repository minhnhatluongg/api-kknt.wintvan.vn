using api.kknt.Application.DTOs;
using api.kknt.Domain.Entities;

namespace api.kknt.Domain.Interfaces
{
    public interface IUserRegistrationRepository
    {
        /// <summary>
        /// Kiểm tra MST có tồn tại trong <c>BosEVATbizzi..tblServerUser</c> trên server đích.
        /// Dùng inline SQL, KHÔNG dùng stored procedure.
        /// </summary>
        Task<ServerUserInfo?> FindServerUserAsync(
            string taxCode,
            string serverHost,
            CancellationToken ct = default);

        Task<int> UpdatePasswordTCT_UserLogin_bosUser(
            string taxCode,
            string passwordHashed,
            string serverHost,
            CancellationToken ct = default);

        Task<CreateTrialOrderResult> CreateOrderAsync(
            ApplicationUser user,
            string serverHost,
            CancellationToken ct = default);

        /// <summary>
        /// SP <c>BosOnline..ins_ServerUser_NEW_WT</c> — insert user vào
        /// <c>[BosEVATbizzi]..[tblServerUser]</c> thông qua linked server.
        /// Tham số <paramref name="serverHost"/> truyền trực tiếp vào SP
        /// (khác với legacy: legacy hardcode 103.252.1.234).
        /// </summary>
        Task<CheckAccountResult?> CreateUserOnServerAsync(
            ApplicationUser user,
            string serverHost,
            CancellationToken ct = default);

        /// <summary>
        /// Tạo user trong master DB — chỉ dùng cho case MST chưa có trên server nào.
        /// Trả về 1 nếu insert thành công, 0 nếu đã tồn tại.
        /// </summary>
        Task<int> CreateUserMasterAsync(
            ApplicationUser user,
            CancellationToken ct = default);
    }
}
