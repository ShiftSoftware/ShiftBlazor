using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShiftSoftware.ShiftBlazor.Extensions
{
    public static class UriExtension
    {
        public static string AbsoluteUriWithoutQuery(this Uri uri)
        {
            if (uri == null)
                throw new ArgumentNullException(nameof(uri));

            return uri.GetLeftPart(UriPartial.Path);
        }
    }
}
