namespace api.kknt.Domain.Entities.Dashboard
{
    /// <summary>
    /// Thông tin hạn nộp báo cáo thuế tháng tiếp theo.
    /// </summary>
    public class ReportTax
    {
        public string DateBC { get; set; } = string.Empty;
        public double RsVal { get; set; }
        public int DayRS { get; set; }
    }
}
