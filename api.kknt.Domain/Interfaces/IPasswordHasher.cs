namespace api.kknt.Domain.Interfaces
{
    /// <summary>
    /// Abstract hoá cách hash password khi lưu DB.
    /// Hiện tại impl = Sha1 (giữ tương thích với hệ thống cũ).
    /// </summary>
    public interface IPasswordHasher
    {
        string Hash(string plainText);
    }
}
