using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace ShiftSoftware.ShiftBlazor.Components;

public partial class AuthorizedMudNavGroup : MudNavGroup
{
    [Inject] internal TypeAuth.Blazor.Services.TypeAuthService TypeAuthService { get; set; } = default!;
    [Parameter]
    public List<TypeAuth.Core.Actions.Action>? TypeAuthActions { get; set; }
}
