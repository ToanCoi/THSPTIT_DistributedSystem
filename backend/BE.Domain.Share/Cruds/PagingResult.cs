using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace BE.Domain.Shared.Cruds
{
    public class PagingResult
    {
        /// <summary>
        /// Dữ liệu trả về
        /// </summary>
        public IList Data { get; set; }

        /// <summary>
        /// Bảng không có dữ liệu
        /// </summary>
        public bool Empty { get; set; }
    }
}
