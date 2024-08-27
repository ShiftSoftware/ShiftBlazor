using Microsoft.AspNetCore.Components;

namespace ShiftSoftware.ShiftBlazor.Components;

public class AppLayoutContext
{
    public string? Title { get; set; }
    public RenderFragment<NavMenuContext>? NavMenuTemplate { get; set; }
    public RenderFragment? HeaderTemplate { get; set; }
    public RenderFragment? FooterTemplate { get; set; }
    public bool EnableAutherization { get; set; }
}
