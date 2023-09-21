using FluentValidation;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.Localization;
using MudBlazor;
using ShiftSoftware.ShiftBlazor.Services;
using ShiftSoftware.ShiftBlazor.Extensions;
using ShiftSoftware.ShiftBlazor.Enums;
using ShiftSoftware.ShiftBlazor.Utils;
using ShiftSoftware.ShiftEntity.Model;

namespace ShiftSoftware.ShiftBlazor.Components
{
    public partial class ShiftFormBasic<T> where T : class, new()
    {
        [Inject] MessageService MsgService { get; set; } = default!;
        [Inject] ShiftModal ShiftModal { get; set; } = default!;
        [Inject] IDialogService DialogService { get; set; } = default!;
        [Inject] SettingManager SettingManager { get; set; } = default!;
        [Inject] IStringLocalizer<Resources.Components.ShiftFormBasic> Loc { get; set; } = default!;

        [Inject] internal TypeAuth.Core.ITypeAuthService TypeAuthService { get; set; } = default!;

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
        public EventCallback<ShiftEvent<EditContext>> OnInvalidSubmit { get; set; }
        [Parameter]
        public EventCallback<ShiftEvent<EditContext>> OnValidSubmit { get; set; }
        [Parameter]
        public EventCallback<ShiftEvent<EditContext>> OnSubmit { get; set; }

        [Parameter]
        public EventCallback<ShiftEvent<FormTasks>> OnTaskStart { get; set; }
        [Parameter]
        public EventCallback<FormTasks> OnTaskFinished { get; set; }

        public EventCallback<FormTasks> _OnTaskStart { get; set; }
        public EventCallback<FormTasks> _OnTaskFinished { get; set; }

        [Parameter]
        public string? DocumentTitle { get; set; }

        [Parameter]
        public FormOnSaveAction? OnSaveAction { get; set; }

        [Parameter]
        public string? SubmitText { get; set; }

        [Parameter]
        public TypeAuth.Core.Actions.Action? TypeAuthAction { get; set; }

        [Parameter]
        public string? NavColor { get; set; }

        [Parameter]
        public bool NavIconFlatColor { get; set; }

        internal virtual bool HideSubmit { get; set; }
        internal virtual string _SubmitText { get; set; }
        internal FormTasks TaskInProgress { get; set; }
        internal bool AlertEnabled { get; set; } = false;
        internal MudBlazor.Severity AlertSeverity { get; set; }
        internal string AlertMessage { get; set; } = default!;

        internal EditContext editContext = default!;
        internal bool MadeChanges = false;
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
            OnSaveAction = SettingManager.Settings.FormOnSaveAction ?? OnSaveAction ?? DefaultAppSetting.FormOnSaveAction;

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
                var val = MadeChanges ? Value : null;
                ShiftModal.Close(MudDialog, val);
            }
        }

        internal async Task<bool> ConfirmClose(string? messageBody = null)
        {
            if (editContext.IsModified())
            {

                var message = new Message
                {
                    Title = Loc["CancelWarningTitle"],
                    Body = messageBody ?? Loc["CancelWarningMessage"],
                };

                var parameters = new DialogParameters
                {
                    { "Message", message },
                    { "Color", Color.Warning },
                    { "ConfirmText",  Loc["CancelConfirmText"].ToString() },
                    { "CancelText",  Loc["CancelDeclineText"].ToString() },
                };

                var result = await DialogService.Show<PopupMessage>("", parameters, new DialogOptions
                {
                    MaxWidth = MaxWidth.ExtraSmall,
                    NoHeader = true,
                }).Result;

                return !result.Canceled;
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
            await OnInvalidSubmit.PreventableInvokeAsync(context);
        }

        internal virtual async Task ValidSubmitHandler(EditContext context)
        {
            await OnValidSubmit.PreventableInvokeAsync(context);
        }

        internal virtual async Task SubmitHandler(EditContext context)
        {
            await RunTask(FormTasks.Save, async () =>
            {
                if (await OnSubmit.PreventableInvokeAsync(context)) return;

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

            await _OnTaskStart.InvokeAsync(Task);
            if (await OnTaskStart.PreventableInvokeAsync(Task)) return;

            TaskInProgress = Task;

            try
            {
                await action.Invoke();
            }
            catch (Exception e)
            {
                MsgService.Error($"Could not {Task} the item.", e.Message, e.ToString());
            }

            TaskInProgress = FormTasks.None;
            await _OnTaskFinished.InvokeAsync(Task);
            await OnTaskFinished.InvokeAsync(Task);
        }
    }

}
