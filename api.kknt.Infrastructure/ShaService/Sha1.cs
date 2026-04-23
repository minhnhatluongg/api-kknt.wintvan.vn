using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace api.kknt.Infrastructure.AesEncryptionService
{
    public class Sha1
    {
        public static byte[] bytes = ASCIIEncoding.ASCII.GetBytes("Nghe!Con");
        public static byte[] bytesUri = ASCIIEncoding.ASCII.GetBytes("Pojh$A9v");
        public static string Encrypt(string inpString)
        {
            DESCryptoServiceProvider cryptoProvider = new DESCryptoServiceProvider();
            MemoryStream memoryStream = new MemoryStream();
            CryptoStream cryptoStream = new CryptoStream(memoryStream, cryptoProvider.CreateEncryptor(bytes, bytes), CryptoStreamMode.Write);
            StreamWriter writer = new StreamWriter(cryptoStream);
            writer.Write(inpString);
            writer.Flush();
            cryptoStream.FlushFinalBlock();
            writer.Flush();
            return Convert.ToBase64String(memoryStream.GetBuffer(), 0, System.Convert.ToInt32(memoryStream.Length));
        }

        public static string Decrypt(string encryptedString)
        {
            if (string.IsNullOrEmpty(encryptedString))
                throw new ArgumentNullException("Null Input String");
            DESCryptoServiceProvider cryptoProvider = new DESCryptoServiceProvider();
            MemoryStream memoryStream = new MemoryStream(Convert.FromBase64String(encryptedString));
            CryptoStream cryptoStream = new CryptoStream(memoryStream, cryptoProvider.CreateDecryptor(bytes, bytes), CryptoStreamMode.Read);
            StreamReader reader = new StreamReader(cryptoStream);
            return reader.ReadToEnd();
        }
        public static string DecryptUri(string encryptedString)
        {
            if (string.IsNullOrEmpty(encryptedString))
                throw new ArgumentNullException("Null Input String");
            DESCryptoServiceProvider cryptoProvider = new DESCryptoServiceProvider();
            MemoryStream memoryStream = new MemoryStream(Convert.FromBase64String(encryptedString));
            CryptoStream cryptoStream = new CryptoStream(memoryStream, cryptoProvider.CreateDecryptor(bytesUri, bytesUri), CryptoStreamMode.Read);
            StreamReader reader = new StreamReader(cryptoStream);
            return reader.ReadToEnd();
        }
    }
}
