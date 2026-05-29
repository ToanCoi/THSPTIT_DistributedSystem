using Dapper;
using System;
using System.Data;

namespace BE.Domain.Mysql
{
    /// <summary>
    /// TypeHandler for Dapper to handle Guid/String conversion for MySQL
    /// </summary>
    public class GuidTypeHandler : SqlMapper.TypeHandler<Guid>
    {
        public override Guid Parse(object value)
        {
            if (value is Guid guid)
                return guid;
            if (value is string str && Guid.TryParse(str, out var result))
                return result;
            return Guid.Empty;
        }

        public override void SetValue(IDbDataParameter parameter, Guid value)
        {
            parameter.Value = value.ToString();
        }
    }

    /// <summary>
    /// Static class to register Dapper type handlers
    /// </summary>
    public static class DapperSetup
    {
        private static bool _registered = false;
        private static readonly object _lock = new object();

        public static void RegisterGuidTypeHandler()
        {
            if (_registered) return;
            lock (_lock)
            {
                if (_registered) return;
                SqlMapper.AddTypeHandler(new GuidTypeHandler());
                _registered = true;
            }
        }
    }
}