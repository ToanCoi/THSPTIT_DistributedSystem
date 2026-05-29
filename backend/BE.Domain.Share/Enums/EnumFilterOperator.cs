using System;
using System.Collections.Generic;
using System.Text;

namespace BE.Domain.Shared.Enums
{
    /// <summary>
    /// Enum các toán tử so sánh cho filter
    /// </summary>
    public enum EnumFilterOperator : int
    {
        /// <summary>
        /// Bằng
        /// </summary>
        Equal = 1,

        /// <summary>
        /// Không bằng
        /// </summary>
        NotEqual = 2,

        /// <summary>
        /// Lớn hơn
        /// </summary>
        GreaterThan = 3,

        /// <summary>
        /// Lớn hơn hoặc bằng
        /// </summary>
        GreaterThanOrEqual = 4,

        /// <summary>
        /// Nhỏ hơn
        /// </summary>
        LessThan = 5,

        /// <summary>
        /// Nhỏ hơn hoặc bằng
        /// </summary>
        LessThanOrEqual = 6,

        /// <summary>
        /// Chứa
        /// </summary>
        Contains = 7,

        /// <summary>
        /// Bắt đầu với
        /// </summary>
        StartsWith = 8,

        /// <summary>
        /// Kết thúc với
        /// </summary>
        EndsWith = 9,

        /// <summary>
        /// Là null
        /// </summary>
        IsNull = 10,

        /// <summary>
        /// Không là null
        /// </summary>
        IsNotNull = 11
    }
}
