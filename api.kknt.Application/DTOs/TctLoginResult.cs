using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace api.kknt.Application.DTOs
{
    public record TctLoginResult(bool IsSuccess, int Code, string Status, string Message);
}
