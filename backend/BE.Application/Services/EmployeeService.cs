using BE.Application.Contracts.Dtos;
using BE.Application.Contracts.Interfaces;
using BE.Application.Exceptions;
using BE.Application.Services.Base;
using BE.Domain.DI.Employee;
using BE.Domain.Entities;
using BE.Domain.Repos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BE.Application.Services
{
    /// <summary>
    /// Service xử lý nghiệp vụ nhân viên
    /// </summary>
    public class EmployeeService : BaseService, IEmployeeService
    {
        private readonly IEmployeeRepo _employeeRepo;

        /// <summary>
        /// Khởi tạo EmployeeService
        /// </summary>
        /// <param name="unitOfWork">UnitOfWork</param>
        /// <param name="employeeRepo">Employee repository</param>
        public EmployeeService(IUnitOfWork unitOfWork, IEmployeeRepo employeeRepo) : base(unitOfWork)
        {
            _employeeRepo = employeeRepo;
        }

        /// <summary>
        /// Lấy nhân viên theo ID
        /// </summary>
        public async Task<EmployeeDto> GetByIdAsync(Guid id)
        {
            var employee = await _employeeRepo.GetByIdAsync<EmployeeEntity>(id);
            if (employee == null)
            {
                throw new BusinessException("Nhân viên không tồn tại", 404);
            }
            return MapToDto(employee);
        }

        /// <summary>
        /// Lấy danh sách nhân viên phân trang
        /// </summary>
        public async Task<PagingResult<EmployeeDto>> GetPagingAsync(EmployeeFilterDto filter)
        {
            var columns = "employee_id, employee_code, full_name, email, phone_number, department_id";
            var sort = $"{filter.sort_field} {filter.sort_order}";
            var filters = BuildFilterExpression(filter);

            var result = await _employeeRepo.GetPaging<EmployeeEntity>(
                typeof(EmployeeEntity),
                columns,
                filter.skip,
                filter.take,
                sort,
                filters
            );

            var employeeList = result.Data.Cast<EmployeeEntity>().ToList();
            return new PagingResult<EmployeeDto>
            {
                data = MapToDtoList(employeeList),
                total = employeeList.Count
            };
        }

        /// <summary>
        /// Thêm mới nhân viên
        /// </summary>
        public async Task<EmployeeDto> CreateAsync(EmployeeEditDto dto)
        {
            // Validate: kiểm tra mã nhân viên đã tồn tại chưa
            var existing = await _employeeRepo.GetByCodeAsync(dto.employee_code);
            if (existing != null)
            {
                throw new BusinessException("Mã nhân viên đã tồn tại");
            }

            // Tạo entity
            var employee = new EmployeeEntity
            {
                employee_id = Guid.NewGuid(),
                employee_code = dto.employee_code,
                full_name = dto.full_name,
                email = dto.email,
                phone_number = dto.phone_number,
                date_of_birth = dto.date_of_birth,
                address = dto.address,
                department_id = dto.department_id,
                created_date = DateTime.UtcNow,
                created_by = "system"
            };

            await _employeeRepo.InsertAsync<EmployeeEntity>(employee);

            return MapToDto(employee);
        }

        /// <summary>
        /// Cập nhật nhân viên
        /// </summary>
        public async Task<EmployeeDto> UpdateAsync(Guid id, EmployeeEditDto dto)
        {
            var employee = await _employeeRepo.GetByIdAsync<EmployeeEntity>(id);
            if (employee == null)
            {
                throw new BusinessException("Nhân viên không tồn tại", 404);
            }

            // Check trùng mã (loại trừ chính mình)
            var existing = await _employeeRepo.GetByCodeAsync(dto.employee_code);
            if (existing != null && existing.employee_id != id)
            {
                throw new BusinessException("Mã nhân viên đã tồn tại");
            }

            // Update fields
            employee.employee_code = dto.employee_code;
            employee.full_name = dto.full_name;
            employee.email = dto.email;
            employee.phone_number = dto.phone_number;
            employee.date_of_birth = dto.date_of_birth;
            employee.address = dto.address;
            employee.department_id = dto.department_id;
            employee.modified_date = DateTime.UtcNow;
            employee.modified_by = "system";

            await _employeeRepo.UpdateAsync<EmployeeEntity>(employee,
                "employee_code,full_name,email,phone_number,date_of_birth,address,department_id,modified_date,modified_by");

            return MapToDto(employee);
        }

        /// <summary>
        /// Xóa nhân viên
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id)
        {
            var employee = await _employeeRepo.GetByIdAsync<EmployeeEntity>(id);
            if (employee == null)
            {
                throw new BusinessException("Nhân viên không tồn tại", 404);
            }

            return await _employeeRepo.DeleteAsync(employee);
        }

        /// <summary>
        /// Xây dựng biểu thức filter
        /// </summary>
        private string BuildFilterExpression(EmployeeFilterDto filter)
        {
            var expressions = new List<string>();

            if (!string.IsNullOrEmpty(filter.keyword))
            {
                expressions.Add($"(employee_code LIKE '%{filter.keyword}%' OR full_name LIKE '%{filter.keyword}%')");
            }

            if (filter.department_id.HasValue)
            {
                expressions.Add($"department_id = {filter.department_id.Value}");
            }

            return string.Join(" AND ", expressions);
        }

        /// <summary>
        /// Map entity sang DTO
        /// </summary>
        private EmployeeDto MapToDto(EmployeeEntity emp)
        {
            return new EmployeeDto
            {
                employee_id = emp.employee_id,
                employee_code = emp.employee_code,
                full_name = emp.full_name,
                email = emp.email,
                phone_number = emp.phone_number,
                date_of_birth = emp.date_of_birth,
                address = emp.address,
                department_id = emp.department_id
            };
        }

        /// <summary>
        /// Map danh sách entity sang DTO
        /// </summary>
        private List<EmployeeDto> MapToDtoList(List<EmployeeEntity> employees)
        {
            var result = new List<EmployeeDto>();
            foreach (var emp in employees)
            {
                result.Add(MapToDto(emp));
            }
            return result;
        }
    }
}
