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

    public static class CollectionExtensions
    {
        /// <summary>
        /// Splits or partition the given collection to a desired size
        /// </summary>
        /// <param name="size">size to split</param>
        /// <example>
        ///	List<string> items = new List<string>();
        ///	for(var i=1;i<=253;i++)
        ///	{
        ///		items.Add(string.Format("String {0}" , i));
        ///	}	
        ///	
        ///	items.Partition<string>(100).Dump();
        /// </example>
        public static IEnumerable<IEnumerable<T>> Partition<T>(this IEnumerable<T> source, int size)
        {
            int i = 0;
            List<T> list = new List<T>(size);
            foreach (T item in source)
            {
                list.Add(item);
                if (++i == size)
                {
                    yield return list;
                    list = new List<T>(size);
                    i = 0;
                }
            }
            if (list.Count > 0)
                yield return list;
        }
    }
}
