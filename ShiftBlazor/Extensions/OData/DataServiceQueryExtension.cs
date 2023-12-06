using Microsoft.OData.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.OData.Client
{
    public static class DataServiceQueryExtension
    {
        public static DataServiceQuery<T> WhereQuery<T>(this DataServiceQuery<T> query, Expression<Func<T, bool>> predicate)
        {
            return (DataServiceQuery<T>)query.Where(predicate);
        }

        public static DataServiceQuery<T> WhereQuery<T>(this DataServiceQuery<T> query, Expression<Func<T, int, bool>> predicate)
        {
            return (DataServiceQuery<T>)query.Where(predicate);
        }

        public static DataServiceQuery<T> WhereIf<T>(this DataServiceQuery<T> query, Expression<Func<T, bool>> predicate, bool condition)
        {
            return condition
                ? query.WhereQuery(predicate)
                : query;
        }

        public static DataServiceQuery<T> AddQueryOptionIf<T>(this DataServiceQuery<T> query, string name, object value, bool condition)
        {
            Console.WriteLine(condition);
            return condition
                ? query.AddQueryOption(name, value)
                : query;
            
        }
    }
}
