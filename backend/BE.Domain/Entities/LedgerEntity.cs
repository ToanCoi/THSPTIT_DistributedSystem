using System;

namespace BE.Domain.Entities
{
    /// <summary>
    /// Entity sổ cái tồn kho - bảng chính ghi nhận các nghiệp vụ nhập/xuất
    /// </summary>
    public class LedgerEntity
    {
        /// <summary>
        /// ID bản ghi
        /// </summary>
        public Guid ledger_id { get; set; }

        /// <summary>
        /// ID sản phẩm
        /// </summary>
        public Guid product_id { get; set; }

        /// <summary>
        /// ID kho
        /// </summary>
        public Guid stock_id { get; set; }

        /// <summary>
        /// Số lượng nhập
        /// </summary>
        public decimal inward_quantity { get; set; }

        /// <summary>
        /// Số lượng xuất
        /// </summary>
        public decimal outward_quantity { get; set; }

        /// <summary>
        /// ID tham chiếu (order_id, inward_id, etc)
        /// </summary>
        public Guid reference_id { get; set; }

        /// <summary>
        /// Loại tham chiếu (ORDER, INWARD, OUTWARD)
        /// </summary>
        public string reference_type { get; set; }

        /// <summary>
        /// Ngày ghi sổ
        /// </summary>
        public DateTime ledger_date { get; set; }

        /// <summary>
        /// Ngày tạo
        /// </summary>
        public DateTime created_date { get; set; }

        /// <summary>
        /// Người tạo
        /// </summary>
        public string created_by { get; set; }
    }
}