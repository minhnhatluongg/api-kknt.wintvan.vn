using api.kknt.Application.DTOs;
using api.kknt.Application.InterfaceServices;
using api.kknt.Domain.Common;
using api.kknt.Domain.Entities;
using api.kknt.Domain.Interfaces.DatabaseConfig;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace api.kknt.Application.ImplementService
{
    public class ServerTaxCodeService : IServerTaxCode
    {
       private readonly IServerResolver _serverResolver;
        public ServerTaxCodeService(IServerResolver serverResolver)
        {
            _serverResolver = serverResolver;
        }
        public async Task<ServerScanResultDto> GetServerLocationAsync(string taxCode)
        {
            var result = await _serverResolver.CheckIPServerWithReport(taxCode);
            
            var dto = new ServerScanResultDto
            {
                UnreachableServers = result.UnreachableServers,
                ScanLogs = result.ScanLogs,
                FoundServers = result.FoundServers.Select(s => new FoundServerDto
                {
                    ServerHost = s.ServerHost,
                    Catalog = s.Catalog,
                    Name = s.TaxCode
                }).ToList()
            };
            return dto;
        }
    }
}
