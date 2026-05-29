using System.Collections.Generic;

namespace BE.Application.Contracts.Dtos
{
    /// <summary>
    /// DTO filter phân trang cơ sở
    /// </summary>
    public class PagingFilterDto
    {
        /// <summary>
        /// Số bản ghi bỏ qua (mặc định 0)
        /// </summary>
        public int skip { get; set; } = 0;

        /// <summary>
        /// Số bản ghi cần lấy (mặc định 20)
        /// </summary>
        public int take { get; set; } = 20;

        /// <summary>
        /// Trường sắp xếp (mặc định created_date)
        /// </summary>
        public string sort_field { get; set; } = "created_date";

        /// <summary>
        /// Thứ tự sắp xếp (DESC hoặc ASC, mặc định DESC)
        /// </summary>
        public string sort_order { get; set; } = "DESC";
    }

    /// <summary>
    /// Kết quả phân trang
    /// </summary>
    /// <typeparam name="T">Kiểu dữ liệu của item</typeparam>
    public class PagingResult<T>
    {
        /// <summary>
        /// Danh sách dữ liệu
        /// </summary>
        public List<T> data { get; set; }

        /// <summary>
        /// Tổng số bản ghi
        /// </summary>
        public int total { get; set; }
    }
}
