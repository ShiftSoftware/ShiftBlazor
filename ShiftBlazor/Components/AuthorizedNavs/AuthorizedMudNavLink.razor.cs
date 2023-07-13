using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace ShiftSoftware.ShiftBlazor.Components;

public partial class AuthorizedMudNavLink : MudNavLink
{
    [Inject] internal TypeAuth.Blazor.Services.TypeAuthService TypeAuthService { get; set; } = default!;
    [Parameter]
    public TypeAuth.Core.Actions.Action? TypeAuthAction { get; set; }
}
