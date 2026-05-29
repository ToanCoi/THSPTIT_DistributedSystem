using BE.Domain.Share.Entities;
using BE.Domain.Shared.Enums;
using System;

namespace BE.Domain.Entities
{
    /// <summary>
    /// Entity nhân viên với các thuộc tính snake_case cho MySQL
    /// </summary>
    public class EmployeeEntity : BaseEntity
    {
        /// <summary>
        /// ID nhân viên (GUID)
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

        /// <summary>
        /// Ngày tạo
        /// </summary>
        public DateTime? created_date { get; set; }

        /// <summary>
        /// Người tạo
        /// </summary>
        public string created_by { get; set; }

        /// <summary>
        /// Ngày sửa
        /// </summary>
        public DateTime? modified_date { get; set; }

        /// <summary>
        /// Người sửa
        /// </summary>
        public string modified_by { get; set; }
    }
}