using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace EmailDeliveryService.Extensions
{
    public static class DataReaderExtensions
    {
        public static T GetValueOrDefault<T>(this IDataReader reader, string columnName)
        {
            object columnValue = reader[columnName];
            T returnValue = default;
            if (!(columnValue is DBNull))
            {
                returnValue = (T)Convert.ChangeType(columnValue, typeof(T));
            }
            return returnValue;
        }
    }
}
