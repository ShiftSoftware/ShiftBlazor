using FluentValidation;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using MudBlazor;
using ShiftSoftware.ShiftBlazor.Services;
using System.Text.Json;
using static ShiftSoftware.ShiftBlazor.Utils.Form;

namespace ShiftSoftware.ShiftBlazor.Components
{
    public partial class ShiftFormBasic<T> where T : class, new()
    {
        [Inject] ShiftModalService ShiftModal { get; set; } = default!;
        [Inject] IDialogService DialogService { get; set; } = default!;

        [CascadingParameter]
        protected MudDialogInstance? MudDialog { get; set; }

        /// <summary>
        /// The current State of the form.
        /// </summary>
        [Parameter]
        public States State { get; set; }

        /// <summary>
        /// An event triggered when the state of the State paramater has changed.
        /// </summary>
        [Parameter]
        public EventCallback<States> StateChanged { get; set; }

        /// <summary>
        /// The current item being view/edited, this will be fetched from the API endpoint that is provided in the Action paramater.
        /// </summary>
        [Parameter]
        public T Value { get; set; } = new T();

        /// <summary>
        /// An event triggered when the state of Value has changed.
        /// </summary>
        [Parameter]
        public EventCallback<T> ValueChanged { get; set; }

        /// <summary>
        /// The title to render on the form header.
        /// </summary>
        [Parameter]
        public string? Title { get; set; }

        [Parameter]
        public RenderFragment? ChildContent { get; set; }

        /// <summary>
        /// The icon displayed before the Form Title, in a string in SVG format.
        /// </summary>
        [Parameter]
        public string IconSvg { get; set; } = @Icons.Material.Filled.ListAlt;

        /// <summary>
        /// Model Validator object.
        /// If Validator is not set, reflection will be used to find the model validator.
        /// Otherwise if it is set, reflection will be disabled and DataAnnotationsValidator will also be disabled.
        /// </summary>
        [Parameter]
        public AbstractValidator<T>? Validator { get; set; }

        /// <summary>
        /// Used to add custom elements to the header.
        /// </summary>
        [Parameter]
        public RenderFragment? HeaderTemplate { get; set; }

        /// <summary>
        /// Used to add custom elements to the footer.
        /// </summary>
        [Parameter]
        public RenderFragment? FooterTemplate { get; set; }

        /// <summary>
        /// Used to add custom elements to the start of the header toolbar.
        /// </summary>
        [Parameter]
        public RenderFragment? ToolbarStartTemplate { get; set; }

        /// <summary>
        /// Used to add custom elements to the end of the header toolbar.
        /// </summary>
        [Parameter]
        public RenderFragment? ToolbarEndTemplate { get; set; }

        /// <summary>
        /// Used to add custom elements to the center of the header toolbar.
        /// </summary>
        [Parameter]
        public RenderFragment? ToolbarCenterTemplate { get; set; }

        /// <summary>
        /// Used to add custom elements to the controls section of the header toolbar.
        /// This section is only visible when the form is opened in a dialog.
        /// </summary>
        [Parameter]
        public RenderFragment? ToolbarControlsTemplate { get; set; }

        /// <summary>
        /// Used to add custom elements to the start of the header toolbar.
        /// </summary>
        [Parameter]
        public RenderFragment? FooterToolbarStartTemplate { get; set; }

        /// <summary>
        /// Used to add custom elements to the end of the header toolbar.
        /// </summary>
        [Parameter]
        public RenderFragment? FooterToolbarEndTemplate { get; set; }

        /// <summary>
        /// Used to add custom elements to the center of the header toolbar.
        /// </summary>
        [Parameter]
        public RenderFragment? FooterToolbarCenterTemplate { get; set; }

        /// <summary>
        /// If true, the toolbar in the header will not be rendered
        /// </summary>
        [Parameter]
        public bool DisableHeaderToolbar { get; set; }

        /// <summary>
        /// If true, the toolbar in the footer will not be rendered
        /// </summary>
        [Parameter]
        public bool DisableFooterToolbar { get; set; }

        [Parameter]
        public EventCallback OnInvalidSubmit { get; set; }
        [Parameter]
        public EventCallback OnValidSubmit { get; set; }

        protected string? DocumentTitle {get; set; }
        protected States StateBeforeSaving { get; set; }
        protected bool IsModified { get; set; }

        private EditContext editContext;

        protected override void OnInitialized()
        {
            base.OnInitialized();

            editContext = new(Value);

            //editContext.OnFieldChanged += FieldChangedHandler;
            //NavManager.RegisterLocationChangingHandler(LocationChangingHandler);

            State = StateBeforeSaving = States.Create;
        }

        public ValueTask LocationChangingHandler(LocationChangingContext ctx)
        {
            if (IsModified)
            {
                ctx.PreventNavigation();
            }
            return new ValueTask();
        }

        public void FieldChangedHandler(object? sender, FieldChangedEventArgs args)
        {
            IsModified = true;
        }

        private async Task Cancel()
        {
            if (editContext.IsModified())
            {
                var dialogOptions = new DialogOptions
                {
                    MaxWidth = MaxWidth.ExtraSmall,
                };

                bool? result = await DialogService.ShowMessageBox(
                    "Warning",
                    "You have unsaved changes, do you want to cancel?",
                    yesText: "Yes",
                    cancelText: "No",
                    options: dialogOptions);

                if (!result.HasValue || result.HasValue && !result.Value)
                {
                    return;
                }
            }

            if (MudDialog != null)
            {
                ShiftModal.Close(MudDialog);
            }
        }

        protected async Task SetState(States state)
        {
            State = state;
            await StateChanged.InvokeAsync(State);
        }

        private async Task InvalidSubmitHandler(EditContext context)
        {
            await OnInvalidSubmit.InvokeAsync();
        }

        private async Task ValidSubmitHandler(EditContext context)
        {
            StateBeforeSaving = State;
            await SetState(States.Saving);

            await OnValidSubmit.InvokeAsync();

            await SetState(StateBeforeSaving);
        }
    }
}
