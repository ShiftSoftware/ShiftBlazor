using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components;
using ShiftSoftware.ShiftBlazor.Services;
using Microsoft.JSInterop;
using ShiftSoftware.ShiftBlazor.Interfaces;
using ShiftSoftware.ShiftBlazor.Enums;

namespace ShiftSoftware.ShiftBlazor.Utils
{
    public class ShiftMainLayout : LayoutComponentBase
    {
        [Inject] private ShiftModal ShiftModal { get; set; } = default!;
        [Inject] private NavigationManager NavManager { get; set; } = default!;

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
    }
}
