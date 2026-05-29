using BE.Application.Contracts.Interfaces;
using BE.Domain.DI.Employee;
using BE.Domain.Mysql;
using BE.Domain.Repos;
using Microsoft.Extensions.DependencyInjection;

namespace BE.Application.Extensions
{
    /// <summary>
    /// Extension methods để đăng ký các dịch vụ infrastructure
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Đăng ký các infrastructure services vào DI container
        /// </summary>
        /// <param name="services">IServiceCollection</param>
        /// <param name="connectionString">Chuỗi kết nối MySQL</param>
        /// <returns>IServiceCollection đã đăng ký</returns>
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
        {
            // Đăng ký UnitOfWork
            services.AddScoped<IUnitOfWork>(_ => new UnitOfWork(connectionString));

            // Đăng ký các Repository
            services.AddScoped<IEmployeeRepo>(_ => new EmployeeRepo(connectionString));

            return services;
        }
    }
}
