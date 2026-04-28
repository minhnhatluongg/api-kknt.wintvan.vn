using api.kknt.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace api.kknt.Application.InterfaceServices
{
    public interface ISaleService
    {
        Task<SaleAuthResponse?> LoginAsync(SaleLoginRequest req, CancellationToken ct);
        Task<SaleOrderListResponse> ListOrdersAsync(string saleId, SaleOrderQuery q, CancellationToken ct);
        Task<SaleOrderDetailResponse?> GetOrderDetailAsync(string oid, CancellationToken ct);
        Task<bool> UpdateStatusAsync(string oid, int newStatus, string saleId, string? reason, CancellationToken ct);
    }
}
