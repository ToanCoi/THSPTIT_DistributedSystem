using System;
using System.Collections.Generic;
using System.Text;

namespace BE.Domain.Shared.Entities
{
    public interface IRecordVersion
    {
        /// <summary>
        /// Phiên bản của dữ liệu
        /// Sẽ được build ra từ modified_date
        /// </summary>
        long EditVersion { get; set; }
    }
}
