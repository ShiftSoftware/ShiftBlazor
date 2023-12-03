using System.Linq.Expressions;
using System.Reflection.Emit;
using System.Reflection;

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
                        CustomAttributeBuilder attributeBuilder = new(attributeCtor, constructorArgs);
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
    }
}
