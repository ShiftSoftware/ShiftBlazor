using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShiftSoftware.ShiftBlazor.Tests
{
    public class ODataResult<T>
    {
        public List<T> value { get; set; } = new();
    }
}
