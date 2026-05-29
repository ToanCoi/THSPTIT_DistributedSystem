using BE.Domain.Repos;

namespace BE.Application.Services.Base
{
    /// <summary>
    /// Service cơ sở cho các service nghiệp vụ
    /// </summary>
    public abstract class BaseService
    {
        /// <summary>
        /// Đối tượng UnitOfWork để quản lý transaction
        /// </summary>
        protected readonly IUnitOfWork UnitOfWork;

        /// <summary>
        /// Khởi tạo BaseService với UnitOfWork
        /// </summary>
        /// <param name="unitOfWork">Đối tượng UnitOfWork</param>
        protected BaseService(IUnitOfWork unitOfWork)
        {
            UnitOfWork = unitOfWork;
        }
    }
}
