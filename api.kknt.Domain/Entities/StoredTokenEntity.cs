using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace api.kknt.Domain.Entities
{
    public class StoredTokenEntity
    {
        public record StoredToken(string TaxCode, DateTime ExpiresAt);
    }
}
