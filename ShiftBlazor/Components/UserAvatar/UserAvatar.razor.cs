using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using ShiftSoftware.ShiftIdentity.Blazor;
using ShiftSoftware.ShiftIdentity.Core;
using System.Security.Claims;

namespace ShiftSoftware.ShiftBlazor.Components;

public partial class UserAvatar
{
    [Inject] IDialogService Dialog { get; set; } = default!;
    [Inject] NavigationManager NavigationManager { get; set; } = default!;
    [Inject] IServiceProvider ServiceProvider { get; set; } = default!;
    [Inject] HttpClient Http { get; set; } = default!;

    [CascadingParameter]
    private Task<AuthenticationState>? AuthenticationStateTask { get; set; }

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


    private string? userFullName;
    private bool isAuthenticated;
    private ShiftIdentityBlazorOptions? identityOptions;
    private IIdentityStore? tokenStore;

    protected override async Task OnInitializedAsync()
    {
        identityOptions = ServiceProvider.GetService<ShiftIdentityBlazorOptions>();
        tokenStore = ServiceProvider.GetService<IIdentityStore>();

        if (AuthenticationStateTask != null)
        {
            var authState = await AuthenticationStateTask;
            var user = authState.User;

            if (user.Identity?.IsAuthenticated == true)
            {
                isAuthenticated = true;
                userFullName = user.FindFirst(ClaimTypes.GivenName)?.Value
                    ?? user.FindFirst(ClaimTypes.Name)?.Value;
            }
        }
        else if (tokenStore != null)
        {
            // Fallback for standalone WASM with IIdentityStore
            isAuthenticated = true;
            var tokenData = await tokenStore.GetTokenAsync();
            userFullName = tokenData?.UserData?.FullName;
        }

    }

    internal async Task Logout()
    {
        if (!isAuthenticated)
            return;

        if (identityOptions?.UseCookieAuth == true)
        {
            try { await Http.PostAsync("/api/identity/logout", null); } catch { }
        }

        if (tokenStore != null)
        {
            await tokenStore.RemoveTokenAsync();
        }

        NavigationManager.NavigateTo("/", true);
    }

    internal async Task OpenSettings() => await Dialog.ShowAsync<Settings>();

    internal void IsOpenHandler(bool open) => IsOpen = open;

    internal void GoToIdentity()
    {
        if (identityOptions == null)
            return;
        NavigationManager.NavigateTo($"{identityOptions.FrontEndBaseUrl}/{Constants.IdentityRoutePreifix}/UserDataForm");
    }
}
