using Microsoft.AspNetCore.Components;
using MudBlazor;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using ShiftSoftware.ShiftBlazor.Enums;
using ShiftSoftware.ShiftBlazor.Services;
using System.Text.Json;

namespace ShiftSoftware.ShiftBlazor.Components
{
    public partial class ShiftForm<TComponent, T> : ComponentBase where TComponent : ComponentBase where T : ShiftEntityViewAndUpsertDTO, new()
    {
        [Inject] private SettingManager SettingManager { get; set; } = default!;
        [CascadingParameter] public IMudDialogInstance? MudDialog { get; set; }
        [Parameter] public FormModes Mode { get; set; } = FormModes.View;
        [Parameter] public object? Key { get; set; }
        [Parameter] public FormOnSaveAction FormSetting { get; set; } = FormOnSaveAction.ViewFormOnSave;
        [Parameter] public T TheItem { get; set; } = new();
        public ShiftEntityForm<T>? FormContainer { get; set; }

        public bool ReadOnly => Mode < FormModes.Edit;
        public bool Disabled => Task != FormTasks.None;
        public FormTasks Task { get; set; }

        public override Task SetParametersAsync(ParameterView parameters)
        {
            var filteredParameters = new Dictionary<string, object?>();

            foreach (var parameter in parameters)
            {
                if (parameter.Name == "TheItem")
                {
                    try
                    {
                        if (parameter.Value is JsonElement itemValue)
                        {
                            TheItem = itemValue.Deserialize<T>() ?? new();
                        }
                        else
                        {
                            TheItem = (T)parameter.Value;
                        }
                    }
                    catch { }
                }
                else if (!parameter.Cascading)
                {
                    filteredParameters.Add(parameter.Name, parameter.Value);
                }
            }

            return base.SetParametersAsync(ParameterView.FromDictionary(filteredParameters));
        }

        protected override void OnInitialized()
        {
            FormSetting = SettingManager.GetFormOnSaveAction();
            base.OnInitialized();
        }

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
