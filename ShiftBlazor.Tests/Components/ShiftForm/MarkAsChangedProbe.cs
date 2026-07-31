using Microsoft.AspNetCore.Components;

namespace ShiftSoftware.ShiftBlazor.Tests.Components.ShiftForm;

/// <summary>
/// Stands in for the real-world component that makes the out-of-band change — a side panel or an
/// embedded survey that PUTs its own endpoint while the hosting form never saves. It reaches the
/// form the same way such a component would: through the <c>ShiftForm</c> cascading value, typed
/// as <see cref="IShiftForm"/>, which does not require knowing the form's DTO type.
/// </summary>
public class MarkAsChangedProbe : ComponentBase
{
    [CascadingParameter(Name = "ShiftForm")]
    public IShiftForm? ShiftForm { get; set; }

    /// <summary>Mirrors what a consumer calls after its own endpoint returns success.</summary>
    public void ReportChange() => ShiftForm?.MarkAsChanged();
}
