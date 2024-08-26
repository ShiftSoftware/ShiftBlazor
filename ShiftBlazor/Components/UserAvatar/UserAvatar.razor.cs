using ShiftSoftware.ShiftIdentity.Core.DTOs;
using ShiftSoftware.ShiftIdentity.Core;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using ShiftSoftware.ShiftIdentity.Blazor;

namespace ShiftSoftware.ShiftBlazor.Components;

public partial class UserAvatar
{
    [Inject] IDialogService Dialog { get; set; } = default!;
    [Inject] IIdentityStore tokenStore { get; set; } = default!;
    [Inject] NavigationManager NavigationManager { get; set; } = default!;

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

    protected override async Task OnInitializedAsync()
    {
        userdata = (await tokenStore.GetTokenAsync())?.UserData;
    }

    internal async Task Logout()
    {
        await tokenStore.RemoveTokenAsync();
        NavigationManager.NavigateTo("/", true);
    }

    internal void OpenSettings() => Dialog.Show<Settings>();

    internal void IsOpenHandler(bool open) => IsOpen = open;

    internal void GoToIdentity()
    {
        NavigationManager.NavigateTo($"{IdentityOptions.FrontEndBaseUrl}/{Constants.IdentityRoutePreifix}/UserDataForm");
    }
}
