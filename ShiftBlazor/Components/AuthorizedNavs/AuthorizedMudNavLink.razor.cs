using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace ShiftSoftware.ShiftBlazor.Components;

public partial class AuthorizedMudNavLink : MudNavLink
{
    [Inject] internal TypeAuth.Core.ITypeAuthService TypeAuthService { get; set; } = default!;
    [Parameter]
    public TypeAuth.Core.Actions.Action? TypeAuthAction { get; set; }
}
