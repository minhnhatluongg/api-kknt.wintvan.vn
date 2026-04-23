using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace api.kknt.Application.DTOs
{
    public class ServerScanResultDto
    {
        /// <summary>
        /// Danh sách các Server tìm thấy MST này (để check trùng lặp)
        /// </summary>
        public List<FoundServerDto> FoundServers { get; set; } = new List<FoundServerDto>();

        /// <summary>
        /// Danh sách IP các server không phản hồi trong quá trình quét
        /// </summary>
        public List<string> UnreachableServers { get; set; } = new List<string>();

        /// <summary>
        /// Nhật ký quá trình quét (Dùng để debug hoặc hiển thị cho Admin)
        /// </summary>
        public List<string> ScanLogs { get; set; } = new List<string>();

        /// <summary>
        /// Trả về true nếu MST tồn tại ít nhất trên 1 server
        /// </summary>
        public bool IsFound => FoundServers.Count > 0;

        /// <summary>
        /// Trả về true nếu MST bị trùng lặp (nằm trên 2 server trở lên)
        /// </summary>
        public bool HasConflict => FoundServers.Count > 1;

        /// <summary>
        /// Trả về true nếu có server bị lỗi kết nối nhưng không tìm thấy MST ở các server còn lại
        /// </summary>
        public bool IsIncomplete => UnreachableServers.Count > 0 && !IsFound;
    }

    /// <summary>
    /// Thông tin server thu gọn để trả về
    /// </summary>
    public class FoundServerDto
    {
        public string ServerHost { get; set; } = string.Empty;
        public string Catalog { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        // Không nên trả Password về DTO để đảm bảo bảo mật
    }
}
