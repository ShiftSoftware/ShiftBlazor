using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using ShiftSoftware.ShiftIdentity.Blazor;
using ShiftSoftware.ShiftIdentity.Core;
using ShiftSoftware.ShiftIdentity.Core.DTOs;

namespace ShiftSoftware.ShiftBlazor.Components;

public partial class UserAvatar
{
    [Inject] IDialogService Dialog { get; set; } = default!;
    [Inject] NavigationManager NavigationManager { get; set; } = default!;
    [Inject] IServiceProvider ServiceProvider { get; set; } = default!;

    [Parameter]
    public MouseEvent ActivationEvent { get; set; } = MouseEvent.LeftClick;

    [Parameter]
    public RenderFragment? MenuItemsTemplate { get; set; }

    [Parameter]
    public bool ShowClosedOpenIcon { get; set; }

    [Parameter]
    public string? IconClosed { get; set; } = Icons.Material.Filled.KeyboardArrowDown;

    [Parameter]
    public string? IconOpen { get; set; } = Icons.Material.Filled.KeyboardArrowUp;

    public bool IsOpen = false;
    
    TokenUserDataDTO? userdata;
    private ShiftIdentityBlazorOptions? IdentityOptions;
    private IIdentityStore? tokenStore;

    protected override async Task OnInitializedAsync()
    {
        IdentityOptions = ServiceProvider.GetService<ShiftIdentityBlazorOptions>();
        tokenStore = ServiceProvider.GetService<IIdentityStore>();

    }

    internal async Task Logout()
    {
        if (tokenStore == null)
            return;
        await tokenStore.RemoveTokenAsync();
        NavigationManager.NavigateTo("/", true);
    }

    internal async Task OpenSettings() => await Dialog.ShowAsync<Settings>();

    internal void IsOpenHandler(bool open) => IsOpen = open;

    internal void GoToIdentity()
    {
        if (IdentityOptions == null)
            return;
        NavigationManager.NavigateTo($"{IdentityOptions.FrontEndBaseUrl}/{Constants.IdentityRoutePreifix}/UserDataForm");
    }
}
