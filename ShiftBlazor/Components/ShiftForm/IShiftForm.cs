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


}
