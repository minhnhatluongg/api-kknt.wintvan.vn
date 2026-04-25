namespace api.kknt.Domain.Entities.Dashboard
{
    /// <summary>
    /// Thông tin gói sử dụng: số hóa đơn còn lại + hạn sử dụng.
    /// </summary>
    public class UnitPerSubCaseInfo
    {
        public int UnitPerSubCase { get; set; }
        public DateTime Date_End { get; set; }
    }
}
