using Microsoft.AspNetCore.Components;

namespace ShiftSoftware.ShiftBlazor.Filters.Models;

public class FilterUIOptions
{
    public int xxl { get; set; }
    public int xl { get; set; }
    public int lg { get; set; }
    public int md { get; set; }
    public int sm { get; set; }
    public int xs { get; set; }
    public int Order { get; set; }

    public RenderFragment<FilterModelBase>? Template { get; set; }
}
