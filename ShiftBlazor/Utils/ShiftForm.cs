using Microsoft.AspNetCore.Components;
using MudBlazor;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using ShiftSoftware.ShiftBlazor.Components;

namespace ShiftSoftware.ShiftBlazor.Utils
{
    public partial class ShiftForm<TComponent, T> : ComponentBase where TComponent : ComponentBase where T : ShiftEntityDTO, new()
    {
        [CascadingParameter] public MudDialogInstance? MudDialog { get; set; }
        [Parameter] public Form.Modes Mode { get; set; } = Form.Modes.View;
        [Parameter] public object? Key { get; set; }
        public FormSettings FormSetting { get; set; } = new();
        public T TheItem { get; set; } = new();
        public ShiftEntityForm<T>? FormContainer { get; set; }

        public bool ReadOnly => Mode < Form.Modes.Edit;
        public bool Disabled => Task != Form.Tasks.None;
        public Form.Tasks Task { get; set; }

        protected override void OnAfterRender(bool firstRender)
        {
            base.OnAfterRender(firstRender);

            if (FormContainer != null && firstRender)
            {
                FormContainer._OnTaskStart = new EventCallbackFactory().Create<Form.Tasks>(this, TaskStartHandler);
                FormContainer._OnTaskFinished = new EventCallbackFactory().Create<Form.Tasks>(this, TaskFinishHandler);
            }
        }

        public void TaskStartHandler(Form.Tasks task)
        {
            Task = task;
        }

        public void TaskFinishHandler(Form.Tasks task)
        {
            Task = Form.Tasks.None;
        }
    }
}
