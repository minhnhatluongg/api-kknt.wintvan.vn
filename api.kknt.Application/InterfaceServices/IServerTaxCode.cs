using api.kknt.Application.DTOs;
using api.kknt.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace api.kknt.Application.InterfaceServices
{
    public interface IServerTaxCode
    {
        Task<ServerScanResultDto> GetServerLocationAsync(string taxCode);
    }
}
