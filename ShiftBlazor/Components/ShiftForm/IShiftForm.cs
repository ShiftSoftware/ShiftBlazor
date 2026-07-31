using FluentValidation;
using Microsoft.AspNetCore.Components.Forms;
using ShiftSoftware.ShiftBlazor.Enums;

namespace ShiftSoftware.ShiftBlazor.Components;

public interface IShiftForm
{
    public Guid Id { get; }
    public string? Title { get; set; }
    public FormModes Mode { get; set; }
    public FormTasks TaskInProgress { get; set; }
    public string IconSvg { get; set; }
    public string? NavColor { get; set; }
    public bool NavIconFlatColor { get; set; }
    public EditContext EditContext { get; set; }
    public IValidator? Validator { get; }

    public bool AddSection(FormSection section);
    public bool RemoveSection(FormSection section);

    public List<FormSection> GetSections();

    public bool Validate();
    public bool Validate(List<FieldIdentifier> fields);
    public void DisplayError(string field, string message);
    public void DisplayError(FieldIdentifier field, string message);

    /// <summary>
    ///     Marks the form as changed, so the hosting <see cref="ShiftList{T}"/> reloads when the
    ///     form closes. See <see cref="ShiftFormBasic{T}.MarkAsChanged"/> for details.
    /// </summary>
    /// <remarks>
    ///     Exposed on the interface so components nested inside the form — which receive it
    ///     through the <c>ShiftForm</c> cascading value and do not know the form's DTO type —
    ///     can report their own out-of-band changes:
    ///     <code>
    ///     [CascadingParameter(Name = "ShiftForm")] public IShiftForm? ShiftForm { get; set; }
    ///     // ... after a successful PUT to this component's own endpoint:
    ///     ShiftForm?.MarkAsChanged();
    ///     </code>
    /// </remarks>
    public void MarkAsChanged();


}
