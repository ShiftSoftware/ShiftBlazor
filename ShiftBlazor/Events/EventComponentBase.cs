using Microsoft.AspNetCore.Components;
using ShiftSoftware.ShiftBlazor.Events.CustomEventArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShiftSoftware.ShiftBlazor.Events
{
    public class EventComponentBase : ComponentBase
    {
        public static event EventHandler<KeyValuePair<string, List<Guid>>> OnGridSort;
        public static event EventHandler<Guid>? OnGridStateChanged;

        public static void TriggerGridSort(KeyValuePair<string, List<Guid>> order)
        {
            OnGridSort?.Invoke(null, order);
        }

        public static void TriggerGridStateChanged(Guid guid)
        {
            OnGridStateChanged?.Invoke(null, guid);
        }
    }
}
