using System;
using System.Collections.Generic;
using System.Text;

namespace BE.Domain.Shared.Enums
{
    /// <summary>
    /// Enum các toán tử logic kết hợp filter
    /// </summary>
    public enum EnumFilterAddition : int
    {
        /// <summary>
        /// Và (AND)
        /// </summary>
        And = 1,

        /// <summary>
        /// Hoặc (OR)
        /// </summary>
        Or = 2
    }
}
