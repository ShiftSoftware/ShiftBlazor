using System.Collections;

namespace ShiftSoftware.ShiftBlazor.Extensions;

public static class TypeExtension
{
    public static bool IsEnumerable(this Type type)
    {
        return type.Name != nameof(String) && type.GetInterface(nameof(IEnumerable)) != null;
    }
}
