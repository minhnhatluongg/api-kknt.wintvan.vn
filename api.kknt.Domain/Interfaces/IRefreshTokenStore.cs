using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static api.kknt.Domain.Entities.StoredTokenEntity;

namespace api.kknt.Application.InterfaceServices
{
    public interface IRefreshTokenStore
    {
        Task<string> CreateAsync(string taxCode, DateTime expiresAt, CancellationToken ct);
        Task<StoredToken?> GetAsync(string token, CancellationToken ct);
        Task RevokeAsync(string token, CancellationToken ct);
    }
}
