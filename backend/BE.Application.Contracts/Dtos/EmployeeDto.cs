using System;

namespace BE.Application.Contracts.Dtos
{
    /// <summary>
    /// DTO xem thông tin nhân viên
    /// </summary>
    public class EmployeeDto
    {
        /// <summary>
        /// ID nhân viên
        /// </summary>
        public Guid employee_id { get; set; }

        /// <summary>
        /// Mã nhân viên
        /// </summary>
        public string employee_code { get; set; }

        /// <summary>
        /// Họ và tên đầy đủ
        /// </summary>
        public string full_name { get; set; }

        /// <summary>
        /// Địa chỉ email
        /// </summary>
        public string email { get; set; }

        /// <summary>
        /// Số điện thoại
        /// </summary>
        public string phone_number { get; set; }

        /// <summary>
        /// Ngày sinh
        /// </summary>
        public DateTime? date_of_birth { get; set; }

        /// <summary>
        /// Địa chỉ
        /// </summary>
        public string address { get; set; }

        /// <summary>
        /// ID phòng ban
        /// </summary>
        public int? department_id { get; set; }
    }

    /// <summary>
    /// DTO thêm/sửa nhân viên
    /// </summary>
    public class EmployeeEditDto
    {
        /// <summary>
        /// Mã nhân viên
        /// </summary>
        public string employee_code { get; set; }

        /// <summary>
        /// Họ và tên đầy đủ
        /// </summary>
        public string full_name { get; set; }

        /// <summary>
        /// Địa chỉ email
        /// </summary>
        public string email { get; set; }

        /// <summary>
        /// Số điện thoại
        /// </summary>
        public string phone_number { get; set; }

        /// <summary>
        /// Ngày sinh
        /// </summary>
        public DateTime? date_of_birth { get; set; }

        /// <summary>
        /// Địa chỉ
        /// </summary>
        public string address { get; set; }

        /// <summary>
        /// ID phòng ban
        /// </summary>
        public int? department_id { get; set; }
    }

    /// <summary>
    /// Filter nhân viên cho phân trang
    /// </summary>
    public class EmployeeFilterDto : PagingFilterDto
    {
        /// <summary>
        /// Từ khóa tìm kiếm (mã hoặc tên)
        /// </summary>
        public string keyword { get; set; }

        /// <summary>
        /// ID phòng ban
        /// </summary>
        public int? department_id { get; set; }
    }
}
