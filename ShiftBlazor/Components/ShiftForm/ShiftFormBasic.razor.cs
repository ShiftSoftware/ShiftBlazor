using FluentValidation;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MudBlazor;
using MudBlazor.Utilities;
using ShiftSoftware.ShiftBlazor.Enums;
using ShiftSoftware.ShiftBlazor.Extensions;
using ShiftSoftware.ShiftBlazor.Interfaces;
using ShiftSoftware.ShiftBlazor.Localization;
using ShiftSoftware.ShiftBlazor.Services;
using ShiftSoftware.ShiftBlazor.Utils;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.TypeAuth.Core;
using ShiftSoftware.TypeAuth.Core.Actions;

namespace ShiftSoftware.ShiftBlazor.Components;

[CascadingTypeParameter(nameof(T))]
public partial class ShiftFormBasic<T> : IShortcutComponent, IShiftForm where T : class, new()
{
    [Inject] MessageService MsgService { get; set; } = default!;
    [Inject] ShiftModal ShiftModal { get; set; } = default!;
    [Inject] IDialogService DialogService { get; set; } = default!;
    [Inject] SettingManager SettingManager { get; set; } = default!;
    [Inject] ShiftBlazorLocalizer Loc { get; set; } = default!;
    [Inject] IServiceProvider ServiceProvider { get; set; } = default!;

    [CascadingParameter]
    public IMudDialogInstance? MudDialog { get; set; }

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
    public string? Title { get; set; } = string.Empty;

    [Parameter]
    public RenderFragment<FormChildContext<T>>? ChildContent { get; set; }

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
    public IValidator? Validator { get; set; }

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

    [Parameter]
    public EventCallback OnReady { get; set; }

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

    [Parameter]
    public bool HideSubmit { get; set; }

    [Parameter]
    public bool AutoFocus { get; set; } = true;

    [Parameter]
    public bool OnlyValidateOnSubmit { get; set;  }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public FormTasks TaskInProgress { get; set; }
    public Dictionary<KeyboardKeys, object> Shortcuts { get; set; } = new();
    public EditForm? Form { get; set; }
    public EditContext EditContext { get; set; } = default!;
    internal virtual string _SubmitText { get; set; } = "Submit";
    internal bool AlertEnabled { get; set; } = false;
    internal MudBlazor.Severity AlertSeverity { get; set; }
    internal string AlertMessage { get; set; } = default!;

    internal bool MadeChanges = false;

    protected ITypeAuthService? TypeAuthService;
    internal bool HasWriteAccess = true;
    internal bool HasDeleteAccess = true;
    internal bool HasReadAccess = true;
    internal ShiftValidator? shiftValidator;
    internal virtual bool IsFooterToolbarEmpty => FooterToolbarStartTemplate == null
                                                  && FooterToolbarCenterTemplate == null
                                                  && FooterToolbarEndTemplate == null
                                                  && (HideSubmit || !HasWriteAccess);
    internal bool _RenderSubmitButton => Mode >= FormModes.Edit && !HideSubmit && HasWriteAccess;

    internal ElementReference ContentContainerRef = default!;
    internal string ContentCssClass =>
        new CssBuilder("form-body")
            .AddClass("shift-scrollable-content-wrapper", MudDialog != null)
            .Build();

    internal bool DisableSubmit => TaskInProgress != FormTasks.None;

    protected override void OnInitialized()
    {
        IShortcutComponent.Register(this);

        TypeAuthService = ServiceProvider.GetService<ITypeAuthService>();

        if (TypeAuthAction is ReadWriteDeleteAction action)
        {
            HasReadAccess = TypeAuthService?.CanRead(action) == true;
            HasWriteAccess = TypeAuthService?.CanWrite(action) == true;
            HasDeleteAccess = TypeAuthService?.CanDelete(action) == true;
        }

        OnSaveAction = SettingManager.Settings.FormOnSaveAction ?? OnSaveAction ?? DefaultAppSetting.FormOnSaveAction;

        EditContext = new EditContext(Value);

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

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender && AutoFocus)
        {
            Task.Delay(10).ContinueWith(async x =>
            {
                await ContentContainerRef.MudFocusFirstAsync();
            });
        }
        base.OnAfterRender(firstRender);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && Mode == FormModes.Create)
        {
            await OnReady.InvokeAsync();
        }
        await base.OnAfterRenderAsync(firstRender);
    }

    //internal ValueTask LocationChangingHandler(LocationChangingContext ctx)
    //{
    //    if (editContext.IsModified())
    //    {
    //        ctx.PreventNavigation();
    //    }
    //    return new ValueTask();
    //}

    public virtual async ValueTask HandleShortcut(KeyboardKeys key)
    {
        switch (key)
        {
            case KeyboardKeys.Escape:
                await Cancel();
                break;

            case KeyboardKeys.KeyS:
                if (Form != null && _RenderSubmitButton)
                {
                    await Form.OnSubmit.InvokeAsync(Form.EditContext);
                }
                break;
        }
    }

    internal async Task Cancel()
    {
        if (MudDialog != null && await ConfirmClose())
        {
            var val = MadeChanges ? Value : null;
            ShiftModal.Close(MudDialog, val);
            IShortcutComponent.Remove(Id);
        }
    }

    internal async Task<bool> ConfirmClose(string? messageBody = null)
    {
        if (EditContext?.IsModified() == true)
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

            var dialogReference = await DialogService.ShowAsync<PopupMessage>("", parameters, new DialogOptions
            {
                MaxWidth = MaxWidth.ExtraSmall,
                NoHeader = true,
                CloseOnEscapeKey = false,
            });

            var result = await dialogReference.Result;

            return result?.Canceled != true;
        }

        return true;
    }

    internal virtual async Task SetMode(FormModes mode)
    {
        Mode = mode;
        await ModeChanged.InvokeAsync(Mode);
        StateHasChanged();
    }

    internal virtual async Task SetValue(T? value)
    {
        if (value != null)
        {
            Value = value;
            await ValueChanged.InvokeAsync(Value);
            EditContext = new EditContext(Value);
            EditContext.MarkAsUnmodified();
            StateHasChanged();
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

    internal virtual async Task SubmitHandler(EditContext context, FormTasks formTask = FormTasks.Save)
    {
        await RunTask(formTask, async () =>
        {
            if (await OnSubmit.PreventableInvokeAsync(context)) return;

            if (context.Validate())
            {
                await ValidSubmitHandler(context);
            }
            else
            {
                // await JsRuntime.InvokeVoidAsync("scrollToFirstError", $"Form-{Id}");
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
            #if DEBUG
                MsgService.Error(Loc["FormTaskError", Loc[$"FormTask{Task.ToString()}"]], e.Message, e.ToString(), buttonText: Loc["DropdownViewButtonText"]);
            #else
                MsgService.Error(Loc["FormTaskError", Loc[$"FormTask{Task.ToString()}"]], buttonText: Loc["DropdownViewButtonText"]);
            #endif
        }

        TaskInProgress = FormTasks.None;
        await _OnTaskFinished.InvokeAsync(Task);
        await OnTaskFinished.InvokeAsync(Task);
    }

    public bool Validate()
    {
        return EditContext.Validate();
    }

    public bool Validate(List<FieldIdentifier> fields)
    {
        var messageStore = shiftValidator?.MessageStore ?? new ValidationMessageStore(EditContext!);
        return EditContext?.Validate(fields, ServiceProvider, Validator, messageStore) ?? true;
    }

    public void DisplayError(string field, string message)
    {
        var fieldId = EditContext?.ToFieldIdentifier(field);
        if (fieldId != null)
        {
            DisplayError(fieldId.Value, message);
        }
    }

    public void DisplayError(FieldIdentifier field, string message)
    {
        var messageStore = shiftValidator?.MessageStore ?? new ValidationMessageStore(EditContext!);
        messageStore?.Add(field, message);
    }

    private HashSet<FormSection> Sections { get; set; } = new HashSet<FormSection>();

    public bool AddSection(FormSection section)
    {
        return Sections.Add(section);
    }

    public List<FormSection> GetSections()
    {
        return Sections.ToList();
    }

    public bool RemoveSection(FormSection section)
    {
        return Sections.Remove(section);
    }

    public void Dispose()
    {
        IShortcutComponent.Remove(Id);
    }
}
