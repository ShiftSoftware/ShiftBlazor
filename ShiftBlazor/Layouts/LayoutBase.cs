using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;
using ShiftSoftware.ShiftBlazor.Components;
using ShiftSoftware.ShiftBlazor.Enums;
using ShiftSoftware.ShiftBlazor.Interfaces;
using ShiftSoftware.ShiftBlazor.Services;

namespace ShiftSoftware.ShiftBlazor.Layouts;

public class LayoutBase : LayoutComponentBase
{
    [Inject] private ShiftModal ShiftModal { get; set; } = default!;
    [Inject] private NavigationManager NavManager { get; set; } = default!;

    [CascadingParameter]
    public AppLayoutContext? App { get; set; }

    protected override void OnInitialized()
    {
        base.OnInitialized();

        NavManager.LocationChanged += HandleLocationChanged;
    }

    protected override Task OnInitializedAsync() => OpenModals();

    [JSInvokable]
    public static void SendKeys(IEnumerable<string> keys)
    {
        _ = IShortcutComponent.SendKeys(keys.Select(Enum.Parse<KeyboardKeys>));
    }

    private void HandleLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        _ = OpenModals();
    }

    private async Task OpenModals()
    {
        await ShiftModal.UpdateModals();
    }

}
