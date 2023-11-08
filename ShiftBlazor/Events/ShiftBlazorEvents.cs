using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShiftSoftware.ShiftBlazor.Events
{
    public static class ShiftBlazorEvents
    {
        public static event EventHandler<KeyValuePair<Guid, List<object>>> OnBeforeGridDataBound;
        public static void TriggerOnBeforeGridDataBound(KeyValuePair<Guid, List<object>> data)
        {
            OnBeforeGridDataBound?.Invoke(null, data);
        }
    }
}
