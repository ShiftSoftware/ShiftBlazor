using Microsoft.AspNetCore.Components;
using MudBlazor;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using ShiftSoftware.ShiftBlazor.Enums;

namespace ShiftSoftware.ShiftBlazor.Components
{
    public partial class ShiftForm<TComponent, T> : ComponentBase where TComponent : ComponentBase where T : ShiftEntityDTO, new()
    {
        [CascadingParameter] public MudDialogInstance? MudDialog { get; set; }
        [Parameter] public FormModes Mode { get; set; } = FormModes.View;
        [Parameter] public object? Key { get; set; }
        public FormSettings FormSetting { get; set; } = new();
        public T TheItem { get; set; } = new();
        public ShiftEntityForm<T>? FormContainer { get; set; }

        public bool ReadOnly => Mode < FormModes.Edit;
        public bool Disabled => Task != FormTasks.None;
        public FormTasks Task { get; set; }

        protected override void OnAfterRender(bool firstRender)
        {
            base.OnAfterRender(firstRender);

            if (FormContainer != null && firstRender)
            {
                FormContainer._OnTaskStart = new EventCallbackFactory().Create<FormTasks>(this, TaskStartHandler);
                FormContainer._OnTaskFinished = new EventCallbackFactory().Create<FormTasks>(this, TaskFinishHandler);
            }
        }

        public void TaskStartHandler(FormTasks task)
        {
            Task = task;
        }

        public void TaskFinishHandler(FormTasks task)
        {
            Task = FormTasks.None;
        }
    }
}
