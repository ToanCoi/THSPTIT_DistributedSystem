using System.Threading.Tasks;

namespace BE.Domain.Repos
{
    /// <summary>
    /// Interface đơn vị công việc - quản lý transaction và lưu thay đổi
    /// </summary>
    public interface IUnitOfWork
    {
        /// <summary>
        /// Bắt đầu một giao dịch mới
        /// </summary>
        Task BeginTransactionAsync();

        /// <summary>
        /// Cam kết giao dịch hiện tại
        /// </summary>
        Task CommitTransactionAsync();

        /// <summary>
        /// Rollback giao dịch hiện tại
        /// </summary>
        Task RollbackTransactionAsync();

        /// <summary>
        /// Lưu tất cả thay đổi vào cơ sở dữ liệu
        /// </summary>
        /// <returns>Số bản ghi bị ảnh hưởng</returns>
        Task<int> SaveChangesAsync();
    }
}
