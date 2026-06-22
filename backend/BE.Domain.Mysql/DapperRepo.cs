using System;
using System.Collections.Generic;
using System.Linq;

namespace BE.Domain.Mysql
{
    /// <summary>
    /// Dapper repository implementation cho các operation cơ bản
    /// </summary>
    public class DapperRepo : BaseRepo
    {
        private static readonly Dictionary<Type, string> _tableNameMap = new Dictionary<Type, string>
        {
            { typeof(BE.Domain.Entities.StockEntity), "stocks" },
            { typeof(BE.Domain.Entities.ProductEntity), "products" },
            { typeof(BE.Domain.Entities.CustomerEntity), "customers" },
            { typeof(BE.Domain.Entities.InwardEntity), "inwards" },
            { typeof(BE.Domain.Entities.OutwardEntity), "outwards" },
            { typeof(BE.Domain.Entities.OrderEntity), "orders" },
            { typeof(BE.Domain.Entities.OrderItemEntity), "order_items" },
            { typeof(BE.Domain.Entities.LedgerEntity), "led_inventory_item_ledger" },
            { typeof(BE.Domain.Entities.EmployeeEntity), "employees" },
            { typeof(BE.Domain.Entities.UserEntity), "users" },
        };

        public DapperRepo(string connectionString) : base(connectionString)
        {
        }

        protected override string GetTableName(Type type)
        {
            if (_tableNameMap.TryGetValue(type, out var tableName))
            {
                return tableName;
            }
            return base.GetTableName(type);
        }
    }
}
