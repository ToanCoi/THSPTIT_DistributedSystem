using System;
using System.Collections.Generic;
using System.Text;

namespace BE.Domain.Shared.Cruds
{
    public class PagingSummaryResult
    {
        /// <summary>
        /// Tổng số bản ghi trả về
        /// </summary>
        public long Total { get; set; }

        /// <summary>
        /// Dữ liệu summary
        /// </summary>
        public object Data { get; set; }
    }
}
