using BE.Domain.Entities;
using BE.Domain.Repos;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BE.Domain.DI.Employee
{
    /// <summary>
    /// Employee repository interface
    /// </summary>
    public interface IEmployeeRepo : IBaseRepo
    {
        /// <summary>
        /// Get employee by code
        /// </summary>
        Task<EmployeeEntity> GetByCodeAsync(string employeeCode);

        /// <summary>
        /// Get employee by email
        /// </summary>
        Task<EmployeeEntity> GetByEmailAsync(string email);

        /// <summary>
        /// Get employees by department
        /// </summary>
        Task<List<EmployeeEntity>> GetByDepartmentAsync(int departmentId);
    }
}