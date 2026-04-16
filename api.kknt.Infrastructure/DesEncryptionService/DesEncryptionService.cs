using api.kknt.Domain.Interfaces.DatabaseConfig;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Threading.Tasks;

namespace api.kknt.Infrastructure.AesEncryptionService
{
    public class DesEncryptionService : IEncryptionService
    {
        public string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return string.Empty;
            try
            {
                return Sha1.Decrypt(cipherText);
            }
            catch
            {
                return cipherText;
            }
        }
        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return string.Empty;
            return Sha1.Encrypt(plainText);
        }
    }
}
