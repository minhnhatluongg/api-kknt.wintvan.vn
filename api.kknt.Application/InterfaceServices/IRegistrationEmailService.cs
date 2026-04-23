using api.kknt.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace api.kknt.Application.InterfaceServices
{
    public interface IRegistrationEmailService
    {
        /// <summary>Gửi email chào mừng tới Khách hàng.</summary>
        Task<bool> SendCustomerAsync(RegistrationEmailContext ctx, CancellationToken ct = default);

        /// <summary>Gửi email thông báo Bộ phận Kinh doanh.</summary>
        Task<bool> SendSaleAsync(RegistrationEmailContext ctx, CancellationToken ct = default);

        /// <summary>Gửi email thông báo Bộ phận Hỗ trợ kỹ thuật.</summary>
        Task<bool> SendSupportAsync(RegistrationEmailContext ctx, CancellationToken ct = default);

        /// <summary>Gửi cả 3 loại email — không throw, log lỗi nội bộ.</summary>
        Task SendAllSafeAsync(RegistrationEmailContext ctx, CancellationToken ct = default);
    }
}
