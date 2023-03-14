using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShiftSoftware.ShiftBlazor.Extensions
{
    public static class StringExtension
    {
        public static string AddUrlPath(this string text, params string?[] paths)
        {
            var _paths = paths.Where(x => !string.IsNullOrWhiteSpace(x));
            if (_paths.Count() == 0)
            {
                return text;
            }

            var baseAddress = text.TrimEnd('/');

            _paths = _paths.Select(x => x!.TrimStart('/').TrimEnd('/'));

            return baseAddress + "/" + string.Join("/", _paths);
        }
    }
}
