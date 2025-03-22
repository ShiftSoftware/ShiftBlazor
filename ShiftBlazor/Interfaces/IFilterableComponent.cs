using ShiftSoftware.ShiftBlazor.Enums;
using ShiftSoftware.ShiftBlazor.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShiftSoftware.ShiftBlazor.Interfaces;
public interface IFilterableComponent
{
    public bool FilterImmediate { get; set; }
    public ODataFilterGenerator Filters { get; }

    public RenderFragment? FilterTemplate { get; set; }
    public void AddFilter(Guid id, string field, ODataOperator op = ODataOperator.Equal, object? value = null);

}
