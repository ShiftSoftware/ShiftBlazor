using Microsoft.AspNetCore.Components;

namespace ShiftSoftware.ShiftBlazor.Components;

public class AppContext
{
    public string? Title { get; set; }
    public bool EnableAutherization { get; set; }
    public bool IsDrawerOpen = false;
    public void ToggleDrawer() => IsDrawerOpen = !IsDrawerOpen;
}

public class AppLayoutContext : AppContext
{
    public RenderFragment<AppContext>? NavMenuTemplate { get; set; }
    public RenderFragment<AppContext>? HeaderTemplate { get; set; }
    public RenderFragment<AppContext>? FooterTemplate { get; set; }
}
