using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShiftSoftware.ShiftBlazor.Extensions;

public static class TypeExtension
{
    public static bool IsEnumerable(this Type type)
    {
        return type.Name != nameof(String) && type.GetInterface(nameof(IEnumerable)) != null;
    }
}
