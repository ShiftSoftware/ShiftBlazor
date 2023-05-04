﻿using FluentValidation;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.Localization;
using MudBlazor;
using ShiftSoftware.ShiftBlazor.Services;
using ShiftSoftware.ShiftBlazor.Enums;

namespace ShiftSoftware.ShiftBlazor.Components
{
    public partial class ShiftFormBasic<T> where T : class, new()
    {
        [Inject] MessageService MsgService { get; set; } = default!;
        [Inject] ShiftModal ShiftModal { get; set; } = default!;
        [Inject] IDialogService DialogService { get; set; } = default!;
        [Inject] IStringLocalizer<Resources.Components.ShiftFormBasic> Loc { get; set; } = default!;

        [CascadingParameter]
        internal MudDialogInstance? MudDialog { get; set; }

        /// <summary>
        ///     The current Mode of the form.
        /// </summary>
        [Parameter]
        public FormModes Mode { get; set; }

        /// <summary>
        ///     An event triggered when the state of the Mode paramater has changed.
        /// </summary>
        [Parameter]
        public EventCallback<FormModes> ModeChanged { get; set; }

        /// <summary>
        ///     The current item being view/edited, this will be fetched from the API endpoint that is provided in the Action
        ///     paramater.
        /// </summary>
        [Parameter]
        public T Value { get; set; } = new();

        /// <summary>
        ///     An event triggered when the state of Value has changed.
        /// </summary>
        [Parameter]
        public EventCallback<T> ValueChanged { get; set; }

        /// <summary>
        ///     The title to render on the form header.
        /// </summary>
        [Parameter]
        public string? Title { get; set; }

        [Parameter]
        public RenderFragment? ChildContent { get; set; }

        /// <summary>
        ///     The icon displayed before the Form Title, in a string in SVG format.
        /// </summary>
        [Parameter]
        public string IconSvg { get; set; } = Icons.Material.Filled.ListAlt;

        /// <summary>
        ///     Model Validator object.
        ///     If Validator is not set, reflection will be used to find the model validator.
        ///     Otherwise if it is set, reflection will be disabled and DataAnnotationsValidator will also be disabled.
        /// </summary>
        [Parameter]
        public AbstractValidator<T>? Validator { get; set; }

        /// <summary>
        ///     Used to add custom elements to the header.
        /// </summary>
        [Parameter]
        public RenderFragment? HeaderTemplate { get; set; }

        /// <summary>
        ///     Used to add custom elements to the footer.
        /// </summary>
        [Parameter]
        public RenderFragment? FooterTemplate { get; set; }

        /// <summary>
        ///     Used to add custom elements to the start of the header toolbar.
        /// </summary>
        [Parameter]
        public RenderFragment? ToolbarStartTemplate { get; set; }

        /// <summary>
        ///     Used to add custom elements to the end of the header toolbar.
        /// </summary>
        [Parameter]
        public RenderFragment? ToolbarEndTemplate { get; set; }

        /// <summary>
        ///     Used to add custom elements to the center of the header toolbar.
        /// </summary>
        [Parameter]
        public RenderFragment? ToolbarCenterTemplate { get; set; }

        /// <summary>
        ///     Used to add custom elements to the controls section of the header toolbar.
        ///     This section is only visible when the form is opened in a dialog.
        /// </summary>
        [Parameter]
        public RenderFragment? ToolbarControlsTemplate { get; set; }

        /// <summary>
        ///     Used to add custom elements to the start of the header toolbar.
        /// </summary>
        [Parameter]
        public RenderFragment? FooterToolbarStartTemplate { get; set; }

        /// <summary>
        ///     Used to add custom elements to the end of the header toolbar.
        /// </summary>
        [Parameter]
        public RenderFragment? FooterToolbarEndTemplate { get; set; }

        /// <summary>
        ///     Used to add custom elements to the center of the header toolbar.
        /// </summary>
        [Parameter]
        public RenderFragment? FooterToolbarCenterTemplate { get; set; }

        /// <summary>
        ///     If true, the toolbar in the header will not be rendered
        /// </summary>
        [Parameter]
        public bool DisableHeaderToolbar { get; set; }

        /// <summary>
        ///     If true, the toolbar in the footer will not be rendered
        /// </summary>
        [Parameter]
        public bool DisableFooterToolbar { get; set; }

        [Parameter]
        public EventCallback<EditContext> OnInvalidSubmit { get; set; }
        [Parameter]
        public EventCallback<EditContext> OnValidSubmit { get; set; }
        [Parameter]
        public EventCallback<EditContext> OnSubmit { get; set; }

        [Parameter]
        public EventCallback<FormTasks> OnTaskStart { get; set; }
        [Parameter]
        public EventCallback<FormTasks> OnTaskFinished { get; set; }

        public EventCallback<FormTasks> _OnTaskStart { get; set; }
        public EventCallback<FormTasks> _OnTaskFinished { get; set; }

        [Parameter]
        public string? DocumentTitle { get; set; }

        [Parameter]
        public FormSettings Settings { get; set; } = new FormSettings();

        [Parameter]
        public string? SubmitText { get; set; }

        internal virtual bool HideSubmit { get; set; }
        internal virtual string _SubmitText { get; set; }
        internal FormTasks TaskInProgress { get; set; }
        internal bool AlertEnabled { get; set; } = false;
        internal MudBlazor.Severity AlertSeverity { get; set; }
        internal string AlertMessage { get; set; } = default!;

        internal EditContext editContext = default!;
        internal string ContentCssClass
        {
            get
            {
                var css = "form-body";
                if (MudDialog != null) css += " shift-scrollable-content-wrapper";
                return css;
            }
        }

        internal bool DisableSubmit => TaskInProgress != FormTasks.None;

        protected override void OnInitialized()
        {
            editContext = new EditContext(Value);

            _SubmitText = string.IsNullOrWhiteSpace(SubmitText)
                ? Loc["SubmitTextDefault"]
                : SubmitText;

            //NavManager.RegisterLocationChangingHandler(LocationChangingHandler);
        }

        protected override async Task OnInitializedAsync()
        {
            await SetMode(FormModes.Create);
            DocumentTitle = Title;
        }

        //internal ValueTask LocationChangingHandler(LocationChangingContext ctx)
        //{
        //    if (editContext.IsModified())
        //    {
        //        ctx.PreventNavigation();
        //    }
        //    return new ValueTask();
        //}


        internal async Task Cancel()
        {
            if (MudDialog != null && await ConfirmClose())
            {
                ShiftModal.Close(MudDialog);
            }
        }

        internal async Task<bool> ConfirmClose(string message = "You have unsaved changes, do you want to cancel?")
        {
            if (editContext.IsModified())
            {
                var dialogOptions = new DialogOptions
                {
                    MaxWidth = MaxWidth.ExtraSmall,
                };

                var result = await DialogService.ShowMessageBox(
                        "Warning",
                        message,
                        yesText: "Yes",
                        cancelText: "No",
                        options: dialogOptions);


                return result.HasValue && result.Value;
            }

            return true;
        }

        internal virtual async Task SetMode(FormModes mode)
        {
            Mode = mode;
            await ModeChanged.InvokeAsync(Mode);
        }

        internal virtual async Task SetValue(T? value)
        {
            if (value != null)
            {
                Value = value;
                await ValueChanged.InvokeAsync(Value);
                editContext = new EditContext(Value);
                editContext.MarkAsUnmodified();
            }
        }

        internal virtual async Task InvalidSubmitHandler(EditContext context)
        {
            await OnInvalidSubmit.InvokeAsync(context);
        }

        internal virtual async Task ValidSubmitHandler(EditContext context)
        {
            await OnValidSubmit.InvokeAsync(context);
        }

        internal virtual async Task SubmitHandler(EditContext context)
        {
            await RunTask(FormTasks.Save, async () =>
            {
                await OnSubmit.InvokeAsync(context);

                if (context.Validate())
                {
                    await ValidSubmitHandler(context);
                }
                else
                {
                    await InvalidSubmitHandler(context);
                }
            });
        }

        internal void ShowAlert(string message, MudBlazor.Severity severity = MudBlazor.Severity.Warning, int? durationInSeconds = null)
        {
            AlertSeverity = severity;
            AlertMessage = message;
            AlertEnabled = true;
            InvokeAsync(StateHasChanged);
            Task.Run(async () =>
            {
                if (durationInSeconds.HasValue)
                {
                    await Task.Delay(durationInSeconds.Value * 1000);
                    AlertEnabled = false;
                    await InvokeAsync(StateHasChanged);
                }
                else
                {
                    AlertEnabled = false;
                }
            });
        }

        internal async Task RunTask(FormTasks Task, Func<ValueTask> action)
        {
            if (TaskInProgress != FormTasks.None)
            {
                return;
            }

            await OnTaskStart.InvokeAsync(Task);
            await _OnTaskStart.InvokeAsync(Task);

            TaskInProgress = Task;

            try
            {
                await action.Invoke();
            }
            catch (Exception e)
            {
                MsgService.Error($"Could not {Task} the item.", e.Message, e.ToString());
            }
            finally
            {
                TaskInProgress = FormTasks.None;
                await OnTaskFinished.InvokeAsync(Task);
                await _OnTaskFinished.InvokeAsync(Task);
            }
        }
    }

}