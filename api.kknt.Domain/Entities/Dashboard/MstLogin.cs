namespace api.kknt.Domain.Entities.Dashboard
{
    /// <summary>
    /// MST Login info.
    /// </summary>
    public class MstLogin
    {
        public string MstLogin_Value { get; set; } = string.Empty;
    }

    /// <summary>
    /// Thông tin đăng nhập lại TCT.
    /// </summary>
    public class DataLoginAgain
    {
        public string? UserTCT { get; set; }
        public string? MstParent { get; set; }
    }

    /// <summary>
    /// Thông tin công ty (dùng cho combobox + đếm hết hạn).
    /// </summary>
    public class CompanyInfo
    {
        public string? Mst { get; set; }
        public string? FullName { get; set; }
        public DateTime Date_Crt_Token { get; set; }
    }

    /// <summary>
    /// Đếm KH thay đổi.
    /// </summary>
    public class CountKHChange
    {
        public int CountKH { get; set; }
    }

    /// <summary>
    /// Field name (danh sách trường tùy chỉnh).
    /// </summary>
    public class FieldName
    {
        public string? FieldName_Value { get; set; }
    }
}
