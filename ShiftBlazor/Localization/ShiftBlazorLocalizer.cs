using ShiftSoftware.ShiftEntity.Core.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShiftSoftware.ShiftBlazor.Localization;

public class ShiftBlazorLocalizer : ShiftLocalizer
{
    public ShiftBlazorLocalizer(IServiceProvider services, Type resourceType) : base(services, resourceType)
    {
    }
}