using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShiftSoftware.ShiftBlazor.Events.CustomEventArgs;

public class UriEventArgs
{
    public Uri? Uri { get; set; }

    public UriEventArgs(Uri uri)
    {
        Uri = uri;
    }
}
