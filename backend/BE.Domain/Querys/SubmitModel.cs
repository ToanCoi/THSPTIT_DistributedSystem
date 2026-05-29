using BE.Domain.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace BE.Domain.Querys
{
    public class SubmitModel
    {
        /// <summary>
        /// Tên của bảng
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// Trạng thái của bản ghi
        /// </summary>
        public ModelState State { get; set; }

        /// <summary>
        /// Dữ liệu
        /// </summary>
        public List<Dictionary<string, object>> Datas { get; set; }

        /// <summary>
        /// Danh sách các trường khóa chính của bảng
        /// </summary>
        public List<string> KeyFields { get; set; }
    }
}
