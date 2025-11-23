using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace ShiftSoftware.ShiftBlazor.Utils;

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
        return propertyPath?.Split(".").ElementAt(0);
    }

    internal static string GetPropertyPath(LambdaExpression expression, string delimiter = ".")
    {
        var paths = new List<string>();
        Expression? current = expression.Body;

        if (current is MethodCallExpression call)
        {
            if (call.Object != null)
            {
                current = call.Object;
            }
            else if (call.Arguments.Count > 0)
            {
                current = call.Arguments[0];
            }
        }

        while (current is UnaryExpression u &&
          (u.NodeType == ExpressionType.Convert ||
           u.NodeType == ExpressionType.ConvertChecked ||
           u.NodeType == ExpressionType.TypeAs))
        {
            current = u.Operand;
        }

        while (current is MemberExpression member)
        {
            paths.Insert(0, member.Member.Name);
            current = member.Expression;
        }

        return string.Join(delimiter, paths);
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

    public static TAttribute? GetAttribute<TElement,TAttribute>()
        where TAttribute : Attribute
    {
        return GetAttribute<TAttribute>(typeof(TElement));
    }

    public static TAttribute? GetAttribute<TAttribute>(Type? ElementType)
        where TAttribute : Attribute
    {
        if (ElementType == null) return null;
        return (TAttribute?)Attribute.GetCustomAttribute(ElementType, typeof(TAttribute));
    }

    internal static object? CreateClassObject(string className, Dictionary<string, Type> Properties, IEnumerable<CustomAttributeData>? attributeDatas = null)
    {
        AssemblyName assemblyName = new AssemblyName("DynamicClassAssembly");
        AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");

        TypeBuilder typeBuilder = moduleBuilder.DefineType(className,
            TypeAttributes.Public |
            TypeAttributes.Class |
            TypeAttributes.AutoClass |
            TypeAttributes.AnsiClass |
            TypeAttributes.BeforeFieldInit |
            TypeAttributes.AutoLayout);

        foreach (var property in Properties)
        {
            var propName = property.Key;
            var type = property.Value;

            FieldBuilder fieldBuilder = typeBuilder.DefineField("_" + propName.ToLower(), typeof(string), FieldAttributes.Private);
            PropertyBuilder propertyBuilder = typeBuilder.DefineProperty(propName, PropertyAttributes.HasDefault, type, null);

            if (attributeDatas != null)
            {
                foreach (var attrData in attributeDatas)
                {
                    ConstructorInfo attributeCtor = attrData.Constructor;
                    object?[] constructorArgs = attrData.ConstructorArguments.Select(arg => arg.Value).ToArray();

                    var namedProperties = new List<PropertyInfo>();
                    var propertyValues = new List<object?>();
                    var namedFields = new List<FieldInfo>();
                    var fieldValues = new List<object?>();

                    foreach (var namedArg in attrData.NamedArguments)
                    {
                        if (namedArg.MemberInfo is PropertyInfo propertyInfo)
                        {
                            namedProperties.Add(propertyInfo);
                            propertyValues.Add(namedArg.TypedValue.Value);
                        }
                        else if (namedArg.MemberInfo is FieldInfo fieldInfo)
                        {
                            namedFields.Add(fieldInfo);
                            fieldValues.Add(namedArg.TypedValue.Value);
                        }
                    }

                    CustomAttributeBuilder attributeBuilder = new(
                        attributeCtor,
                        constructorArgs,
                        namedProperties.ToArray(),
                        propertyValues.ToArray(),
                        namedFields.ToArray(),
                        fieldValues.ToArray()
                    );

                    propertyBuilder.SetCustomAttribute(attributeBuilder);
                }
            }

            MethodBuilder getPropMthdBldr = typeBuilder.DefineMethod("get_" + propName, MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, type, Type.EmptyTypes);
            ILGenerator getIl = getPropMthdBldr.GetILGenerator();

            getIl.Emit(OpCodes.Ldarg_0);
            getIl.Emit(OpCodes.Ldfld, fieldBuilder);
            getIl.Emit(OpCodes.Ret);

            MethodBuilder setPropMthdBldr = typeBuilder.DefineMethod("set_" + propName, MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, null, new Type[] { type });
            ILGenerator setIl = setPropMthdBldr.GetILGenerator();

            setIl.Emit(OpCodes.Ldarg_0);
            setIl.Emit(OpCodes.Ldarg_1);
            setIl.Emit(OpCodes.Stfld, fieldBuilder);
            setIl.Emit(OpCodes.Ret);

            propertyBuilder.SetGetMethod(getPropMthdBldr);
            propertyBuilder.SetSetMethod(setPropMthdBldr);
        }

        Type classType = typeBuilder.CreateType();
        return Activator.CreateInstance(classType);
    }

    public static string GetExpressionPath<T, P>(Expression<Func<T, P>> expr)
    {
        MemberExpression? memberExpression;

        switch (expr.Body.NodeType)
        {
            case ExpressionType.Convert:
            case ExpressionType.ConvertChecked:
                var ue = expr.Body as UnaryExpression;
                memberExpression = ((ue != null) ? ue.Operand : null) as MemberExpression;
                break;
            default:
                memberExpression = expr.Body as MemberExpression;
                break;
        }

        var path = new List<string>();

        while (memberExpression != null)
        {
            path.Add(memberExpression.Member.Name);
            memberExpression = memberExpression.Expression as MemberExpression;
        }

        path.Reverse();
        return string.Join(".", path);
    }


    // Mud's FieldType.Identify doesn't work with DateTimeOffset
    public static bool IsDateTime(Type? type)
    {
        if (type is null)
        {
            return false;
        }

        if (type == typeof(DateTime) || type == typeof(DateTimeOffset))
        {
            return true;
        }

        var underlyingType = Nullable.GetUnderlyingType(type);

        return underlyingType is not null && (underlyingType == typeof(DateTime) || underlyingType == typeof(DateTimeOffset));
    }

    // https://stackoverflow.com/a/11124118
    // https://www.somacon.com/p576.php
    public static string BytesToString(long value)
    {
        string suffix;
        double readable;

        switch (Math.Abs(value))
        {
            case >= 0x1000000000000000:
                suffix = "EiB";
                readable = value >> 50;
                break;
            case >= 0x4000000000000:
                suffix = "PiB";
                readable = value >> 40;
                break;
            case >= 0x10000000000:
                suffix = "TiB";
                readable = value >> 30;
                break;
            case >= 0x40000000:
                suffix = "GiB";
                readable = value >> 20;
                break;
            case >= 0x100000:
                suffix = "MiB";
                readable = value >> 10;
                break;
            case >= 0x400:
                suffix = "KiB";
                readable = value;
                break;
            default:
                return value.ToString("0 B");
        }

        return (readable / 1024).ToString("0.## ", CultureInfo.InvariantCulture) + suffix;
    }
}
