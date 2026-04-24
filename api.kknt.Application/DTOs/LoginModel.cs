using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace api.kknt.Application.DTOs
{
    public class LoginModel
    {
        public record LoginRequest(string TaxCode, string? Password);
        public record RefreshRequest(string RefreshToken);

        public record AuthResponse(
            string AccessToken,
            string RefreshToken,
            int ExpiresIn,
            string TaxCode,
            string ServerHost,
            string? CompanyName,
            string? BosUserCode,
            string TokenType = "Bearer");

        // Claims constants
        public static class AppClaims
        {
            public const string TaxCode = "taxCode";
            public const string BosUserCode = "bosUserCode";
            public const string CmpnID = "cmpnID";
            public const string ServerKey = "serverKey";
            public const string Catalog = "catalog";
            public const string ServerHost = "serverHost";
        }
    }
}
