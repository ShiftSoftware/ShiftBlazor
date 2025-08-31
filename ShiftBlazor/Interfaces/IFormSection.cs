using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;


namespace ShiftSoftware.ShiftBlazor.Interfaces;

public interface IFormSection
{
    public RenderFragment? ChildContent { get; set; }
    public string? For { get; set; }
    public bool IsValid { get; }

    public void Add(FieldIdentifier formControl);

    public void Remove(FieldIdentifier formControl);

}
