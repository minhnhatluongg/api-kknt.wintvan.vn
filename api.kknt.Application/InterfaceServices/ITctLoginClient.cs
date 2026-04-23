using api.kknt.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace api.kknt.Application.InterfaceServices
{
    public interface ITctLoginClient
    {
        Task<TctLoginResult> LoginAsync(string taxCode, string password, CancellationToken ct = default);
    }
}
