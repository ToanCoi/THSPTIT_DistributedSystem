using BE.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BE.Domain.DI.Customer
{
    /// <summary>
    /// Interface repository khách hàng
    /// </summary>
    public interface ICustomerRepo
    {
        /// <summary>
        /// Lấy khách hàng theo ID
        /// </summary>
        Task<CustomerEntity> GetByIdAsync(Guid customerId);

        /// <summary>
        /// Lấy tất cả khách hàng
        /// </summary>
        Task<IEnumerable<CustomerEntity>> GetAllAsync();

        /// <summary>
        /// Thêm khách hàng mới
        /// </summary>
        Task<bool> InsertAsync(CustomerEntity customer);

        /// <summary>
        /// Cập nhật khách hàng
        /// </summary>
        Task<bool> UpdateAsync(CustomerEntity customer);

        /// <summary>
        /// Xóa khách hàng
        /// </summary>
        Task<bool> DeleteAsync(Guid customerId);
    }
}