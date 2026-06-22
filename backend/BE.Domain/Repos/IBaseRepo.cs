using BE.Domain.Querys;
using BE.Domain.Shared.Cruds;
using BE.Domain.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;

namespace BE.Domain.Repos
{
    public interface IBaseRepo
    {
        /// <summary>
        /// Lấy kết nối đến cơ sở dữ liệu.
        /// </summary>
        /// <returns></returns>
        IDbConnection GetConnection();
        /// <summary>
        /// Lấy kết nối đến cơ sở dữ liệu không đồng bộ.
        /// </summary>
        /// <returns></returns>
        Task<IDbConnection> GetConnectionAsync();

        /// <summary>
        /// Đóng kết nối đến cơ sở dữ liệu.
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        Task CloseConnection(IDbConnection connection);

        /// <summary>
        /// Lấy một đối tượng theo ID từ cơ sở dữ liệu.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<T> GetByIdAsync<T>(object id);

        /// <summary>
        /// Lấy một đối tượng theo ID từ cơ sở dữ liệu với kết nối đã cung cấp.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="connection"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<T> GetByIdAsync<T>(IDbConnection connection, object id);

        /// <summary>
        /// Lấy một đối tượng theo ID từ cơ sở dữ liệu với kết nối và giao dịch đã cung cấp.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="transaction"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<T> GetByIdAsync<T>(IDbTransaction transaction, object id);

        /// <summary>
        /// Lấy một đối tượng từ cơ sở dữ liệu theo các điều kiện đã cung cấp.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="columns"></param>
        /// <param name="field"></param>
        /// <param name="value"></param>
        /// <param name="op"></param>
        /// <returns></returns>
        Task<T> GetAsync<T>(string columns, string field, object value, int op = (int)EnumFilterOperator.Equal);

        /// <summary>
        /// Lấy một đối tượng từ cơ sở dữ liệu theo các điều kiện đã cung cấp với kết nối đã cung cấp.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="connection"></param>
        /// <param name="columns"></param>
        /// <param name="field"></param>
        /// <param name="value"></param>
        /// <param name="op"></param>
        /// <returns></returns>
        Task<T> GetAsync<T>(IDbConnection connection, string columns, string field, object value, int op = (int)EnumFilterOperator.Equal);

        /// <summary>
        /// Chèn một đối tượng vào cơ sở dữ liệu và trả về đối tượng đã chèn với ID.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        /// <returns></returns>
        Task<object> InsertAsync<T>(object entity);

        /// <summary>
        /// Chèn một đối tượng vào cơ sở dữ liệu với kết nối đã cung cấp và trả về đối tượng đã chèn với ID.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="connection"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        Task<object> InsertAsync<T>(IDbConnection connection, object entity);

        /// <summary>
        /// Chèn một đối tượng vào cơ sở dữ liệu với kết nối đã cung cấp và trả về đối tượng đã chèn với ID.
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        Task<object> InsertAsync(IDbTransaction transaction, object entity);

        /// <summary>
        /// Chèn một đối tượng vào cơ sở dữ liệu với giao dịch đã cung cấp và trả về đối tượng đã chèn với ID.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="transaction"></param>
        /// <param name="type">Kiểu dữ liệu chứa tên bảng</param>
        /// <param name="entity"></param>
        /// <returns></returns>
        Task<object> InsertAsync<T>(IDbTransaction transaction, Type type, object entity);

        /// <summary>
        /// Cập nhật một đối tượng trong cơ sở dữ liệu và trả về true nếu thành công.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        /// <param name="fields"></param>
        /// <returns></returns>
        Task<bool> UpdateAsync<T>(object entity, string fields = null);

        /// <summary>
        /// Cập nhật một đối tượng trong cơ sở dữ liệu với kết nối đã cung cấp và trả về true nếu thành công.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="connection"></param>
        /// <param name="entity"></param>
        /// <param name="fields"></param>
        /// <returns></returns>
        Task<bool> UpdateAsync<T>(IDbConnection connection, object entity, string fields = null);

        /// <summary>
        /// Cập nhật một đối tượng trong cơ sở dữ liệu với giao dịch đã cung cấp và trả về true nếu thành công.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="transaction"></param>
        /// <param name="entity"></param>
        /// <param name="fields"></param>
        /// <returns></returns>
        Task<bool> UpdateAsync<T>(IDbTransaction transaction, object entity, string fields = null);

        /// <summary>
        /// Cập nhật một đối tượng trong cơ sở dữ liệu với giao dịch đã cung cấp và trả về true nếu thành công.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="transaction"></param>
        /// <param name="type">Kiểu dữ liệu chứ tên bảng</param>
        /// <param name="entity"></param>
        /// <param name="fields"></param>
        /// <returns></returns>
        Task<bool> UpdateAsync<T>(IDbTransaction transaction, Type type, object entity, string fields = null);

        /// <summary>
        /// Xóa một đối tượng khỏi cơ sở dữ liệu và trả về true nếu thành công.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        Task<bool> DeleteAsync(object entity);

        /// <summary>
        /// Xóa một đối tượng khỏi cơ sở dữ liệu với kết nối đã cung cấp và trả về true nếu thành công.
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        Task<bool> DeleteAsync(IDbConnection connection, object entity);

        /// <summary>
        /// Xóa một đối tượng khỏi cơ sở dữ liệu với giao dịch đã cung cấp và trả về true nếu thành công.
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        Task<bool> DeleteAsync(IDbTransaction transaction, object entity);

        /// <summary>
        /// Xóa một đối tượng khỏi cơ sở dữ liệu với giao dịch đã cung cấp và trả về true nếu thành công.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="transaction"></param>
        /// <param name="type">Kiểu dữ liệu chứa tên bảng</param>
        /// <param name="entity"></param>
        /// <returns></returns>
        Task<bool> DeleteAsync<T>(IDbTransaction transaction, Type type, object entity);

        /// <summary>
        /// Gửi một danh sách các mô hình để thực hiện các thao tác chèn, cập nhật hoặc xóa trong cơ sở dữ liệu.
        /// </summary>
        /// <param name="submitModels"></param>
        /// <returns></returns>
        Task SubmitAsync(List<SubmitModel> submitModels);

        /// <summary>
        /// Gửi một danh sách các mô hình để thực hiện các thao tác chèn, cập nhật hoặc xóa trong cơ sở dữ liệu với kết nối đã cung cấp.
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="submitModels"></param>
        /// <returns></returns>
        Task SubmitAsync(IDbConnection connection, List<SubmitModel> submitModels);

        /// <summary>
        /// Gửi một danh sách các mô hình để thực hiện các thao tác chèn, cập nhật hoặc xóa trong cơ sở dữ liệu với kết nối đã cung cấp.
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="submitModels"></param>
        /// <returns></returns>
        Task SubmitAsync(IDbTransaction transaction, List<SubmitModel> submitModels);

        /// <summary>
        /// Lấy dữ liệu phân trang từ cơ sở dữ liệu.
        /// </summary>
        /// <typeparam name="T">Kiểu entity</typeparam>
        /// <param name="columns">Các cột cần lấy</param>
        /// <param name="skip">Số bản ghi bỏ qua</param>
        /// <param name="take">Số bản ghi cần lấy</param>
        /// <param name="sort">Câu lệnh sắp xếp</param>
        /// <param name="filters">Câu lệnh lọc</param>
        /// <returns>Kết quả phân trang</returns>
        Task<PagingResult> GetPaging<T>(string columns, int skip, int take, string sort, string filters = null);

        /// <summary>
        /// Lấy dữ liệu phân trang từ cơ sở dữ liệu và trả về tổng số bản ghi.
        /// </summary>
        /// <typeparam name="T">Kiểu entity</typeparam>
        /// <param name="columns">Các cột cần lấy</param>
        /// <param name="filters">Câu lệnh lọc</param>
        /// <returns>Kết quả tổng hợp phân trang</returns>
        Task<PagingSummaryResult> GetPagingSummary<T>(string columns, string filters = null);
    }
}
