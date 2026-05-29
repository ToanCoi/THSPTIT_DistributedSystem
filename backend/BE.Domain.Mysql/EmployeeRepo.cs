using Dapper;
using BE.Domain.Entities;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using BE.Domain.DI.Employee;

namespace BE.Domain.Mysql
{
    /// <summary>
    /// Employee repository implementation sử dụng MySQL với Dapper
    /// </summary>
    public class EmployeeRepo : BaseRepo, IEmployeeRepo
    {
        /// <summary>
        /// Khởi tạo EmployeeRepo với chuỗi kết nối
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối MySQL</param>
        public EmployeeRepo(string connectionString) : base(connectionString)
        {
        }

        /// <summary>
        /// Lấy nhân viên theo mã nhân viên
        /// </summary>
        /// <param name="employeeCode">Mã nhân viên</param>
        /// <returns>Nhân viên được tìm thấy hoặc null</returns>
        public async Task<EmployeeEntity> GetByCodeAsync(string employeeCode)
        {
            return await GetAsync<EmployeeEntity>("*", "employee_code", employeeCode);
        }

        /// <summary>
        /// Lấy nhân viên theo email
        /// </summary>
        /// <param name="email">Địa chỉ email</param>
        /// <returns>Nhân viên được tìm thấy hoặc null</returns>
        public async Task<EmployeeEntity> GetByEmailAsync(string email)
        {
            return await GetAsync<EmployeeEntity>("*", "email", email);
        }

        /// <summary>
        /// Lấy danh sách nhân viên theo phòng ban
        /// </summary>
        /// <param name="departmentId">ID phòng ban</param>
        /// <returns>Danh sách nhân viên</returns>
        public async Task<List<EmployeeEntity>> GetByDepartmentAsync(int departmentId)
        {
            using var connection = new MySqlConnection(_connectionString);
            var sql = "SELECT * FROM employee WHERE department_id = @DepartmentId";
            var result = await connection.QueryAsync<EmployeeEntity>(sql, new { DepartmentId = departmentId });
            return result.AsList();
        }

        /// <summary>
        /// Ghi đè tên bảng cho entity employee
        /// </summary>
        /// <param name="type">Kiểu dữ liệu</param>
        /// <returns>Tên bảng</returns>
        protected override string GetTableName(Type type)
        {
            if (type == typeof(EmployeeEntity))
            {
                return "employee";
            }
            return base.GetTableName(type);
        }

        /// <summary>
        /// Ghi đè khóa chính cho entity employee
        /// </summary>
        /// <param name="type">Kiểu dữ liệu</param>
        /// <returns>Tên cột khóa chính</returns>
        protected override string GetPrimaryKey(Type type)
        {
            if (type == typeof(EmployeeEntity))
            {
                return "employee_id";
            }
            return base.GetPrimaryKey(type);
        }
    }
}