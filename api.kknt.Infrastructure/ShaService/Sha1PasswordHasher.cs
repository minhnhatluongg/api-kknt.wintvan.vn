using api.kknt.Domain.Interfaces;

namespace api.kknt.Infrastructure.AesEncryptionService
{
    /// <summary>
    /// Wrap Sha1 (legacy) thành IPasswordHasher để Application không phải
    /// reference thẳng Infrastructure.
    /// </summary>
    public sealed class Sha1PasswordHasher : IPasswordHasher
    {
        public string Hash(string plainText) => Sha1.Encrypt(plainText);
    }
}
