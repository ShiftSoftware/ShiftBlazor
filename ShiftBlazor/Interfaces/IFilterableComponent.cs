using Microsoft.AspNetCore.Components;
using ShiftSoftware.ShiftBlazor.Filters.Models;
using ShiftSoftware.ShiftBlazor.Enums;
using ShiftSoftware.ShiftBlazor.Utils;

namespace ShiftSoftware.ShiftBlazor.Interfaces;
public interface IFilterableComponent
{
    public bool FilterImmediate { get; set; }
    public ODataFilterGenerator ODataFilters { get; }

    public RenderFragment? FilterTemplate { get; set; }

    public Dictionary<Guid, FilterModelBase> Filters { get; set; }

    public void AddFilter(Guid id, string field, ODataOperator op = ODataOperator.Equal, object? value = null);
}
