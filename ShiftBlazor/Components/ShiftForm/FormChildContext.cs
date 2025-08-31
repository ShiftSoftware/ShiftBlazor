using Microsoft.AspNetCore.Components;
using ShiftSoftware.ShiftBlazor.Enums;

namespace ShiftSoftware.ShiftBlazor.Components;

public class FormChildContext<T> where T : class, new() 
{
    public ShiftFormBasic<T> Self { get; }
    public FormModes Mode => Self.Mode;
    public FormTasks CurrentTask => Self.TaskInProgress;
    public bool ReadOnly => Mode < FormModes.Edit;
    public bool Disabled => CurrentTask != FormTasks.None;
    public T Item => Self.Value;
    public Guid Id => Self.Id;
    public Func<Task> CloseModal;
    public Func<FormModes, Task> SetMode;
    public Action<string, MudBlazor.Severity, int?> ShowAlert;
    public Func<bool> Validate;

    public FormChildContext(ShiftFormBasic<T> self)
    {
        Self = self;
        CloseModal = self.Cancel;
        SetMode = self.SetMode;
        ShowAlert = self.ShowAlert;
        Validate = self.Validate;
    }
}