using Dapper;
using MySqlConnector;
using BE.Domain.Querys;
using BE.Domain.Repos;
using BE.Domain.Shared.Cruds;
using BE.Domain.Shared.Entities;
using BE.Domain.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace BE.Domain.Mysql
{
    /// <summary>
    /// Base repository implementation cho MySQL sử dụng Dapper
    /// </summary>
    public abstract class BaseRepo : IBaseRepo
    {
        protected readonly string _connectionString;

        static BaseRepo()
        {
            DapperSetup.RegisterGuidTypeHandler();
        }

        /// <summary>
        /// Khởi tạo BaseRepo với chuỗi kết nối
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối MySQL</param>
        protected BaseRepo(string connectionString)
        {
            _connectionString = connectionString;
        }

        #region Connection Management - Quản lý kết nối

        /// <summary>
        /// Lấy kết nối đến cơ sở dữ liệu MySQL
        /// </summary>
        /// <returns>Kết nối database</returns>
        public IDbConnection GetConnection()
        {
            return new MySqlConnection(_connectionString);
        }

        /// <summary>
        /// Lấy kết nối đến cơ sở dữ liệu MySQL một cách không đồng bộ
        /// </summary>
        /// <returns>Task chứa kết nối database</returns>
        public Task<IDbConnection> GetConnectionAsync()
        {
            return Task.FromResult<IDbConnection>(new MySqlConnection(_connectionString));
        }

        /// <summary>
        /// Đóng kết nối đến cơ sở dữ liệu
        /// </summary>
        /// <param name="connection">Kết nối cần đóng</param>
        public async Task CloseConnection(IDbConnection connection)
        {
            if (connection != null && connection.State != ConnectionState.Closed)
            {
                await Task.Run(() => connection.Close());
            }
        }

        #endregion

        #region GetById - Lấy theo ID

        /// <summary>
        /// Lấy một đối tượng theo ID từ cơ sở dữ liệu
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu của đối tượng</typeparam>
        /// <param name="id">ID của đối tượng</param>
        /// <returns>Đối tượng được tìm thấy hoặc null</returns>
        public async Task<T> GetByIdAsync<T>(object id)
        {
            using var connection = new MySqlConnection(_connectionString);
            return await GetByIdAsync<T>(connection, id);
        }

        /// <summary>
        /// Lấy một đối tượng theo ID với kết nối đã cung cấp
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu của đối tượng</typeparam>
        /// <param name="connection">Kết nối database</param>
        /// <param name="id">ID của đối tượng</param>
        /// <returns>Đối tượng được tìm thấy hoặc null</returns>
        public async Task<T> GetByIdAsync<T>(IDbConnection connection, object id)
        {
            var type = typeof(T);
            var tableName = GetTableName(type);
            var primaryKey = GetPrimaryKey(type);

            var sql = $"SELECT * FROM {tableName} WHERE {primaryKey} = @Id";
            var result = await connection.QueryFirstOrDefaultAsync<T>(sql, new { Id = id });
            return result;
        }

        /// <summary>
        /// Lấy một đối tượng theo ID với giao dịch đã cung cấp
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu của đối tượng</typeparam>
        /// <param name="transaction">Giao dịch database</param>
        /// <param name="id">ID của đối tượng</param>
        /// <returns>Đối tượng được tìm thấy hoặc null</returns>
        public async Task<T> GetByIdAsync<T>(IDbTransaction transaction, object id)
        {
            using var connection = new MySqlConnection(_connectionString);
            connection.Open();
            using var trans = connection.BeginTransaction();

            var type = typeof(T);
            var tableName = GetTableName(type);
            var primaryKey = GetPrimaryKey(type);

            var sql = $"SELECT * FROM {tableName} WHERE {primaryKey} = @Id";
            var result = await connection.QueryFirstOrDefaultAsync<T>(sql, new { Id = id }, transaction);
            return result;
        }

        #endregion

        #region GetAsync - Lấy theo điều kiện

        /// <summary>
        /// Lấy một đối tượng theo các điều kiện đã cung cấp
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu của đối tượng</typeparam>
        /// <param name="columns">Các cột cần lấy</param>
        /// <param name="field">Tên trường để so sánh</param>
        /// <param name="value">Giá trị để so sánh</param>
        /// <param name="op">Toán tử so sánh</param>
        /// <returns>Đối tượng được tìm thấy hoặc null</returns>
        public async Task<T> GetAsync<T>(string columns, string field, object value, int op = (int)EnumFilterOperator.Equal)
        {
            using var connection = new MySqlConnection(_connectionString);
            return await GetAsync<T>(connection, columns, field, value, op);
        }

        /// <summary>
        /// Lấy một đối tượng theo các điều kiện đã cung cấp với kết nối đã cung cấp
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu của đối tượng</typeparam>
        /// <param name="connection">Kết nối database</param>
        /// <param name="columns">Các cột cần lấy</param>
        /// <param name="field">Tên trường để so sánh</param>
        /// <param name="value">Giá trị để so sánh</param>
        /// <param name="op">Toán tử so sánh</param>
        /// <returns>Đối tượng được tìm thấy hoặc null</returns>
        public async Task<T> GetAsync<T>(IDbConnection connection, string columns, string field, object value, int op = (int)EnumFilterOperator.Equal)
        {
            var type = typeof(T);
            var tableName = GetTableName(type);

            var sql = $"SELECT {columns} FROM {tableName} WHERE {field} {GetOperator(op)} @Value";
            var result = await connection.QueryFirstOrDefaultAsync<T>(sql, new { Value = value });
            return result;
        }

        #endregion

        #region Insert - Chèn mới

        /// <summary>
        /// Chèn một đối tượng vào cơ sở dữ liệu và trả về ID
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu của đối tượng</typeparam>
        /// <param name="entity">Đối tượng cần chèn</param>
        /// <returns>ID của đối tượng vừa chèn</returns>
        public async Task<object> InsertAsync<T>(object entity)
        {
            using var connection = new MySqlConnection(_connectionString);
            return await InsertAsync<T>(connection, entity);
        }

        /// <summary>
        /// Chèn một đối tượng vào cơ sở dữ liệu với kết nối đã cung cấp và trả về ID
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu của đối tượng</typeparam>
        /// <param name="connection">Kết nối database</param>
        /// <param name="entity">Đối tượng cần chèn</param>
        /// <returns>ID của đối tượng vừa chèn</returns>
        public async Task<object> InsertAsync<T>(IDbConnection connection, object entity)
        {
            var type = typeof(T);
            var tableName = GetTableName(type);
            var properties = GetInsertProperties(type, entity);
            var columns = string.Join(", ", properties.Select(p => p.column));
            var parameters = string.Join(", ", properties.Select(p => $"@{p.param}"));

            var sql = $"INSERT INTO {tableName} ({columns}) VALUES ({parameters}); SELECT LAST_INSERT_ID();";

            var paramDict = properties.ToDictionary(p => p.param, p => p.value);

            // Lấy ID sau khi chèn
            var id = await connection.ExecuteScalarAsync(sql, paramDict);
            return id;
        }

        /// <summary>
        /// Chèn một đối tượng vào cơ sở dữ liệu với giao dịch đã cung cấp và trả về ID
        /// </summary>
        /// <param name="transaction">Giao dịch database</param>
        /// <param name="entity">Đối tượng cần chèn</param>
        /// <returns>ID của đối tượng vừa chèn</returns>
        public async Task<object> InsertAsync(IDbTransaction transaction, object entity)
        {
            // Với giao dịch, cần xác định kiểu từ entity
            var type = entity.GetType();
            var tableName = GetTableName(type);
            var properties = GetInsertProperties(type, entity);
            var columns = string.Join(", ", properties.Select(p => p.column));
            var parameters = string.Join(", ", properties.Select(p => $"@{p.param}"));

            var sql = $"INSERT INTO {tableName} ({columns}) VALUES ({parameters}); SELECT LAST_INSERT_ID();";

            var paramDict = properties.ToDictionary(p => p.param, p => p.value);

            var connection = transaction.Connection;
            var id = await connection.ExecuteScalarAsync(sql, paramDict, transaction);
            return id;
        }

        /// <summary>
        /// Chèn một đối tượng vào cơ sở dữ liệu với giao dịch đã cung cấp và trả về ID
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu</typeparam>
        /// <param name="transaction">Giao dịch database</param>
        /// <param name="type">Kiểu dữ liệu chứa tên bảng</param>
        /// <param name="entity">Đối tượng cần chèn</param>
        /// <returns>ID của đối tượng vừa chèn</returns>
        public async Task<object> InsertAsync<T>(IDbTransaction transaction, Type type, object entity)
        {
            var tableName = GetTableName(type);
            var properties = GetInsertProperties(type, entity);
            var columns = string.Join(", ", properties.Select(p => p.column));
            var parameters = string.Join(", ", properties.Select(p => $"@{p.param}"));

            var sql = $"INSERT INTO {tableName} ({columns}) VALUES ({parameters}); SELECT LAST_INSERT_ID();";

            var paramDict = properties.ToDictionary(p => p.param, p => p.value);

            var connection = transaction.Connection;
            var id = await connection.ExecuteScalarAsync(sql, paramDict, transaction);
            return id;
        }

        #endregion

        #region Update - Cập nhật

        /// <summary>
        /// Cập nhật một đối tượng trong cơ sở dữ liệu
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu của đối tượng</typeparam>
        /// <param name="entity">Đối tượng cần cập nhật</param>
        /// <param name="fields">Danh sách các trường cần cập nhật (null = tất cả)</param>
        /// <returns>true nếu cập nhật thành công</returns>
        public async Task<bool> UpdateAsync<T>(object entity, string fields = null)
        {
            using var connection = new MySqlConnection(_connectionString);
            return await UpdateAsync<T>(connection, entity, fields);
        }

        /// <summary>
        /// Cập nhật một đối tượng trong cơ sở dữ liệu với kết nối đã cung cấp
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu của đối tượng</typeparam>
        /// <param name="connection">Kết nối database</param>
        /// <param name="entity">Đối tượng cần cập nhật</param>
        /// <param name="fields">Danh sách các trường cần cập nhật (null = tất cả)</param>
        /// <returns>true nếu cập nhật thành công</returns>
        public async Task<bool> UpdateAsync<T>(IDbConnection connection, object entity, string fields = null)
        {
            var type = typeof(T);
            var tableName = GetTableName(type);
            var primaryKey = GetPrimaryKey(type);
            var id = GetPrimaryKeyValue(type, entity);

            var properties = GetUpdateProperties(type, entity, fields);
            if (!properties.Any())
            {
                return false;
            }

            var setClause = string.Join(", ", properties.Select(p => $"{p.column} = @{p.param}"));
            var sql = $"UPDATE {tableName} SET {setClause} WHERE {primaryKey} = @Id";

            var paramDict = properties.ToDictionary(p => p.param, p => p.value);
            paramDict["Id"] = id;

            var rows = await connection.ExecuteAsync(sql, paramDict);
            return rows > 0;
        }

        /// <summary>
        /// Cập nhật một đối tượng trong cơ sở dữ liệu với giao dịch đã cung cấp
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu của đối tượng</typeparam>
        /// <param name="transaction">Giao dịch database</param>
        /// <param name="entity">Đối tượng cần cập nhật</param>
        /// <param name="fields">Danh sách các trường cần cập nhật (null = tất cả)</param>
        /// <returns>true nếu cập nhật thành công</returns>
        public async Task<bool> UpdateAsync<T>(IDbTransaction transaction, object entity, string fields = null)
        {
            var type = typeof(T);
            var tableName = GetTableName(type);
            var primaryKey = GetPrimaryKey(type);
            var id = GetPrimaryKeyValue(type, entity);

            var properties = GetUpdateProperties(type, entity, fields);
            if (!properties.Any())
            {
                return false;
            }

            var setClause = string.Join(", ", properties.Select(p => $"{p.column} = @{p.param}"));
            var sql = $"UPDATE {tableName} SET {setClause} WHERE {primaryKey} = @Id";

            var paramDict = properties.ToDictionary(p => p.param, p => p.value);
            paramDict["Id"] = id;

            var connection = transaction.Connection;
            var rows = await connection.ExecuteAsync(sql, paramDict, transaction);
            return rows > 0;
        }

        /// <summary>
        /// Cập nhật một đối tượng trong cơ sở dữ liệu với giao dịch đã cung cấp
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu</typeparam>
        /// <param name="transaction">Giao dịch database</param>
        /// <param name="type">Kiểu dữ liệu chứa tên bảng</param>
        /// <param name="entity">Đối tượng cần cập nhật</param>
        /// <param name="fields">Danh sách các trường cần cập nhật (null = tất cả)</param>
        /// <returns>true nếu cập nhật thành công</returns>
        public async Task<bool> UpdateAsync<T>(IDbTransaction transaction, Type type, object entity, string fields = null)
        {
            var tableName = GetTableName(type);
            var primaryKey = GetPrimaryKey(type);
            var id = GetPrimaryKeyValue(type, entity);

            var properties = GetUpdateProperties(type, entity, fields);
            if (!properties.Any())
            {
                return false;
            }

            var setClause = string.Join(", ", properties.Select(p => $"{p.column} = @{p.param}"));
            var sql = $"UPDATE {tableName} SET {setClause} WHERE {primaryKey} = @Id";

            var paramDict = properties.ToDictionary(p => p.param, p => p.value);
            paramDict["Id"] = id;

            var connection = transaction.Connection;
            var rows = await connection.ExecuteAsync(sql, paramDict, transaction);
            return rows > 0;
        }

        #endregion

        #region Delete - Xóa

        /// <summary>
        /// Xóa một đối tượng khỏi cơ sở dữ liệu
        /// </summary>
        /// <param name="entity">Đối tượng cần xóa</param>
        /// <returns>true nếu xóa thành công</returns>
        public async Task<bool> DeleteAsync(object entity)
        {
            using var connection = new MySqlConnection(_connectionString);
            return await DeleteAsync(connection, entity);
        }

        /// <summary>
        /// Xóa một đối tượng khỏi cơ sở dữ liệu với kết nối đã cung cấp
        /// </summary>
        /// <param name="connection">Kết nối database</param>
        /// <param name="entity">Đối tượng cần xóa</param>
        /// <returns>true nếu xóa thành công</returns>
        public async Task<bool> DeleteAsync(IDbConnection connection, object entity)
        {
            var type = entity.GetType();
            var tableName = GetTableName(type);
            var primaryKey = GetPrimaryKey(type);
            var id = GetPrimaryKeyValue(type, entity);

            var sql = $"DELETE FROM {tableName} WHERE {primaryKey} = @Id";
            var rows = await connection.ExecuteAsync(sql, new { Id = id });
            return rows > 0;
        }

        /// <summary>
        /// Xóa một đối tượng khỏi cơ sở dữ liệu với giao dịch đã cung cấp
        /// </summary>
        /// <param name="transaction">Giao dịch database</param>
        /// <param name="entity">Đối tượng cần xóa</param>
        /// <returns>true nếu xóa thành công</returns>
        public async Task<bool> DeleteAsync(IDbTransaction transaction, object entity)
        {
            var type = entity.GetType();
            var tableName = GetTableName(type);
            var primaryKey = GetPrimaryKey(type);
            var id = GetPrimaryKeyValue(type, entity);

            var sql = $"DELETE FROM {tableName} WHERE {primaryKey} = @Id";
            var connection = transaction.Connection;
            var rows = await connection.ExecuteAsync(sql, new { Id = id }, transaction);
            return rows > 0;
        }

        /// <summary>
        /// Xóa một đối tượng khỏi cơ sở dữ liệu với giao dịch đã cung cấp
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu</typeparam>
        /// <param name="transaction">Giao dịch database</param>
        /// <param name="type">Kiểu dữ liệu chứa tên bảng</param>
        /// <param name="entity">Đối tượng cần xóa</param>
        /// <returns>true nếu xóa thành công</returns>
        public async Task<bool> DeleteAsync<T>(IDbTransaction transaction, Type type, object entity)
        {
            var tableName = GetTableName(type);
            var primaryKey = GetPrimaryKey(type);
            var id = GetPrimaryKeyValue(type, entity);

            var sql = $"DELETE FROM {tableName} WHERE {primaryKey} = @Id";
            var connection = transaction.Connection;
            var rows = await connection.ExecuteAsync(sql, new { Id = id }, transaction);
            return rows > 0;
        }

        #endregion

        #region Submit - Thao tác hàng loạt

        /// <summary>
        /// Thực hiện danh sách các thao tác chèn, cập nhật hoặc xóa trong một giao dịch
        /// </summary>
        /// <param name="submitModels">Danh sách các mô hình thao tác</param>
        public async Task SubmitAsync(List<SubmitModel> submitModels)
        {
            using var connection = new MySqlConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                foreach (var model in submitModels)
                {
                    await ProcessSubmitModel(connection, transaction, model);
                }
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        /// <summary>
        /// Thực hiện danh sách các thao tác chèn, cập nhật hoặc xóa với kết nối đã cung cấp
        /// </summary>
        /// <param name="connection">Kết nối database</param>
        /// <param name="submitModels">Danh sách các mô hình thao tác</param>
        public async Task SubmitAsync(IDbConnection connection, List<SubmitModel> submitModels)
        {
            using var transaction = connection.BeginTransaction();
            try
            {
                foreach (var model in submitModels)
                {
                    await ProcessSubmitModel(connection, transaction, model);
                }
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        /// <summary>
        /// Thực hiện danh sách các thao tác chèn, cập nhật hoặc xóa với giao dịch đã cung cấp
        /// </summary>
        /// <param name="transaction">Giao dịch database</param>
        /// <param name="submitModels">Danh sách các mô hình thao tác</param>
        public async Task SubmitAsync(IDbTransaction transaction, List<SubmitModel> submitModels)
        {
            try
            {
                foreach (var model in submitModels)
                {
                    await ProcessSubmitModel(transaction.Connection, transaction, model);
                }
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Xử lý một mô hình thao tác (SubmitModel)
        /// </summary>
        /// <param name="connection">Kết nối database</param>
        /// <param name="transaction">Giao dịch database</param>
        /// <param name="model">Mô hình thao tác</param>
        private async Task ProcessSubmitModel(IDbConnection connection, IDbTransaction transaction, SubmitModel model)
        {
            if (model.Datas == null || !model.Datas.Any())
                return;

            switch (model.State)
            {
                case ModelState.Insert:
                    foreach (var data in model.Datas)
                    {
                        await InsertData(connection, transaction, model.TableName, data);
                    }
                    break;

                case ModelState.Update:
                    foreach (var data in model.Datas)
                    {
                        await UpdateData(connection, transaction, model.TableName, data, model.KeyFields);
                    }
                    break;

                case ModelState.Delete:
                    foreach (var data in model.Datas)
                    {
                        await DeleteData(connection, transaction, model.TableName, data, model.KeyFields);
                    }
                    break;

                case ModelState.None:
                    break;
            }
        }

        /// <summary>
        /// Chèn dữ liệu vào bảng
        /// </summary>
        /// <param name="connection">Kết nối database</param>
        /// <param name="transaction">Giao dịch database</param>
        /// <param name="tableName">Tên bảng</param>
        /// <param name="data">Dữ liệu cần chèn</param>
        private async Task InsertData(IDbConnection connection, IDbTransaction transaction, string tableName, Dictionary<string, object> data)
        {
            var columns = string.Join(", ", data.Keys);
            var parameters = string.Join(", ", data.Keys.Select(k => $"@{k}"));
            var sql = $"INSERT INTO {tableName} ({columns}) VALUES ({parameters})";
            await connection.ExecuteAsync(sql, data, transaction);
        }

        /// <summary>
        /// Cập nhật dữ liệu trong bảng
        /// </summary>
        /// <param name="connection">Kết nối database</param>
        /// <param name="transaction">Giao dịch database</param>
        /// <param name="tableName">Tên bảng</param>
        /// <param name="data">Dữ liệu cần cập nhật</param>
        /// <param name="keyFields">Danh sách trường khóa chính</param>
        private async Task UpdateData(IDbConnection connection, IDbTransaction transaction, string tableName, Dictionary<string, object> data, List<string> keyFields)
        {
            if (keyFields == null || !keyFields.Any())
                return;

            var updateFields = data.Keys.Where(k => !keyFields.Contains(k)).ToList();
            var setClause = string.Join(", ", updateFields.Select(f => $"{f} = @{f}"));
            var whereClause = string.Join(" AND ", keyFields.Select(f => $"{f} = @{f}"));

            var sql = $"UPDATE {tableName} SET {setClause} WHERE {whereClause}";
            await connection.ExecuteAsync(sql, data, transaction);
        }

        /// <summary>
        /// Xóa dữ liệu trong bảng
        /// </summary>
        /// <param name="connection">Kết nối database</param>
        /// <param name="transaction">Giao dịch database</param>
        /// <param name="tableName">Tên bảng</param>
        /// <param name="data">Dữ liệu để xác định bản ghi cần xóa</param>
        /// <param name="keyFields">Danh sách trường khóa chính</param>
        private async Task DeleteData(IDbConnection connection, IDbTransaction transaction, string tableName, Dictionary<string, object> data, List<string> keyFields)
        {
            if (keyFields == null || !keyFields.Any())
                return;

            var whereClause = string.Join(" AND ", keyFields.Select(f => $"{f} = @{f}"));
            var sql = $"DELETE FROM {tableName} WHERE {whereClause}";
            await connection.ExecuteAsync(sql, data, transaction);
        }

        #endregion

        #region Paging - Phân trang

        /// <summary>
        /// Lấy dữ liệu phân trang từ cơ sở dữ liệu
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu của đối tượng</typeparam>
        /// <param name="type">Kiểu dữ liệu chứa tên bảng</param>
        /// <param name="columns">Các cột cần lấy</param>
        /// <param name="skip">Số bản ghi bỏ qua</param>
        /// <param name="take">Số bản ghi cần lấy</param>
        /// <param name="sort">Câu lệnh sắp xếp</param>
        /// <param name="filters">Câu lệnh lọc</param>
        /// <returns>Kết quả phân trang</returns>
        public async Task<PagingResult> GetPaging<T>(Type type, string columns, int skip, int take, string sort, string filters = null)
        {
            using var connection = new MySqlConnection(_connectionString);
            var tableName = GetTableName(type);

            var whereClause = string.IsNullOrEmpty(filters) ? "" : $"WHERE {filters}";
            var orderClause = string.IsNullOrEmpty(sort) ? "ORDER BY created_date DESC" : $"ORDER BY {sort}";

            var sql = $"SELECT {columns} FROM {tableName} {whereClause} {orderClause} LIMIT @Take OFFSET @Skip";
            var countSql = $"SELECT COUNT(*) FROM {tableName} {whereClause}";

            var parameters = new { Skip = skip, Take = take };
            var whereParameters = ParseFilterParameters(filters);

            var data = await connection.QueryAsync<T>(sql, whereParameters);
            var total = await connection.ExecuteScalarAsync<long>(countSql, whereParameters);

            return new PagingResult
            {
                Data = data.ToList(),
                Empty = !data.Any()
            };
        }

        /// <summary>
        /// Lấy tổng số bản ghi từ cơ sở dữ liệu với bộ lọc
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu của đối tượng</typeparam>
        /// <param name="type">Kiểu dữ liệu chứa tên bảng</param>
        /// <param name="columns">Các cột cần lấy</param>
        /// <param name="filters">Câu lệnh lọc</param>
        /// <returns>Kết quả tổng hợp phân trang</returns>
        public async Task<PagingSummaryResult> GetPagingSummary<T>(Type type, string columns, string filters = null)
        {
            using var connection = new MySqlConnection(_connectionString);
            var tableName = GetTableName(type);

            var whereClause = string.IsNullOrEmpty(filters) ? "" : $"WHERE {filters}";
            var countSql = $"SELECT COUNT(*) FROM {tableName} {whereClause}";

            var whereParameters = ParseFilterParameters(filters);
            var total = await connection.ExecuteScalarAsync<long>(countSql, whereParameters);

            return new PagingSummaryResult
            {
                Total = total,
                Data = null
            };
        }

        #endregion

        #region Helper Methods - Các phương thức hỗ trợ

        /// <summary>
        /// Lấy tên bảng từ kiểu dữ liệu (giả định tên bảng là dạng snake_case số nhiều)
        /// Ví dụ: Employee -> employee, EmployeeAddress -> employee_address
        /// </summary>
        /// <param name="type">Kiểu dữ liệu</param>
        /// <returns>Tên bảng</returns>
        protected virtual string GetTableName(Type type)
        {
            var name = type.Name;
            // Chuyển PascalCase sang snake_case
            var snakeCase = string.Concat(name.Select((c, i) =>
                i > 0 && char.IsUpper(c) ? "_" + char.ToLower(c) : char.ToLower(c).ToString()));
            return snakeCase;
        }

        /// <summary>
        /// Lấy tên cột khóa chính (giả định có hậu tố _id)
        /// Ví dụ: Employee -> employee_id
        /// </summary>
        /// <param name="type">Kiểu dữ liệu</param>
        /// <returns>Tên cột khóa chính</returns>
        protected virtual string GetPrimaryKey(Type type)
        {
            var tableName = GetTableName(type);
            return tableName + "_id";
        }

        /// <summary>
        /// Lấy giá trị khóa chính từ đối tượng
        /// </summary>
        /// <param name="type">Kiểu dữ liệu</param>
        /// <param name="entity">Đối tượng</param>
        /// <returns>Giá trị khóa chính</returns>
        protected virtual object GetPrimaryKeyValue(Type type, object entity)
        {
            var idProperty = type.GetProperty("id") ?? type.GetProperty(type.Name.ToLower() + "_id");
            if (idProperty == null)
            {
                // Thử tìm property có hậu tố _id
                var prop = type.GetProperties().FirstOrDefault(p => p.Name.EndsWith("_id", StringComparison.OrdinalIgnoreCase));
                if (prop != null)
                {
                    return prop.GetValue(entity);
                }
                throw new Exception($"Không tìm thấy property ID trên kiểu {type.Name}");
            }
            return idProperty.GetValue(entity);
        }

        /// <summary>
        /// Lấy toán tử SQL từ EnumFilterOperator
        /// </summary>
        /// <param name="op">Mã toán tử</param>
        /// <returns>Toán tử SQL</returns>
        protected virtual string GetOperator(int op)
        {
            return op switch
            {
                (int)EnumFilterOperator.Equal => "=",
                (int)EnumFilterOperator.NotEqual => "<>",
                (int)EnumFilterOperator.GreaterThan => ">",
                (int)EnumFilterOperator.GreaterThanOrEqual => ">=",
                (int)EnumFilterOperator.LessThan => "<",
                (int)EnumFilterOperator.LessThanOrEqual => "<=",
                (int)EnumFilterOperator.Contains => "LIKE",
                (int)EnumFilterOperator.StartsWith => "LIKE",
                (int)EnumFilterOperator.EndsWith => "LIKE",
                (int)EnumFilterOperator.IsNull => "IS NULL",
                (int)EnumFilterOperator.IsNotNull => "IS NOT NULL",
                _ => "="
            };
        }

        /// <summary>
        /// Lấy danh sách các thuộc tính cho câu lệnh INSERT
        /// </summary>
        /// <param name="type">Kiểu dữ liệu</param>
        /// <param name="entity">Đối tượng</param>
        /// <returns>Danh sách các thuộc tính (tên cột, tên param, giá trị)</returns>
        protected virtual List<(string column, string param, object value)> GetInsertProperties(Type type, object entity)
        {
            var properties = new List<(string column, string param, object value)>();

            foreach (var prop in type.GetProperties())
            {
                // Bỏ qua các thuộc tính được xử lý riêng (như record history)
                var propName = prop.Name;
                if (propName == "id" || propName == "created_date" || propName == "created_by" ||
                    propName == "modified_date" || propName == "modified_by")
                {
                    if (propName == "id")
                    {
                        // Bao gồm id cho insert
                        var idValue = prop.GetValue(entity);
                        if (idValue != null && (Guid)idValue != Guid.Empty)
                        {
                            properties.Add((GetColumnName(propName), propName, idValue));
                        }
                    }
                    continue;
                }

                var propValue = prop.GetValue(entity);
                if (propValue != null)
                {
                    properties.Add((GetColumnName(propName), propName, propValue));
                }
            }

            return properties;
        }

        /// <summary>
        /// Lấy danh sách các thuộc tính cho câu lệnh UPDATE
        /// </summary>
        /// <param name="type">Kiểu dữ liệu</param>
        /// <param name="entity">Đối tượng</param>
        /// <param name="fields">Danh sách các trường cần cập nhật (null = tất cả)</param>
        /// <returns>Danh sách các thuộc tính (tên cột, tên param, giá trị)</returns>
        protected virtual List<(string column, string param, object value)> GetUpdateProperties(Type type, object entity, string fields)
        {
            var properties = new List<(string column, string param, object value)>();

            if (string.IsNullOrEmpty(fields))
            {
                // Cập nhật tất cả các thuộc tính có thể thay đổi
                foreach (var prop in type.GetProperties())
                {
                    var updName = prop.Name;
                    if (updName == "id" || updName == "created_date" || updName == "created_by")
                        continue;

                    var updValue = prop.GetValue(entity);
                    if (updValue != null)
                    {
                        properties.Add((GetColumnName(updName), updName, updValue));
                    }
                }
            }
            else
            {
                // Chỉ cập nhật các trường được chỉ định
                var fieldList = fields.Split(',').Select(f => f.Trim()).ToList();
                foreach (var field in fieldList)
                {
                    var propName = ToPascalCase(field);
                    var prop = type.GetProperty(propName);
                    if (prop != null && prop.Name != "id")
                    {
                        var value = prop.GetValue(entity);
                        properties.Add((field, field, value));
                    }
                }
            }

            return properties;
        }

        /// <summary>
        /// Chuyển PascalCase sang snake_case
        /// </summary>
        /// <param name="propertyName">Tên thuộc tính</param>
        /// <returns>Tên cột snake_case</returns>
        protected virtual string GetColumnName(string propertyName)
        {
            return string.Concat(propertyName.Select((c, i) =>
                i > 0 && char.IsUpper(c) ? "_" + char.ToLower(c) : char.ToLower(c).ToString()));
        }

        /// <summary>
        /// Chuyển snake_case sang PascalCase
        /// </summary>
        /// <param name="snakeCase">Tên snake_case</param>
        /// <returns>Tên PascalCase</returns>
        protected virtual string ToPascalCase(string snakeCase)
        {
            return string.Join("", snakeCase.Split('_').Select(s =>
                string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s.Substring(1).ToLower()));
        }

        /// <summary>
        /// Parse các tham số lọc cho Dapper
        /// </summary>
        /// <param name="filters">Câu lệnh lọc</param>
        /// <returns>DynamicParameters</returns>
        protected virtual DynamicParameters ParseFilterParameters(string filters)
        {
            var parameters = new DynamicParameters();
            // Parse đơn giản - trong production nên dùng parser mạnh hơn
            return parameters;
        }

        #endregion
    }
}
