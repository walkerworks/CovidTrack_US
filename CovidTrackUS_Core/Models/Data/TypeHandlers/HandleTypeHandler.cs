using CovidTrackUS_Core.Enums;
using Dapper;
using System;
using System.Data;

namespace CovidTrackUS_Core.Models.Data.TypeHandlers
{
    /// <summary>
    /// Handler for Dapper to properly map the <see cref="HandleType"/> object type
    /// </summary>
    public class HandleTypeHandler : SqlMapper.ITypeHandler
    {
        public object Parse(Type destinationType, object value)
        {
            if (destinationType == typeof(HandleType))
                return (HandleType)((string)value);
            else return null;
        }

        public void SetValue(IDbDataParameter parameter, object value)
        {
            parameter.DbType = DbType.String;
            parameter.Value = (string)((dynamic)value);
        }
    }
}
