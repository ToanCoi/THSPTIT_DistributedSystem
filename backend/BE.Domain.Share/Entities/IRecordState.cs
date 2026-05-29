using BE.Domain.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace BE.Domain.Shared.Entities
{
    public interface IRecordState
    {
        /// <summary>
        /// Trạng thái của dữ liệu, đây là trường đặc biệt k lưu vào DB, xác định với các DTO thôi
        /// </summary>
        ModelState State { get; set; }
    }
}
