using BE.Application.Contracts.Dtos;
using BE.Application.Contracts.Interfaces.Customer;
using BE.Application.Exceptions;
using BE.Domain.DI.Customer;
using BE.Domain.Entities;
using BE.Domain.Repos;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BE.Application.Services.Customer
{
    /// <summary>
    /// Service xử lý nghiệp vụ khách hàng
    /// </summary>
    public class CustomerService : ICustomerService
    {
        private readonly ICustomerRepo _customerRepo;
        private readonly IBaseRepo _baseRepo;
        private readonly ILogger<CustomerService> _logger;

        public CustomerService(ICustomerRepo customerRepo, IBaseRepo baseRepo, ILogger<CustomerService> logger)
        {
            _customerRepo = customerRepo;
            _baseRepo = baseRepo;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<CustomerDto> GetByIdAsync(Guid customerId)
        {
            var customer = await _customerRepo.GetByIdAsync(customerId);
            if (customer == null)
            {
                throw new BusinessException("Không tìm thấy khách hàng", 404);
            }
            return MapToDto(customer);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<CustomerDto>> GetAllAsync()
        {
            var customers = await _customerRepo.GetAllAsync();
            var result = new List<CustomerDto>();
            foreach (var customer in customers)
            {
                result.Add(MapToDto(customer));
            }
            return result;
        }

        /// <inheritdoc />
        public async Task<PagingResult<CustomerDto>> GetAllPagingAsync(PagingFilterDto filter)
        {
            var columns = "customer_id, user_id, full_name, phone, email, address, created_date";
            var sort = $"{filter.sort_field} {filter.sort_order}";

            var pagingResult = await _baseRepo.GetPaging<CustomerEntity>(
                columns,
                filter.skip,
                filter.take,
                sort,
                null
            );

            var dtos = new List<CustomerDto>();
            if (pagingResult.Data != null)
            {
                foreach (var entity in pagingResult.Data.Cast<CustomerEntity>())
                {
                    dtos.Add(MapToDto(entity));
                }
            }

            return new PagingResult<CustomerDto>
            {
                data = dtos,
                total = dtos.Count
            };
        }

        /// <inheritdoc />
        public async Task<CustomerDto> CreateAsync(CustomerCreateDto dto)
        {
            var customer = new CustomerEntity
            {
                customer_id = Guid.NewGuid(),
                full_name = dto.full_name,
                phone = dto.phone,
                email = dto.email,
                address = dto.address,
                created_date = DateTime.UtcNow,
                created_by = "system"
            };

            await _customerRepo.InsertAsync(customer);
            _logger.LogInformation("Tạo khách hàng mới [{customer_id}]", customer.customer_id);
            return MapToDto(customer);
        }

        /// <inheritdoc />
        public async Task<CustomerDto> UpdateAsync(Guid customerId, CustomerUpdateDto dto)
        {
            var customer = await _customerRepo.GetByIdAsync(customerId);
            if (customer == null)
            {
                throw new BusinessException("Không tìm thấy khách hàng", 404);
            }

            customer.full_name = dto.full_name;
            customer.phone = dto.phone;
            customer.email = dto.email;
            customer.address = dto.address;
            customer.modified_date = DateTime.UtcNow;
            customer.modified_by = "system";

            await _customerRepo.UpdateAsync(customer);
            _logger.LogInformation("Cập nhật khách hàng [{customer_id}]", customerId);
            return MapToDto(customer);
        }

        /// <inheritdoc />
        public async Task<bool> DeleteAsync(Guid customerId)
        {
            var customer = await _customerRepo.GetByIdAsync(customerId);
            if (customer == null)
            {
                throw new BusinessException("Không tìm thấy khách hàng", 404);
            }

            var result = await _customerRepo.DeleteAsync(customerId);
            _logger.LogInformation("Xóa khách hàng [{customer_id}]", customerId);
            return result;
        }

        private CustomerDto MapToDto(CustomerEntity customer)
        {
            return new CustomerDto
            {
                customer_id = customer.customer_id,
                user_id = customer.user_id,
                full_name = customer.full_name,
                phone = customer.phone,
                email = customer.email,
                address = customer.address,
                created_date = customer.created_date
            };
        }
    }
}
