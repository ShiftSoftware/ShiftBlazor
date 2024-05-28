using ShiftSoftware.ShiftBlazor.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShiftSoftware.ShiftBlazor.Interfaces;
public interface IFilterableComponent
{
    public void AddFilter(Guid id, string field, ODataOperator op = ODataOperator.Equal, object? value = null);

}
