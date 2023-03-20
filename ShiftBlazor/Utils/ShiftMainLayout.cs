using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components;
using ShiftSoftware.ShiftBlazor.Services;

namespace ShiftSoftware.ShiftBlazor.Utils
{
    public class ShiftMainLayout : LayoutComponentBase
    {
        [Inject] private ShiftModalService ShiftModal { get; set; } = default!;
        [Inject] private NavigationManager NavManager { get; set; } = default!;

        protected override void OnInitialized()
        {
            base.OnInitialized();

            NavManager.LocationChanged += HandleLocationChanged;
            OpenModals();
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
