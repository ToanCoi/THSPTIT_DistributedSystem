using BE.Application.Contracts.Dtos;
using System;
using System.Threading.Tasks;

namespace BE.Application.Contracts.Interfaces
{
    /// <summary>
    /// Interface service cho nhân viên
    /// </summary>
    public interface IEmployeeService
    {
        /// <summary>
        /// Lấy nhân viên theo ID
        /// </summary>
        /// <param name="id">ID nhân viên</param>
        /// <returns>Nhân viên DTO</returns>
        Task<EmployeeDto> GetByIdAsync(Guid id);

        /// <summary>
        /// Lấy danh sách nhân viên phân trang
        /// </summary>
        /// <param name="filter">Filter phân trang</param>
        /// <returns>Kết quả phân trang</returns>
        Task<PagingResult<EmployeeDto>> GetPagingAsync(EmployeeFilterDto filter);

        /// <summary>
        /// Thêm mới nhân viên
        /// </summary>
        /// <param name="dto">DTO thêm mới</param>
        /// <returns>Nhân viên vừa tạo</returns>
        Task<EmployeeDto> CreateAsync(EmployeeEditDto dto);

        /// <summary>
        /// Cập nhật nhân viên
        /// </summary>
        /// <param name="id">ID nhân viên</param>
        /// <param name="dto">DTO cập nhật</param>
        /// <returns>Nhân viên đã cập nhật</returns>
        Task<EmployeeDto> UpdateAsync(Guid id, EmployeeEditDto dto);

        /// <summary>
        /// Xóa nhân viên
        /// </summary>
        /// <param name="id">ID nhân viên</param>
        /// <returns>true nếu xóa thành công</returns>
        Task<bool> DeleteAsync(Guid id);
    }
}
