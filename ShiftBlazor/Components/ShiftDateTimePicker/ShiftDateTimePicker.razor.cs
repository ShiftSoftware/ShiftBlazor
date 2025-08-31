using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;
using MudBlazor.Utilities;
using ShiftSoftware.ShiftBlazor.Enums;
using ShiftSoftware.ShiftBlazor.Interfaces;
using ShiftSoftware.ShiftBlazor.Utils;
using System.Linq.Expressions;

namespace ShiftSoftware.ShiftBlazor.Components;

public partial class ShiftDateTimePicker : IDisposable, IShortcutComponent
{
    // TODO
    // Fix adornment padding in Filled and Outlined variants

    [Inject] IJSRuntime JsRuntime { get; set; } = default!;

    [CascadingParameter]
    public EditContext? EditContext { get; set; }

    [CascadingParameter(Name = "ParentDisabled")]
    private bool ParentDisabled { get; set; }

    [CascadingParameter(Name = "ParentReadOnly")]
    private bool ParentReadOnly { get; set; }

    #region Obsolete parameters
    [Obsolete("This parameter is no longer used", false)]
    [Parameter]
    public string? DateLabel { get; set; }

    [Obsolete("This parameter is no longer used", false)]
    [Parameter]
    public string? TimeLabel { get; set; }

    [Obsolete("This parameter is no longer used", false)]
    [Parameter]
    public int DateLg { get; set; }

    [Obsolete("This parameter is no longer used", false)]
    [Parameter]
    public int DateMd { get; set; }

    [Obsolete("This parameter is no longer used", false)]
    [Parameter]
    public int DateSm { get; set; }

    [Obsolete("This parameter is no longer used", false)]
    [Parameter]
    public int DateXs { get; set; }

    [Obsolete("This parameter is no longer used", false)]
    [Parameter]
    public int TimeLg { get; set; }

    [Obsolete("This parameter is no longer used", false)]
    [Parameter]
    public int TimeMd { get; set; }

    [Obsolete("This parameter is no longer used", false)]
    [Parameter]
    public int TimeSm { get; set; }

    [Obsolete("This parameter is no longer used", false)]
    [Parameter]
    public int TimeXs { get; set; }
    #endregion

    [Parameter]
    public DateTimeOffset? DateTimeOffset { get; set; } = System.DateTimeOffset.Now;

    [Parameter]
    public EventCallback<DateTimeOffset?> DateTimeOffsetChanged { get; set; }

    [Parameter]
    public Expression<Func<DateTimeOffset?>>? For { get; set; }

    [Parameter]
    public string? Label { get; set; }

    [Parameter]
    public bool Clearable { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public bool ReadOnly { get; set; }

    [Parameter]
    public Variant Variant { get; set; } = Variant.Text;

    [Parameter]
    public bool Required { get; set; }

    [Parameter]
    public string? ErrorText { get; set; }

    [Parameter]
    public bool Error { get; set; }

    [Parameter]
    public Origin AnchorOrigin { get; set; } = Origin.BottomLeft;

    [Parameter]
    public Origin TransformOrigin { get; set; } = Origin.TopLeft;

    [Parameter]
    public OverflowBehavior OverflowBehavior { get; set; } = OverflowBehavior.FlipOnOpen;

    [Parameter]
    public bool DropdownFixed { get; set; } = false;

    [Parameter]
    public bool LockScroll { get; set; } = false;

    [Parameter]
    public bool Underline { get; set; } = true;

    [Parameter]
    public string? Placeholder { get; set; }

    [Parameter]
    public DateTime? MaxDate { get; set; }

    [Parameter]
    public DateTime? MinDate { get; set; }

    // ======== Adornment Parameters ========

    [Parameter]
    public string? AdornmentIcon { get; set; }

    [Parameter]
    public Adornment Adornment { get; set; } = Adornment.None;

    [Parameter]
    public Color AdornmentColor { get; set; } = Color.Default;

    [Parameter]
    public Size AdornmentSize { get; set; } = Size.Medium;

    [Parameter]
    public string? AdornmentAriaLabel { get; set; }

    [Parameter]
    public EventCallback<MouseEventArgs> OnAdornmentClick { get; set; }

    // ======== Classes and Styles =========
    [Parameter]
    public string? Class { get; set; }

    [Parameter]
    public string? Style { get; set; }

    [Parameter]
    public string? InputClass { get; set; }

    [Parameter]
    public string? InputStyle { get; set; }

    [Parameter]
    public string? DropdownClass { get; set; }

    // ======== Template Parameters =========
    [Parameter]
    public RenderFragment? PickerActionsTemplate { get; set; }

    // ======== Events Parameters ========

    [Parameter]
    public Func<ValueTask<bool>>? OnInputFocus { get; set; }

    [Parameter]
    public Func<ValueTask<bool>>? OnInputBlur { get; set; }

    [Parameter]
    public Func<KeyboardEventArgs, ValueTask<bool>>? OnKeyDown { get; set; }

    [Parameter]
    public Func<KeyboardEventArgs, ValueTask<bool>>? OnKeyUp { get; set; }

    [Parameter]
    public EventCallback OnClearClick { get; set; }

    [Parameter]
    public EventCallback<bool> OnDropdownStateChanged { get; set; }

    protected string Classname =>
            new CssBuilder("shift-datetime-picker shift-input")
                .AddClass("mud-input-required", when: () => Required)
                .AddClass($"mud-input-{Variant.ToDescriptionString()}-with-label", !string.IsNullOrEmpty(Label))
                .AddClass(Class)
                .Build();

    protected string InputContainerClassname =>
            new CssBuilder("shift-input-wrapper")
                .AddClass($"mud-input-{Variant.ToDescriptionString()}-with-label", !string.IsNullOrEmpty(Label))
                .AddClass("mud-shrink", this.DateTimeOffset != null || IsFocused || Adornment == Adornment.Start || !string.IsNullOrWhiteSpace(Placeholder))
                .Build();

    protected string InputClassname =>
            new CssBuilder("shift-datetime-picker-input")
                .AddClass(InputClass)
                .Build();

    protected string DropdownClassname =>
            new CssBuilder("shift-datetime-picker-popover")
                .AddClass(DropdownClass)
                .Build();

    protected string Stylename =>
            new StyleBuilder()
                .AddStyle("z-index", (ZIndex + 1).ToString(), ZIndex != 0)
                .AddStyle(Style)
                .Build();

    private bool IsDisabled => ParentDisabled || Disabled;
    private bool IsReadOnly => ParentReadOnly || ReadOnly;
    private bool DisplayClearable => Clearable && !IsReadOnly && !IsDisabled && this.DateTimeOffset != null;
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string InputId => "Input" + Id.ToString().Replace("-", string.Empty);
    private ElementReference? OverlayChildReference { get; set; }
    private FieldIdentifier _fieldIdentifier;
    public bool IsFocused { get; private set; } = false;
    private bool IsPickerOpen { get; set; }
    private int ZIndex { get; set; } = 0;
    public Dictionary<KeyboardKeys, object> Shortcuts { get; set; } = new();


    protected override void OnInitialized()
    {
        if (For != null)
        {
            Required = FormHelper.IsRequired(For);

            // Display validation errors if For is set and is inside a form
            if (EditContext != null)
            {
                _fieldIdentifier = FieldIdentifier.Create(For);
                EditContext.OnValidationStateChanged += OnValidationStateChanged;
            }
        }
    }

    public async ValueTask HandleShortcut(KeyboardKeys key)
    {
        // inheriting IShortcutComponent will allow us
        // to close the dropdown menu without
        // closing the modal if the input is inside a form
        switch (key)
        {
            case KeyboardKeys.Escape:
                await ClosePicker();
                StateHasChanged();
                break;
        }
    }

    private async Task InputFocusHandler()
    {
        if (IsReadOnly || IsDisabled)
        {
            return;
        }

        IsFocused = true;

        if (OnInputFocus != null && await OnInputFocus.Invoke() == false)
        {
            return;
        }

        if (!IsPickerOpen)
        {
            await OpenPicker();
        }
    }

    private async Task InputBlurHandler()
    {
        IsFocused = false;

        if (OnInputBlur != null && await OnInputBlur.Invoke() == false)
        {
            return;
        }
    }

    public async Task OpenPicker()
    {
        IShortcutComponent.Register(this);
        await OnDropdownStateChanged.InvokeAsync(true);
        IsPickerOpen = true;

        await Task.Delay(10);
        ZIndex = await JsRuntime.InvokeAsync<int>("window.mudpopoverHelper.getEffectiveZIndex", OverlayChildReference);
        StateHasChanged();
    }

    public async Task ClosePicker()
    {
        IShortcutComponent.Remove(Id);
        await OnDropdownStateChanged.InvokeAsync(false);

        IsPickerOpen = false;
        ZIndex = 0;
    }

    public async Task SetDate(DateTime? dateTime)
    {
        if (dateTime != null || dateTime == null && this.DateTimeOffset != null)
        {
            var date = dateTime ?? default(DateTimeOffset);
            var dateTimeOffset = new DateTimeOffset(date.Year, date.Month, date.Day, this.DateTimeOffset?.Hour ?? 0, this.DateTimeOffset?.Minute ?? 0, 0, date.Offset);
            await SetDateTime(dateTimeOffset);
        }
    }

    public async Task SetTime(TimeSpan? timeSpan)
    {
        if (timeSpan != null || timeSpan == null && this.DateTimeOffset != null)
        {
            var date = this.DateTimeOffset ?? System.DateTimeOffset.Now;
            var dateTimeOffset = new DateTimeOffset(date.Year, date.Month, date.Day, timeSpan?.Hours ?? 0, timeSpan?.Minutes ?? 0, 0, date.Offset);
            await SetDateTime(dateTimeOffset);
        }
    }

    public async Task SetDateTimeFromString(string? dateTime)
    {
        System.DateTimeOffset.TryParse(dateTime, out DateTimeOffset _dateTime);
        if (_dateTime != default)
        {
            await SetDateTime(_dateTime);
        }
    }

    public async Task SetDateTime(DateTimeOffset? newDateTime)
    {
        var oldDateTime = this.DateTimeOffset;

        if (!newDateTime.Equals(oldDateTime) && (newDateTime == null || (MaxDate == null || newDateTime < MaxDate) && (MinDate == null || newDateTime > MinDate)))
        {
            this.DateTimeOffset = newDateTime;
            await this.DateTimeOffsetChanged.InvokeAsync(this.DateTimeOffset);

            if (EditContext != null && For != null)
            {
                EditContext.NotifyFieldChanged(FieldIdentifier.Create(For));
            }
        }
    }

    public async Task ClearDateTime()
    {
        await OnClearClick.InvokeAsync();
        await SetDateTime(null);
    }

    private async Task KeyDownHandler(KeyboardEventArgs args)
    {
        if (IsReadOnly || IsDisabled)
        {
            return;
        }

        if (OnKeyDown != null && await OnKeyDown.Invoke(args) == false)
        {
            return;
        }

        switch (args.Key)
        {
            case "Tab":
                await ClosePicker();
                break;
        }
    }

    private async Task KeyUpHandler(KeyboardEventArgs args)
    {
        if (OnKeyUp != null)
        {
            await OnKeyUp.Invoke(args);
        }
    }

    private void OnValidationStateChanged(object? sender, ValidationStateChangedEventArgs e)
    {
        if (EditContext is not null && !_fieldIdentifier.Equals(default(FieldIdentifier)))
        {
            var errorMessages = EditContext.GetValidationMessages(_fieldIdentifier).ToArray();
            Error = errorMessages.Length > 0;
            ErrorText = Error ? errorMessages[0] : null;

            StateHasChanged();
        }
    }

    private async Task FieldClickHandler()
    {
        if (IsPickerOpen || IsReadOnly || IsDisabled)
        {
            return;
        }

        await OpenPicker();
    }

    public void Dispose()
    {
        if (EditContext != null)
        {
            EditContext.OnValidationStateChanged -= OnValidationStateChanged;
        }

        IShortcutComponent.Remove(Id);
    }
}
