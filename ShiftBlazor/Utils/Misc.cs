using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace ShiftSoftware.ShiftBlazor.Utils
{
    public static class Misc
    {
        internal static object? GetValueFromPropertyPath(object item, string propertyPath)
        {
            var currentValue = item;
            var currentType = item.GetType();
            var propertyNames = propertyPath.Split('.');

            foreach (var propertyName in propertyNames)
            {
                var currentProperty = currentType.GetProperty(propertyName);
                if (currentProperty == null)
                {
                    throw new ArgumentException($"Property '{propertyName}' does not exist in {propertyPath}.");
                }
                currentValue = currentProperty.GetValue(currentValue);
                currentType = currentProperty.PropertyType;
            }
            return currentValue;
        }

        internal static string? GetFieldFromPropertyPath(string propertyPath)
        {
            return propertyPath.Split(".").ElementAt(0);
        }

        public static Expression<Func<T, object>> CreateExpression<T>(string propertyName)
        {
            // Fetch the property from the type
            var propertyInfo = typeof(T).GetProperty(propertyName);
            if (propertyInfo == null)
            {
                throw new ArgumentException($"No property '{propertyName}' on type '{typeof(T).FullName}'");
            }

            // Construct the expression: x => x.Property
            var parameter = Expression.Parameter(typeof(T), "x");
            var propertyAccess = Expression.MakeMemberAccess(parameter, propertyInfo);

            // Handle value type properties by converting to object
            var convertExpression = Expression.Convert(propertyAccess, typeof(object));

            return Expression.Lambda<Func<T, object>>(convertExpression, parameter);
        }
    }
}
