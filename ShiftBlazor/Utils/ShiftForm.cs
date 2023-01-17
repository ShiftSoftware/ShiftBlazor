using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Reflection;
using ShiftSoftware.ShiftEntity.Core.Dtos;
using ShiftSoftware.ShiftBlazor.Components;

namespace ShiftSoftware.ShiftBlazor.Utils
{
    public partial class ShiftForm<TComponent, T> : ComponentBase where TComponent : ComponentBase where T : ShiftEntityDTO, new()
    {
        [CascadingParameter] MudDialogInstance? MudDialog { get; set; }
        [Parameter] public Form.States State { get; set; } = Form.States.View;
        [Parameter] public object? Key { get; set; }
        public FormSettings FormSetting { get; set; } = new FormSettings();
        public string Url { get; set; }
        public T TheItem { get; set; } = new T();
        public ShiftFormContainer<T>? FormContainer { get; set; }

        public ShiftForm()
        {
            var attr = typeof(TComponent).GetCustomAttribute<RouteAttribute>(false);
            Url = attr?.Template ?? "";
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            //isReadOnly = FormMode == Form.Modes.View;
        }
    }
}
