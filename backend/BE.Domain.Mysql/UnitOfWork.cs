using BE.Domain.Repos;
using MySqlConnector;
using System;
using System.Data;
using System.Threading.Tasks;

namespace BE.Domain.Mysql
{
    /// <summary>
    /// Implementation của IUnitOfWork cho MySQL
    /// </summary>
    public class UnitOfWork : IUnitOfWork
    {
        private readonly string _connectionString;
        private IDbConnection _connection;
        private IDbTransaction _transaction;

        /// <summary>
        /// Khởi tạo UnitOfWork với chuỗi kết nối
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối MySQL</param>
        public UnitOfWork(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Bắt đầu một giao dịch mới
        /// </summary>
        public async Task BeginTransactionAsync()
        {
            if (_connection == null)
            {
                _connection = new MySqlConnection(_connectionString);
            }

            if (_connection.State != ConnectionState.Open)
            {
                await ((MySqlConnection)_connection).OpenAsync();
            }

            _transaction = _connection.BeginTransaction();
        }

        /// <summary>
        /// Cam kết giao dịch hiện tại
        /// </summary>
        public async Task CommitTransactionAsync()
        {
            if (_transaction != null)
            {
                await Task.Run(() => _transaction.Commit());
                _transaction.Dispose();
                _transaction = null;
            }
        }

        /// <summary>
        /// Rollback giao dịch hiện tại
        /// </summary>
        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
            {
                await Task.Run(() => _transaction.Rollback());
                _transaction.Dispose();
                _transaction = null;
            }
        }

        /// <summary>
        /// Lưu tất cả thay đổi vào cơ sở dữ liệu
        /// </summary>
        /// <returns>Số bản ghi bị ảnh hưởng</returns>
        public Task<int> SaveChangesAsync()
        {
            // Với Dapper, các thay đổi đã được execute trực tiếp
            // Phương thức này trả về 0 vì không cóchange tracking
            return Task.FromResult(0);
        }

        /// <summary>
        /// Lấy kết nối hiện tại
        /// </summary>
        public IDbConnection GetConnection()
        {
            return _connection ?? new MySqlConnection(_connectionString);
        }

        /// <summary>
        /// Lấy giao dịch hiện tại
        /// </summary>
        public IDbTransaction GetTransaction()
        {
            return _transaction;
        }
    }
}
