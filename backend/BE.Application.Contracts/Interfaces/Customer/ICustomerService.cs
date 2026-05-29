using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BE.Application.Contracts.Interfaces.Customer
{
    /// <summary>
    /// Interface service khách hàng
    /// </summary>
    public interface ICustomerService
    {
        /// <summary>
        /// Lấy khách hàng theo ID
        /// </summary>
        Task<CustomerDto> GetByIdAsync(Guid customerId);

        /// <summary>
        /// Lấy tất cả khách hàng
        /// </summary>
        Task<IEnumerable<CustomerDto>> GetAllAsync();

        /// <summary>
        /// Tạo khách hàng mới
        /// </summary>
        Task<CustomerDto> CreateAsync(CustomerCreateDto dto);

        /// <summary>
        /// Cập nhật khách hàng
        /// </summary>
        Task<CustomerDto> UpdateAsync(Guid customerId, CustomerUpdateDto dto);

        /// <summary>
        /// Xóa khách hàng
        /// </summary>
        Task<bool> DeleteAsync(Guid customerId);
    }

    /// <summary>
    /// DTO khách hàng
    /// </summary>
    public class CustomerDto
    {
        public Guid customer_id { get; set; }
        public Guid? user_id { get; set; }
        public string full_name { get; set; }
        public string phone { get; set; }
        public string email { get; set; }
        public string address { get; set; }
        public DateTime created_date { get; set; }
    }

    /// <summary>
    /// DTO tạo khách hàng
    /// </summary>
    public class CustomerCreateDto
    {
        public string full_name { get; set; }
        public string phone { get; set; }
        public string email { get; set; }
        public string address { get; set; }
    }

    /// <summary>
    /// DTO cập nhật khách hàng
    /// </summary>
    public class CustomerUpdateDto
    {
        public string full_name { get; set; }
        public string phone { get; set; }
        public string email { get; set; }
        public string address { get; set; }
    }
}