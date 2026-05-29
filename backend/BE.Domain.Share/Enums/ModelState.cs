using System;
using System.Collections.Generic;
using System.Text;

namespace BE.Domain.Shared.Enums
{
    public enum ModelState
    {
        /// <summary>
        /// Không thao tác
        /// </summary>
        None = 0,
        /// <summary>
        /// Thêm mới
        /// </summary>
        Insert = 1,
        /// <summary>
        /// Cập nhật
        /// </summary>
        Update = 2,
        /// <summary>
        /// Xóa
        /// </summary>
        Delete = 3
    }
}
