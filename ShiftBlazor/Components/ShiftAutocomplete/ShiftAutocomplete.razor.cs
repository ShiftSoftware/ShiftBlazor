using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Microsoft.OData.Client;
using MudBlazor;
using MudBlazor.Utilities;
using ShiftSoftware.ShiftBlazor.Components.ShiftAutocomplete;
using ShiftSoftware.ShiftBlazor.Enums;
using ShiftSoftware.ShiftBlazor.Filters.Models;
using ShiftSoftware.ShiftBlazor.Interfaces;
using ShiftSoftware.ShiftBlazor.Localization;
using ShiftSoftware.ShiftBlazor.Services;
using ShiftSoftware.ShiftBlazor.Utils;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using System.ComponentModel;
using System.Linq.Expressions;

namespace ShiftSoftware.ShiftBlazor.Components;

public partial class ShiftAutocomplete<TEntitySet> : IODataRequestComponent<TEntitySet>, IFilterableComponent, IShortcutComponent, IDisposable where TEntitySet : ShiftEntityDTOBase
{
    [Inject] public SettingManager SettingManager { get; private set; } = default!;
    [Inject] public HttpClient HttpClient { get; private set; } = default!;
    [Inject] public ShiftBlazorLocalizer Loc  { get; private set; } = default!;
    [Inject] private ODataQuery OData { get; set; } = default!;
    [Inject] private ShiftModal ShiftModal { get; set; } = default!;
    [Inject] private MessageService MessageService { get; set; } = default!;
    [Inject] private IScrollManager ScrollManager { get; set; } = null!;
    [Inject] private IJSRuntime JsRuntime { get; set; } = default!;

    [CascadingParameter(Name = "ParentDisabled")]
    private bool ParentDisabled { get; set; }

    [CascadingParameter(Name = "ParentReadOnly")]
    private bool ParentReadOnly { get; set; }
    [CascadingParameter]
    private EditContext? EditContext { get; set; } = default!;

    [Parameter]
    [Description("Currently selected value (single select).")]
    public ShiftEntitySelectDTO? Value { get; set; }

    [Parameter]
    [Description("Raised when the single selected value changes.")]
    public EventCallback<ShiftEntitySelectDTO?> ValueChanged { get; set; }

    [Parameter]
    [Description("Currently selected values (multi-select).")]
    public List<ShiftEntitySelectDTO>? SelectedValues { get; set; } = [];

    [Parameter]
    [Description("Raised when the multi-select collection changes.")]
    public EventCallback<List<ShiftEntitySelectDTO>> SelectedValuesChanged { get; set; }

    [Parameter]
    [Description("OData entity set name to query.")]
    [EditorRequired]
    public string EntitySet { get; set; }

    [Parameter]
    [Description("Overrides the default OData endpoint path.")]
    public string? Endpoint { get; set; }

    [Parameter]
    [Description("Absolute base URL for the API (overrides app settings).")]
    public string? BaseUrl { get; set; }

    [Parameter]
    [Description("Lookup key for resolving the API base URL from settings.")]
    public string? BaseUrlKey { get; set; }

    [Parameter]
    [Description("Input label text.")]
    public string? Label { get; set; }

    [Parameter]
    [Description("MudBlazor input variant (e.g., Text, Filled, Outlined).")]
    public Variant Variant { get; set; } = Variant.Text;

    [Parameter]
    [Description("Marks the field as required and shows required styling.")]
    public bool Required { get; set; }

    [Parameter]
    [Description("Enable multiple selection of items.")]
    public bool MultiSelect { get; set; }

    [Parameter]
    [Description("Maximum number of items to fetch/display per query.")]
    public int MaxItems { get; set; } = 25;

    [Parameter]
    [Description("Maximum dropdown height in pixels.")]
    public int MaxHeight { get; set; } = 300;

    [Parameter]
    [Description("Allow arbitrary user text as a selectable value (free input).")]
    public bool FreeInput { get; set; }

    [Parameter]
    [Description("Show a clear button and allow clearing the selection.")]
    public bool Clearable { get; set; }

    [Parameter]
    [Description("Automatically open the dropdown when the input gains focus.")]
    public bool OpenOnFocus { get; set; } = true;

    [Parameter]
    [Description("Disables user interaction with the input.")]
    public bool Disabled { get; set; }

    [Parameter]
    [Description("Makes the input read-only (no editing, still focusable).")]
    public bool ReadOnly { get; set; }

    [Parameter]
    [Description("EditForm binding for validation; usually like: For=\"@(() => Model.Field)\".")]
    public Expression<Func<ShiftEntitySelectDTO>>? For { get; set; }

    [Parameter]
    [Description("Custom error text to display when the field is invalid.")]
    public string? ErrorText { get; set; }

    [Parameter]
    [Description("Force the field into an error state (visual only).")]
    public bool Error { get; set; }

    [Parameter]
    [Description("Dropdown anchor origin (popper alignment).")]
    public Origin AnchorOrigin { get; set; } = Origin.BottomLeft;

    [Parameter]
    [Description("Dropdown transform origin (popper transform alignment).")]
    public Origin TransformOrigin { get; set; } = Origin.TopLeft;

    [Parameter]
    [Description("Behavior when the dropdown would overflow its container.")]
    public OverflowBehavior OverflowBehavior { get; set; } = OverflowBehavior.FlipOnOpen;

    [Parameter]
    [Description("Fix the dropdown position (useful in modals/overlays).")]
    public bool DropdownFixed { get; set; } = false;

    [Parameter]
    [Description("Lock page scroll while the dropdown is open.")]
    public bool LockScroll { get; set; } = true;

    [Parameter]
    [Description("Show an underline style for the input (Material style).")]
    public bool Underline { get; set; } = true;

    [Parameter]
    [Description("Placeholder text shown when no value is selected.")]
    public string? Placeholder { get; set; }

    [Parameter]
    [Description("Suppress rapid input text updates during typing (improves UX).")]
    public bool TextUpdateSuppression { get; set; } = true;

    [Parameter]
    [Description("Custom content rendered inside the component.")]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    [Description("Group/collapse selected chips into a single expandable group.")]
    public bool GroupSelectedValues { get; set; }

    [Parameter]
    public int MaxSelectedValues { get; set; }

    [Parameter]
    public Func<TEntitySet, bool>? DropdownItemDisabledFunc { get; set; }

    // ======== Classes and Styles =========
    [Parameter]
    [Description("CSS class applied to the root element.")]
    public string? Class { get; set; }

    [Parameter]
    [Description("Inline styles applied to the root element.")]
    public string? Style { get; set; }

    [Parameter]
    [Description("CSS class applied to the input element.")]
    public string? InputClass { get; set; }

    [Parameter]
    [Description("Inline styles applied to the input element.")]
    public string? InputStyle { get; set; }

    [Parameter]
    [Description("CSS class applied to the dropdown container.")]
    public string? DropdownClass { get; set; }

    [Parameter]
    [Description("CSS class applied to the list (menu) element.")]
    public string? ListClass { get; set; }

    [Parameter]
    [Description("CSS class applied to each list item.")]
    public string? ListItemClass { get; set; }


    // ======== Template Parameters =========
    [Parameter]
    [Description("Template for rendering each dropdown item. Context provides the item and selection state.")]
    public RenderFragment<DropdownItemContext<TEntitySet>>? DropdownItemTemplate { get; set; }

    [Parameter]
    [Description("Template for customizing the input area (text field, chips, etc.).")]
    public RenderFragment<AutcompleteInputContext<TEntitySet>>? InputTemplate { get; set; }

    [Parameter]
    [Description("Template for rendering selected values (chips/items).")]
    public RenderFragment<SelectedValueContext<TEntitySet>>? SelectedValuesTemplate { get; set; }

    [Parameter]
    [Description("Template shown when no items are available.")]
    public RenderFragment? NoItemsTemplate { get; set; }

    [Parameter]
    [Description("Template shown while items are loading.")]
    public RenderFragment? LoadingTemplate { get; set; }

    [Parameter]
    [Description("Template rendered after the items list (footer).")]
    public RenderFragment? AfterItemsTemplate { get; set; }

    [Parameter]
    [Description("Template rendered before the items list (header).")]
    public RenderFragment? BeforeItemsTemplate { get; set; }

    [Parameter]
    [Description("Template for the grouped selected-values panel.")]
    public RenderFragment<SelectedValuesGroupContext<TEntitySet>>? SelectedValuesGroupTemplate { get; set; }

    // ======== Adornment Parameters ========

    [Parameter]
    [Description("Icon shown as an input adornment (defaults to up/down arrow depending on the dropdown state).")]
    public string? AdornmentIcon { get; set; }

    [Parameter]
    [Description("Adornment position (Start or End).")]
    public Adornment Adornment { get; set; } = Adornment.End;

    [Parameter]
    [Description("Color of the adornment icon.")]
    public Color AdornmentColor { get; set; } = Color.Default;

    [Parameter]
    [Description("Size of the adornment icon.")]
    public Size AdornmentSize { get; set; } = Size.Medium;

    [Parameter]
    [Description("ARIA label for the adornment button (accessibility).")]
    public string? AdornmentAriaLabel { get; set; }

    [Parameter]
    [Description("Raised when the adornment is clicked. If not handled, clicking toggles the dropdown.")]
    public EventCallback<MouseEventArgs> OnAdornmentClick { get; set; }

    // ======== Quick Add Parameters ========
    [Parameter]
    [Description("Component type to render in the Quick Add modal.")]
    public Type? QuickAddComponentType { get; set; }

    [Parameter]
    [Description("Quick Add parameter name to receive the current text input.")]
    public string? QuickAddParameterName { get; set; }

    [Parameter]
    [Description("Icon shown as an input adornment (defaults to Add/Preview depending on state).")]
    public string? QuickAddIcon { get; set; }

    [Parameter]
    [Description("QuickAdd position (Start or End).")]
    public Adornment QuickAddPosition { get; set; } = Adornment.End;

    [Parameter]
    [Description("Color of the QuickAdd icon.")]
    public Color QuickAddColor { get; set; } = Color.Default;

    [Parameter]
    [Description("Size of the QuickAdd icon.")]
    public Size QuickAddSize { get; set; } = Size.Medium;

    [Parameter]
    [Description("ARIA label for the QuickAdd button (accessibility).")]
    public string? QuickAddAriaLabel { get; set; }

    // ======== Events Parameters ========

    [Parameter]
    [System.ComponentModel.Description("Hook before sending the HTTP request. Return false to cancel.")]
    public Func<HttpRequestMessage, ValueTask<bool>>? OnBeforeRequest { get; set; }

    [Parameter]
    [System.ComponentModel.Description("Hook after receiving the HTTP response. Return false to skip default handling.")]
    public Func<HttpResponseMessage, ValueTask<bool>>? OnResponse { get; set; }

    [Parameter]
    [System.ComponentModel.Description("Hook when an error occurs during fetch. Return true to mark as handled.")]
    public Func<Exception, ValueTask<bool>>? OnError { get; set; }

    [Parameter]
    [System.ComponentModel.Description("Hook after parsing the OData result. Return false to skip default update.")]
    public Func<ODataDTO<TEntitySet>?, ValueTask<bool>>? OnResult { get; set; }

    [Parameter]
    [System.ComponentModel.Description("Raised when the input gains focus. Return false to block default behavior.")]
    public Func<ValueTask<bool>>? OnInputFocus { get; set; }

    [Parameter]
    [System.ComponentModel.Description("Raised when the input loses focus. Return false to block default behavior.")]
    public Func<ValueTask<bool>>? OnInputBlur { get; set; }

    [Parameter]
    [System.ComponentModel.Description("Key down handler for the input. Return false to suppress default handling.")]
    public Func<KeyboardEventArgs, ValueTask<bool>>? OnFieldKeyDown { get; set; }

    [Parameter]
    [System.ComponentModel.Description("Key up handler for the input. Return false to suppress default handling.")]
    public Func<KeyboardEventArgs, ValueTask<bool>>? OnFieldKeyUp { get; set; }

    [Parameter]
    [System.ComponentModel.Description("Raised before opening the Quick Add modal.")]
    public EventCallback<object> OnQuickAddClick { get; set; }

    [Parameter]
    [System.ComponentModel.Description("Raised when the clear button is clicked.")]
    public EventCallback OnClearClick { get; set; }

    [Parameter]
    [System.ComponentModel.Description("Raised when the dropdown open/close state changes.")]
    public EventCallback<bool> OnDropdownStateChanged { get; set; }
    // ======= Filter Properties =======

    [Parameter]
    [Description("Callback to build and inject OData filters for the query.")]
    public Action<ODataFilterGenerator>? Filter { get; set; }
    public bool FilterImmediate { get; set; }
    public RenderFragment? FilterTemplate { get; set; }
    public ODataFilterGenerator ODataFilters { get; private set; } = new ODataFilterGenerator(true);
    public Dictionary<Guid, FilterModelBase> Filters { get; set; } = [];


    // ======== Classnames =========
    protected string Classname =>
            new CssBuilder("shift-autocomplete shift-input")
                .AddClass("mud-input-required", when: () => Required)
                .AddClass($"mud-input-{Variant.ToDescriptionString()}-with-label", !string.IsNullOrEmpty(Label))
                .AddClass(Class)
                .Build();

    protected string InputContainerClassname =>
            new CssBuilder("shift-input-wrapper")
                .AddClass($"mud-input-{Variant.ToDescriptionString()}-with-label", !string.IsNullOrEmpty(Label))
                .AddClass("mud-shrink", !string.IsNullOrWhiteSpace(Text) || IsFocused || SelectedValues?.Count > 0 || Adornment == Adornment.Start || !string.IsNullOrWhiteSpace(Placeholder) || IsIntitialValueLoading)
                .Build();

    protected string InputClassname =>
            new CssBuilder("shift-autocomplete-input")
                .AddClass(InputClass)
                .Build();

    protected string GetListItemClassname(bool isSelected) =>
            new CssBuilder()
                .AddClass("mud-selected-item mud-primary-text mud-primary-hover", isSelected)
                .AddClass(ListItemClass)
                .Build();

    public static readonly string HighlightedClassname = "highlighted-selected-value";

    // ======== Computed Properties =========
    public string InputId => "Input" + Id.ToString().Replace("-", string.Empty);
    public bool IsLoading => FetchTokenSource?.IsCancellationRequested == false;
    public bool IsIntitialValueLoading => InitialUpdateTokenSource?.IsCancellationRequested == false;
    private string OpenIcon => Icons.Material.Filled.ArrowDropUp;
    private string CloseIcon => Icons.Material.Filled.ArrowDropDown;
    private string CurrentIcon => !string.IsNullOrWhiteSpace(AdornmentIcon) ? AdornmentIcon : IsDropdownOpen ? OpenIcon : CloseIcon;
    private bool IsDisabled => ParentDisabled || Disabled;
    private bool IsReadOnly => ParentReadOnly || ReadOnly;
    private bool DisplayClearable => Clearable && !IsReadOnly && !IsDisabled && (!string.IsNullOrWhiteSpace(Text) || Value != null || SelectedValues?.Count > 0);
    private bool IsValueNull => Value == null || Value.Value == string.Empty && Value.Text == null;
    private bool DisplayQuickAdd => QuickAddComponentType != null && (!IsValueNull || IsValueNull && !IsReadOnly && !IsDisabled);
    private bool IsAddValuesDisabled => MaxSelectedValues > 0 && SelectedValues != null && SelectedValues.Count >= MaxSelectedValues;

    // ======== Private Fields =========
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Dictionary<KeyboardKeys, object> Shortcuts { get; set; } = new();

    public bool IsDropdownOpen { get; private set; } = false;
    public bool IsFocused { get; private set; } = false;
    public string Text { get; private set; } = string.Empty;

    public string? UpdateInitialValueError { get; private set; }

    internal bool IsSelectedValuesGroupOpen { get; set; }

    internal string DataValueField = string.Empty;
    internal string DataTextField = string.Empty;

    private int HighlightedListItemIndex { get; set; } = 0;
    internal int SelectedValuesIndex { get; set; } = int.MaxValue;

    public List<TEntitySet> DropdownItems { get; private set; } = [];

    private CancellationTokenSource? FetchTokenSource;
    private CancellationTokenSource? InitialUpdateTokenSource;

    private MudTextField<string>? _InputRef;
    private ElementReference ContainerRef = default!;
    private Debouncer Debouncer = new();
    private FieldIdentifier _fieldIdentifier;

    public override Task SetParametersAsync(ParameterView parameters)
    {
        // Set Text property if Value is changed
        var value = parameters.GetValueOrDefault<ShiftEntitySelectDTO?>(nameof(Value));
        if (!MultiSelect && (value?.Value != Value?.Value || value?.Text != Value?.Text))
        {
            Text = value?.Text ?? string.Empty;
        }

        return base.SetParametersAsync(parameters);
    }

    protected override void OnParametersSet()
    {
        // Try reloading items that only have Value set, but no Text
        if (MultiSelect && SelectedValues?.Any(x => x.Value != null && x.Text == null) == true)
        {
            _ = UpdateInitialValue();
        }
        else if (!MultiSelect && Value?.Value != null &&  Value.Text == null)
        {
            _ = UpdateInitialValue();
        }

        if (For != null && EditContext != null)
        {
            _fieldIdentifier = FieldIdentifier.Create(For);
        }

        base.OnParametersSet();
    }

    protected override void OnInitialized()
    {
        if (string.IsNullOrWhiteSpace(EntitySet))
        {
            throw new ArgumentException("EntitySet parameter is required.", nameof(EntitySet));
        }

        var shiftEntityKeyAndNameAttribute = Misc.GetAttribute<TEntitySet, ShiftEntityKeyAndNameAttribute>();

        if (shiftEntityKeyAndNameAttribute == null)
        {
            throw new Exception($"The type {typeof(TEntitySet).Name} must be decorated with the [{nameof(ShiftEntityKeyAndNameAttribute)}] attribute.");
        }

        DataValueField = shiftEntityKeyAndNameAttribute.Value;
        DataTextField = shiftEntityKeyAndNameAttribute.Text;

        // Display validation errors if For is set and is inside a form
        if (For != null && EditContext != null)
        {
            _fieldIdentifier = FieldIdentifier.Create(For);
            EditContext.OnValidationStateChanged += OnValidationStateChanged;
        }

        // display the initial value if it exists
        if (!string.IsNullOrWhiteSpace(Value?.Text) && !MultiSelect)
        {
            Text = Value.Text;
        }

        base.OnInitialized();
    }

    public async ValueTask HandleShortcut(KeyboardKeys key)
    {
        // inheriting IShortcutComponent will allow us
        // to close the dropdown menu without
        // closing the modal if the input is inside a form
        switch (key)
        {
            case KeyboardKeys.Escape:
                await CloseDropdown();
                if (GroupSelectedValues)
                {
                    CloseSelectedValuesGroup();
                }
                StateHasChanged();
                break;
        }
    }

    private void DebouncedFetchItems(string? searchQuery = null)
    {
        Debouncer.Debounce(200, async () => await FetchItems(searchQuery));
    }

    private async Task FetchItems(string? searchQuery = null)
    {
        HighlightedListItemIndex = 0; // Reset the selected dropdown item
        
        FetchTokenSource?.Cancel();
        FetchTokenSource?.Dispose();
        FetchTokenSource = new CancellationTokenSource();
        var cts = FetchTokenSource;
        StateHasChanged();

        try
        {
            var url = BuildODataUrl(searchQuery);
            //if (string.IsNullOrWhiteSpace(url))
                //throw new Exception(Loc["DataReadUrlError"]);
            var uri = new Uri(url!);

            var items = await IODataRequestComponent<TEntitySet>.GetFromJsonAsync(this, uri, cts.Token);

            if (items == null)
                return;

            DropdownItems = items.Value;
            // the cancelation token is also used to indicate loading state
            FetchTokenSource?.Cancel();
            StateHasChanged();
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception e)
        {
            if (OnError != null && !(await OnError.Invoke(e)))
                return;

            Console.WriteLine($"fetch error {e}");
            StateHasChanged();

        }
    }

    private string? BuildODataUrl(string? query)
    {
        var url = IRequestComponent.GetPath(this);
        var builder = OData
                .CreateNewQuery<TEntitySet>(EntitySet, url);

        // Filters components
        var filters = Filters.Select(x => x.Value.ToODataFilter().ToString()).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var filter = new ODataFilterGenerator().Add(DataTextField, ODataOperator.Contains, query);
            filters.Add(filter.ToString());
        }

        // Filter Parameter
        if (Filter != null)
        {
            var filter = new ODataFilterGenerator(true, Id);
            Filter.Invoke(filter);
            ODataFilters.Add(filter);
        }

        if (ODataFilters.Count > 0)
        {
            filters.Add(ODataFilters.ToString());
        }

        if (filters.Count > 0)
        {
            var filterQueryString = $"({string.Join(") and (", filters)})";
            builder = builder.AddQueryOption("$filter", filterQueryString);
        }

        return builder.Take(MaxItems).ToString();
    }

    public async Task OpenDropdown(bool selectText = true, bool fetchItems = true)
    {
        if (IsDropdownOpen)
        {
            return;
        }

        IShortcutComponent.Register(this);
        await OnDropdownStateChanged.InvokeAsync(true);

        if (selectText && _InputRef != null)
        {
            await _InputRef.SelectAsync();
        }

        if (fetchItems)
        {
            await FetchItems();
        }

        // dont open the dropdown if list items are being fetched
        if (!IsLoading)
        {
            IsDropdownOpen = true;
        }
    }

    public async Task CloseDropdown(bool clearText = true)
    {
        IShortcutComponent.Remove(Id);

        if (!IsDropdownOpen)
        {
            return;
        }

        await OnDropdownStateChanged.InvokeAsync(false);

        IsDropdownOpen = false;
        if (MultiSelect && clearText)
        {
            Text = string.Empty;
        }

        FetchTokenSource?.Cancel();
    }

    internal async Task InputFocusHandler()
    {
        if (IsReadOnly || IsDisabled)
        {
            return;
        }

        IsFocused = true;
        if (GroupSelectedValues)
        {
            CloseSelectedValuesGroup();
        }

        if (OnInputFocus != null && await OnInputFocus.Invoke() == false)
        {
            return;
        }

        await ResetSelectedValuesIndex();
        if (OpenOnFocus)
        {
            await OpenDropdown();
        }
    }

    internal async Task InputBlurHandler()
    {
        IsFocused = false;

        if (OnInputBlur != null && await OnInputBlur.Invoke() == false)
        {
            return;
        }

        if (!MultiSelect)
        {
            // If the input is empty, set the value to null
            if (string.IsNullOrWhiteSpace(Text) && Value != null)
            {
                await SetValue(null);
            }
            else if (FreeInput && !string.IsNullOrWhiteSpace(Text) && Value?.Text != Text)
            {
                await AddFreeInputValue();
            }
        }


    }

    public async Task SelectItem(TEntitySet item)
    {
        // convert TEntitySet to ShiftEntitySelectDTO
        var _item = ToSelectDTO(item);
        if (_item == null)
        {
            MessageService.Error(Loc["CouldNotSelectItemError"]);
            return;
        }
        await SelectItem(_item);
    }

    public async Task SelectItem(ShiftEntitySelectDTO item)
    {
        SelectedValuesIndex = int.MaxValue;
        if (MultiSelect)
        {
            // If the item is already selected, remove it from the selected values
            // If the item is not selected, add it to the selected values
            if (!await RemoveSelected(item))
            {
                if (!IsAddValuesDisabled)
                {
                    SelectedValues ??= [];
                    SelectedValues.Add(item);
                    await SelectedValuesChanged.InvokeAsync(SelectedValues);
                }
            }
        }
        else
        {
            await SetValue(item);
            await CloseDropdown();
        }

        if (EditContext != null && For != null)
        {
            EditContext.NotifyFieldChanged(FieldIdentifier.Create(For));
        }
    }

    public async Task<bool> RemoveSelected(ShiftEntitySelectDTO item)
    {
        if (SelectedValues == null || SelectedValues.Count == 0)
        {
            return false;
        }

        // If the item has no value, compare by text only
        // For when the item is a free input value
        var match = SelectedValues.FirstOrDefault(x =>
        {
            return item.Value == null
                ? x.Text == item.Text
                : x.Value == item.Value;
        });

        var removed = match != null && SelectedValues.Remove(match);

        if (removed)
        {
            await SelectedValuesChanged.InvokeAsync(SelectedValues);
        }

        if (SelectedValuesIndex <= SelectedValues.Count && !IsFocused)
        {
            await Task.Delay(10);
            var index = int.Min(SelectedValuesIndex, SelectedValues.Count - 1);
            if (SelectedValuesIndex == index)
            {
                await FocusSelectedValue(index);
            }
            else
            {
                await ChangeSelectedValuesIndex(index);
            }
        }

        return removed;
    }

    private string GetProperty<T>(T entity, string propertyName)
    {
        return typeof(T)
            .GetProperty(propertyName)?
            .GetValue(entity)?
            .ToString() ?? string.Empty;
    }

    public async Task AddEditItem(object? key = null)
    {
        if (QuickAddComponentType == null)
        {
            return;
        }

        await OnQuickAddClick.InvokeAsync();

        Dictionary<string, object>? parameters = null;

        if (QuickAddParameterName != null)
        {
            parameters = new()
            {
                {QuickAddParameterName, Text}
            };
        }

        var result = await ShiftModal.Open(QuickAddComponentType, key, ModalOpenMode.Popup, parameters);

        if (result?.Data == null || result.Canceled == true)
        {
            return;
        }

        var quickAddType = result.Data.GetType();
        var keyAndName = Misc.GetAttribute<ShiftEntityKeyAndNameAttribute>(quickAddType);

        // use the same field names as TEntitySet if QuickAddType doesn't have the attribute
        var dataValueField = keyAndName?.Value ?? DataValueField;
        var dataTextField = keyAndName?.Text ?? DataTextField;

        var value = quickAddType.GetProperty(dataValueField)?.GetValue(result.Data)?.ToString();
        var text = quickAddType.GetProperty(dataTextField)?.GetValue(result.Data)?.ToString();

        if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var localizedText = LocalizedTextJsonConverter.ParseLocalizedText(text);

        await SelectItem(new ShiftEntitySelectDTO
        {
            Value = value,
            Text = localizedText,
            Data = result.Data
        });
    }

    public ShiftEntitySelectDTO? ToSelectDTO(TEntitySet entity)
    {
        var id = GetProperty(entity, DataValueField);
        var text = GetProperty(entity, DataTextField);

        return new ShiftEntitySelectDTO
        {
            Value = id,
            Text = text,
            Data = entity
        };
    }

    internal async Task TextChangedHandler(string? text)
    {
        if (text == Text || IsReadOnly || IsDisabled)
        {
            return;
        }

        Text = text ?? string.Empty;

        if (IsFocused)
        {
            if (!IsDropdownOpen)
            {
                await OpenDropdown(false, false);
            }

            DebouncedFetchItems(text);
        }
    }

    private async Task AdornmentClickHandler()
    {
        if (OnAdornmentClick.HasDelegate)
        {
            await OnAdornmentClick.InvokeAsync();
        }
        else if (!IsReadOnly && !IsDisabled)
        {
            await OpenDropdown(selectText: false);
        }
    }

    private async Task FieldKeyDownHandler(KeyboardEventArgs args)
    {
        if (IsReadOnly || IsDisabled)
        {
            return;
        }

        if (OnFieldKeyDown != null && await OnFieldKeyDown.Invoke(args) == false)
        {
            return;
        }

        switch (args.Key)
        {
            case "Backspace":
                if (IsFocused)
                {
                    await MoveSelectedValuesIndex(-1);
                }
                break;
            case "ArrowLeft":
                await MoveSelectedValuesIndex(-1);
                break;
            case "ArrowRight":
                await MoveSelectedValuesIndex(+1);
                break;
            case "ArrowDown":
                if (IsFocused)
                {
                    if (IsDropdownOpen)
                    {
                        SelectDropdownItem(HighlightedListItemIndex + 1);
                    }
                    // don't attempt to open the dropdown if items are being fetched
                    // otherwise this will just canceled the current fetch request
                    else if (!IsLoading)
                    {
                        await OpenDropdown();
                    }
                }
                break;
            case "ArrowUp":
                if (IsFocused)
                {
                    if (IsDropdownOpen)
                    {
                        SelectDropdownItem(HighlightedListItemIndex - 1);
                    }
                    else if (!IsLoading)
                    {
                        await OpenDropdown();
                    }
                }
                break;
            case "Tab":
                if (IsFocused)
                {
                    await CloseDropdown();
                }
                if (GroupSelectedValues)
                {
                    CloseSelectedValuesGroup();
                }
                break;
        }
    }

    private async Task FieldKeyUpHandler(KeyboardEventArgs args)
    {
        if (!IsFocused || IsReadOnly || IsDisabled)
        {
            return;
        }

        if (OnFieldKeyUp != null && await OnFieldKeyUp.Invoke(args) == false)
        {
            return;
        }

        switch (args.Key)
        {
            case "Enter":
            case "NumpadEnter":
                // When trying fast and adding items,
                // the search might not be finished yet,
                // so we only select an item if search is finished (IsLoading == false).
                if (IsDropdownOpen && !IsLoading)
                {
                    if (FreeInput && MultiSelect && HighlightedListItemIndex >= DropdownItems.Count)
                    {
                        await AddFreeInputValue();
                    }
                    else if (DropdownItems.Count == 0)
                    {
                        await CloseDropdown();
                    }
                    else if (HighlightedListItemIndex >= 0 && HighlightedListItemIndex < DropdownItems.Count)
                    {
                        await SelectItem(DropdownItems.ElementAt(HighlightedListItemIndex));
                    }
                }
                else if (!IsDropdownOpen)
                {
                    await OpenDropdown();
                }

                break;
        }

    }

    private async Task ClearSelected()
    {
        await OnClearClick.InvokeAsync();

        if (MultiSelect)
        {
            SelectedValues?.Clear();
            await SelectedValuesChanged.InvokeAsync(SelectedValues);
        }
        else
        {
            await SetValue(null);
        }
    }

    private async Task AddFreeInputValue(string? text = null)
    {
        text ??= Text;

        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        if (IsDropdownOpen && DropdownItems.Count == 0)
        {
            await CloseDropdown();
        }

        var value = new ShiftEntitySelectDTO { Value = string.Empty, Text = text };
        await SelectItem(value);

        if (MultiSelect)
        {
            Text = string.Empty;
            SelectDropdownItem(0);
        }
    }

    private async Task SetValue(ShiftEntitySelectDTO? value)
    {
        Value = value;
        Text = value?.Text ?? value?.ToString() ?? string.Empty;
        await ValueChanged.InvokeAsync(value);
    }

    private async Task UpdateInitialValue(bool force = false)
    {
        if (UpdateInitialValueError != null && !force)
        {
            return;
        }

        var values = MultiSelect
            ? SelectedValues ?? []
            : Value != null ? [Value] : [];

        var itemsToLoad = values.Where(x => !string.IsNullOrWhiteSpace(x.Value) && x.Text == null);

        if (!itemsToLoad.Any())
        {
            return;
        }

        var ids = itemsToLoad.Select(x => x.Value).Distinct();

        InitialUpdateTokenSource?.Cancel();
        InitialUpdateTokenSource?.Dispose();
        InitialUpdateTokenSource = new CancellationTokenSource();
        var cts = InitialUpdateTokenSource;
        StateHasChanged();

        try
        {
            var _url = IRequestComponent.GetPath(this);
            var builder = OData
                .CreateNewQuery<TEntitySet>(EntitySet, _url);

            var url = builder.WhereQuery(x => ids.Contains(x.ID))
                    .ToString();

            var items = await IODataRequestComponent<TEntitySet>.GetFromJsonAsync(this, new Uri(url), cts.Token);

            if (items == null || items.Value.Count == 0)
            {
                UpdateInitialValueError = "Failed to load initial data.";
                InitialUpdateTokenSource?.Cancel();
                StateHasChanged();
                return;
            }

            ShiftEntitySelectDTO? itemFound = null;
            foreach (var item in items.Value)
            {
                var text = GetProperty(item, DataTextField);
                var value = GetProperty(item, DataValueField);

                itemFound = itemsToLoad.FirstOrDefault(x => x.Value == value);

                if (itemFound != null)
                {
                    itemFound.Text = text;
                    itemFound.Data = item;
                }

            }

        }
        catch (TaskCanceledException)
        {
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception e)
        {
            MessageService.Error(Loc["Failed to load initial data"], Loc["Failed to retrieve data"], e.Message);
        }

        if (!MultiSelect)
        {
            Text = Value?.Text ?? string.Empty;
        }

        cts.Cancel();
        StateHasChanged();
    }

    public void SelectDropdownItem(int index)
    {
        var maxIndex = FreeInput && MultiSelect && !string.IsNullOrWhiteSpace(Text)
            ? DropdownItems.Count +1
            : DropdownItems.Count;

        if (DropdownItems.Count > 0 && index >= 0 && index < maxIndex)
        {
            HighlightedListItemIndex = index;
            ScrollManager.ScrollToListItemAsync(GetItemId(index));
        }
    }

    public string GetItemId(int index)
    {
        return $"{Id}-item-{index}";
    }

    public async Task ChangeSelectedValuesIndex(int index)
    {
        if (SelectedValuesIndex == index)
        {
            return;
        }

        if (index >= 0 && index < SelectedValues?.Count)
        {
            SelectedValuesIndex = index;
        }
        else
        {
            SelectedValuesIndex = int.MaxValue;
        }

        await FocusSelectedValue(SelectedValuesIndex);
    }

    private async Task FocusSelectedValue(int index)
    {
        if (index >= 0 && index < SelectedValues?.Count)
        {
            var focusableElementsCount = await JsRuntime.InvokeAsync<int>("GetFocusableElementCount", ContainerRef);
            if (focusableElementsCount <= 1)
            {
                return;
            }

            await CloseDropdown();
            await ContainerRef.MudFocusFirstAsync(index);
        }
        else if (_InputRef != null)
        {
            await _InputRef.FocusAsync();
        }
    }

    public async Task MoveSelectedValuesIndex(int direction)
    {
        if (!MultiSelect || SelectedValues == null || SelectedValues.Count == 0)
        {
            return;
        }

        var position = await JsRuntime.InvokeAsync<int[]>("getCursorPosition", InputId);

        // If the cursor is not at the start of the input field,
        // do not change the selected values index.
        // We check for both the start and end of selection
        if (position[0] != 0 || position[1] != 0)
        {
            return;
        }

        var index = SelectedValuesIndex;

        var chipCounter = GroupSelectedValues
            ? SelectedValues.Count > 0 ? 1 : 0
            : SelectedValues.Count;

        if (direction < 0 && SelectedValuesIndex > 0)
        {
            index = int.Min(SelectedValuesIndex, chipCounter) - 1;
        }
        else if (direction > 0 && SelectedValuesIndex < chipCounter)
        {
            index++;
        }

        await ChangeSelectedValuesIndex(index);
    }

    public async Task ResetSelectedValuesIndex()
    {
        await ChangeSelectedValuesIndex(int.MaxValue);
    }

    private string GetAdornmentIcon()
    {
        if (string.IsNullOrWhiteSpace(QuickAddIcon))
        {
            return Value == null || MultiSelect
                ? Icons.Material.Filled.AddCircle
                : Icons.Material.Filled.RemoveRedEye;
        }

        return QuickAddIcon;
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

    internal void OpenSelectedValuesGroup()
    {
        if (!IsSelectedValuesGroupOpen)
        {
            IsSelectedValuesGroupOpen = true;
        }
    }

    internal void CloseSelectedValuesGroup()
    {
        if (IsSelectedValuesGroupOpen)
        {
            IsSelectedValuesGroupOpen = false;
        }
    }

    // we don't wanna do anything on click as the Menu component will handle click
    // We only add this handler to make the component focusable
    private void SelectedValuesGroupClickHandler() { }

    public void AddFilter(Guid id, string field, ODataOperator op, object? value)
    {
        throw new NotImplementedException();
    }
    public void Dispose()
    {
        if (EditContext != null)
        {
            EditContext.OnValidationStateChanged -= OnValidationStateChanged;
        }
        FetchTokenSource?.Dispose();
        InitialUpdateTokenSource?.Dispose();
        IShortcutComponent.Remove(Id);
    }

    public object? GetParamValue(string paramName)
    {
        var prop = GetType().GetProperty(paramName);
        return prop?.GetValue(this);
    }

}
