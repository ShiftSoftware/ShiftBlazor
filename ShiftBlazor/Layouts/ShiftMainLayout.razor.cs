using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components;
using ShiftSoftware.ShiftBlazor.Services;
using Microsoft.JSInterop;
using ShiftSoftware.ShiftBlazor.Interfaces;
using ShiftSoftware.ShiftBlazor.Enums;
using ShiftSoftware.ShiftBlazor.Components;

namespace ShiftSoftware.ShiftBlazor.Layouts;

public partial class ShiftMainLayout : LayoutComponentBase
{
    [Inject] private ShiftModal ShiftModal { get; set; } = default!;
    [Inject] private NavigationManager NavManager { get; set; } = default!;
    [Inject] private  SettingManager SettingManager { get; set; } = default!;

    [CascadingParameter]
    public AppLayoutContext? App { get; set; }

    bool _drawerOpen = false;

    protected override void OnInitialized()
    {
        base.OnInitialized();

        NavManager.LocationChanged += HandleLocationChanged;
        OpenModals();
    }

    [JSInvokable]
    public static void SendKeys(IEnumerable<string> keys)
    {
        _ = IShortcutComponent.SendKeys(keys.Select(Enum.Parse<KeyboardKeys>));
    }

    private void HandleLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        OpenModals();
    }

    private void OpenModals()
    {
        ShiftModal.UpdateModals();
    }

    private void DrawerToggle()
    {
        _drawerOpen = !_drawerOpen;
    }
}
