using FluentValidation;
using Microsoft.AspNetCore.Components.Forms;
using ShiftSoftware.ShiftBlazor.Enums;
using ShiftSoftware.ShiftBlazor.Interfaces;

namespace ShiftSoftware.ShiftBlazor.Components;

public interface IShiftForm : IStandaloneComponent
{
    public FormModes Mode { get; set; }
    public FormTasks TaskInProgress { get; set; }
    public EditContext EditContext { get; set; }
    public IValidator? Validator { get; }

    public bool AddSection(FormSection section);
    public bool RemoveSection(FormSection section);

    public List<FormSection> GetSections();

    public bool Validate();
    public bool Validate(List<FieldIdentifier> fields);
    public void DisplayError(string field, string message);
    public void DisplayError(FieldIdentifier field, string message);


}
