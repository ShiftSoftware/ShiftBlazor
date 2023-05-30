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
        public static event EventHandler<UriEventArgs> OnRequestFailed;
        public static event EventHandler<UriEventArgs> OnRequestStarted;

        public static void TriggerRequestFailed(Uri uri)
        {
            OnRequestFailed?.Invoke(null, new UriEventArgs(uri));
        }

        public static void TriggerRequestStarted(Uri uri)
        {
            OnRequestStarted?.Invoke(null, new UriEventArgs(uri));
        }
    }
}
