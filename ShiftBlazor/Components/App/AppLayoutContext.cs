using Microsoft.AspNetCore.Components;
using ShiftSoftware.ShiftBlazor.Services;

namespace ShiftSoftware.ShiftBlazor.Components;

public class AppContext
{
    private SettingManager SettingManager { get; set; }
    public string? Title { get; set; }
    public bool EnableAuthorization { get; set; }
    public bool IsDrawerOpen { get; set; }

    public AppContext(SettingManager settingManager)
    {
        SettingManager = settingManager;
        IsDrawerOpen = settingManager.GetDrawerState();
    }
    public void ToggleDrawer() => IsDrawerOpen = SettingManager.SetDrawerState(!IsDrawerOpen);
}

public class AppLayoutContext : AppContext
{
    public AppLayoutContext(SettingManager settingManager) : base(settingManager)
    {}

    public RenderFragment<AppContext>? NavMenuTemplate { get; set; }
    public RenderFragment<AppContext>? HeaderTemplate { get; set; }
    public RenderFragment<AppContext>? FooterTemplate { get; set; }
    public Type? MainLayout { get; set; }
}
