using BE.Domain.Entities;
using BE.Domain.Shared.Entities;
using BE.Domain.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace BE.Domain.DI.Employee.DTO
{
    public class EmpoyeeEntityDtoEdit : EmployeeEntity, IRecordState
    {
        /// <summary>
        /// Trạng thái bản ghi để theo dõi (không lưu vào DB)
        /// </summary>
        public ModelState State { get; set; }
    }
}
