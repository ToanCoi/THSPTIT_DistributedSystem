using System;
using System.Collections.Generic;
using System.Text;

namespace BE.Domain.Shared.Entities
{
    /// <summary>
    /// Interface cho khóa chính của bản ghi
    /// </summary>
    /// <typeparam name="TKey">Kiểu dữ liệu của khóa chính</typeparam>
    public interface IRecordKey<TKey>
    {
        /// <summary>
        /// Khóa chính của bản ghi
        /// </summary>
        TKey id { get; set; }
    }
}
