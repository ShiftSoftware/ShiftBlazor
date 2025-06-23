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
using ShiftSoftware.ShiftBlazor.Services;
using ShiftSoftware.ShiftBlazor.Utils;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ShiftSoftware.ShiftBlazor.Components;

public partial class ShiftAutocomplete<TEntitySet> : IFilterableComponent, IDisposable where TEntitySet : ShiftEntityDTOBase
{
    [Inject] private SettingManager SettingManager { get; set; } = default!;
    [Inject] private HttpClient Http { get; set; } = default!;
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
    public ShiftEntitySelectDTO? Value { get; set; }

    [Parameter]
    public EventCallback<ShiftEntitySelectDTO?> ValueChanged { get; set; }

    [Parameter]
    public List<ShiftEntitySelectDTO> SelectedValues { get; set; } = [];

    [Parameter]
    public EventCallback<List<ShiftEntitySelectDTO>> SelectedValuesChanged { get; set; }

    [Parameter]
    [EditorRequired]
    public string EntitySet { get; set; }

    [Parameter]
    public string? BaseUrl { get; set; }

    [Parameter]
    public string? BaseUrlKey { get; set; }

    [Parameter]
    public string? DataValueField { get; set; }

    [Parameter]
    public string? DataTextField { get; set; }

    [Parameter]
    public string? Label { get; set; }

    [Parameter]
    public Variant Variant { get; set; } = Variant.Text;

    [Parameter]
    public bool Required { get; set; }

    [Parameter]
    public bool MultiSelect { get; set; }

    [Parameter]
    public bool SimplifyResponseData { get; set; }

    [Parameter]
    public int MaxItems { get; set; } = 25;

    [Parameter]
    public int MaxHeight { get; set; } = 300;

    [Parameter]
    public bool FreeInput { get; set; }

    [Parameter]
    public bool Clearable { get; set; }

    [Parameter]
    public bool OpenOnFocus { get; set; } = true;

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public bool ReadOnly { get; set; }

    [Parameter]
    public Expression<Func<ShiftEntitySelectDTO>>? For { get; set; }

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
    public bool LockScroll { get; set; } = true;

    [Parameter]
    public bool Underline { get; set; } = true;

    [Parameter]
    public string? Placeholder { get; set; }

    [Parameter]
    public bool TextUpdateSuppression { get; set; } = true;

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

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

    [Parameter]
    public string? ListClass { get; set; }

    [Parameter]
    public string? ListItemClass { get; set; }


    // ======== Template Parameters =========
    [Parameter]
    public RenderFragment<DropdownItemContext<TEntitySet>>? DropdownItemTemplate { get; set; }

    [Parameter]
    public RenderFragment<AutcompleteInputContext<TEntitySet>>? InputTemplate { get; set; }

    [Parameter]
    public RenderFragment<SelectedValueContext<TEntitySet>>? SelectedValuesTemplate { get; set; }

    [Parameter]
    public RenderFragment? NoItemsTemplate { get; set; }

    [Parameter]
    public RenderFragment? LoadingTemplate { get; set; }

    [Parameter]
    public RenderFragment? AfterItemsTemplate { get; set; }

    [Parameter]
    public RenderFragment? BeforeItemsTemplate { get; set; }

    // ======== Adornment Parameters ========

    [Parameter]
    public string? AdornmentIcon { get; set; }

    [Parameter]
    public Adornment Adornment { get; set; } = Adornment.End;

    [Parameter]
    public Color AdornmentColor { get; set; } = Color.Default;

    [Parameter]
    public Size AdornmentSize { get; set; } = Size.Medium;

    [Parameter]
    public string? AdornmentAriaLabel { get; set; }

    [Parameter]
    public EventCallback<MouseEventArgs> OnAdornmentClick { get; set; }

    // ======== Quick Add Parameters ========
    [Parameter]
    public Type? QuickAddComponentType { get; set; }

    [Parameter]
    public string? QuickAddParameterName { get; set; }

    [Parameter]
    public string? QuickAddDataValueField { get; set; }

    [Parameter]
    public string? QuickAddDataTextField { get; set; }

    // ======== Events Parameters ========
    [Parameter]
    public EventCallback<List<TEntitySet>> OnEntityResponse { get; set; }
    
    [Parameter]
    public EventCallback<HttpResponseMessage> OnResponse { get; set; }

    [Parameter]
    public Func<ValueTask<bool>>? OnInputFocus { get; set; }

    [Parameter]
    public Func<ValueTask<bool>>? OnInputBlur { get; set; }

    [Parameter]
    public Func<KeyboardEventArgs, ValueTask<bool>>? OnFieldKeyDown { get; set; }

    [Parameter]
    public Func<KeyboardEventArgs, ValueTask<bool>>? OnFieldKeyUp { get; set; }

    [Parameter]
    public Func<KeyboardEventArgs, ValueTask<bool>>? OnInputKeyDown { get; set; }

    [Parameter]
    public Func<KeyboardEventArgs, ValueTask<bool>>? OnInputKeyUp { get; set; }

    [Parameter]
    public EventCallback<object> OnQuickAddClick { get; set; }

    [Parameter]
    public EventCallback OnClearClick { get; set; }

    [Parameter]
    public EventCallback<bool> OnDropdownStateChanged { get; set; }

    // ======= Filter Properties =======

    [Parameter]
    public Action<ODataFilterGenerator>? Filter { get; set; }
    public bool FilterImmediate { get; set; }
    public RenderFragment? FilterTemplate { get; set; }
    public ODataFilterGenerator ODataFilters { get; private set; } = new ODataFilterGenerator(true);
    public Dictionary<Guid, FilterModelBase> Filters { get; set; } = [];


    // ======== Classnames =========
    protected string Classname =>
            new CssBuilder("shift-autocomplete")
                .AddClass("mud-input-required", when: () => Required)
                .AddClass($"mud-input-{Variant.ToDescriptionString()}-with-label", !string.IsNullOrEmpty(Label))
                .AddClass(Class)
                .Build();

    protected string InputContainerClassname =>
            new CssBuilder("shift-autocomplete-input-container")
                .AddClass($"mud-input-{Variant.ToDescriptionString()}-with-label", !string.IsNullOrEmpty(Label))
                .AddClass("mud-shrink", !string.IsNullOrWhiteSpace(Text) || IsFocused || SelectedValues.Count > 0 || Adornment == Adornment.Start || !string.IsNullOrWhiteSpace(Placeholder))
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
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string InputId => "Input" + Id.ToString().Replace("-", string.Empty);

    public bool IsDropdownOpen { get; private set; } = false;
    public bool IsFocused { get; private set; } = false;
    public bool IsLoading { get; private set; } = false;
    public bool IsIntitialValueLoading { get; private set; } = false;
    public string Text { get; private set; } = string.Empty;

    internal string _DataValueField = string.Empty;
    internal string _DataTextField = string.Empty;

    private int HighlightedListItemIndex { get; set; } = 0;
    internal int SelectedValuesIndex { get; set; } = int.MaxValue;

    private string OpenIcon => Icons.Material.Filled.ArrowDropUp;
    private string CloseIcon => Icons.Material.Filled.ArrowDropDown;

    private string CurrentIcon => !string.IsNullOrWhiteSpace(AdornmentIcon) ? AdornmentIcon : IsDropdownOpen ? OpenIcon : CloseIcon;

    private List<TEntitySet> DropdownItems { get; set; } = [];

    private CancellationTokenSource FetchToken = new CancellationTokenSource();

    private MudTextField<string>? _InputRef;
    private ElementReference ContainerRef = default!;
    private Debouncer Debouncer = new();
    private FieldIdentifier _fieldIdentifier;

    private bool IsDisabled => ParentDisabled || Disabled;
    private bool IsReadOnly => ParentReadOnly || ReadOnly;
    private bool DisplayClearable => Clearable && !IsReadOnly && !IsDisabled && ( !string.IsNullOrWhiteSpace(Text) || Value != null || SelectedValues.Count != 0);
    private bool DisplayQuickAdd => QuickAddComponentType != null && (Value != null || Value == null && !IsReadOnly && !IsDisabled);

    public override Task SetParametersAsync(ParameterView parameters)
    {
        // Set Text property if Value is changed
        var value = parameters.GetValueOrDefault<ShiftEntitySelectDTO?>(nameof(Value));
        if ((value?.Value != Value?.Value || value?.Text != Value?.Text) && !MultiSelect)
        {
            Text = value?.Text ?? string.Empty;
        }

        return base.SetParametersAsync(parameters);
    }

    protected override void OnInitialized()
    {
        if (string.IsNullOrWhiteSpace(EntitySet))
        {
            throw new ArgumentException("EntitySet parameter is required.", nameof(EntitySet));
        }

        // If no DataValueField or DataTextField is provided, use the ShiftEntityKeyAndNameAttribute if available
        // these two variables are the names of the properties used for the value and text of the items
        var shiftEntityKeyAndNameAttribute = Misc.GetAttribute<TEntitySet, ShiftEntityKeyAndNameAttribute>();
        _DataValueField = DataValueField ?? shiftEntityKeyAndNameAttribute?.Value ?? "";
        _DataTextField = DataTextField ?? shiftEntityKeyAndNameAttribute?.Text ?? "";

        if (string.IsNullOrWhiteSpace(_DataValueField))
            throw new ArgumentNullException(nameof(DataValueField));

        if (string.IsNullOrWhiteSpace(_DataTextField))
            throw new ArgumentNullException(nameof(DataTextField));

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

        // Try reloading items that only have Value set, but no Text
        _ = UpdateInitialValue();

        base.OnInitialized();
    }

    private void DebouncedFetchItems(string? searchQuery = null)
    {
        Debouncer.Debounce(200, async () => await FetchItems(searchQuery));
    }

    private async Task FetchItems(string? searchQuery = null)
    {
        HighlightedListItemIndex = 0; // Reset the selected dropdown item
        IsLoading = true;

        var builder = OData
                .CreateNewQuery<TEntitySet>(EntitySet, GetPath());

        if (SimplifyResponseData)
        {
            builder = builder.AddQueryOption("$select", $"{_DataValueField},{_DataTextField}");
        }

        var filters = Filters.Select(x => x.Value.ToODataFilter().ToString()).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        
        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            var filter = new ODataFilterGenerator().Add(_DataTextField, ODataOperator.Contains, searchQuery);
            filters.Add(filter.ToString());
        }

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

        var url = builder.Take(MaxItems).ToString();
        
        FetchToken.Cancel();
        FetchToken.Dispose();
        FetchToken = new CancellationTokenSource();
        StateHasChanged();

        try
        {
            using var res = await Http.GetAsync(url, FetchToken.Token);
            var items = await ParseEntityResponse(res);
            DropdownItems = items ?? [];
        }
        catch (TaskCanceledException)
        {
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception e)
        {
            Console.WriteLine("fetch error");
        }

        IsLoading = false;
        StateHasChanged();

    }

    // copy from EnityForm
    internal async Task<List<TEntitySet>> ParseEntityResponse(HttpResponseMessage res)
    {
        await OnResponse.InvokeAsync(res);

        if (res.StatusCode == HttpStatusCode.NoContent)
        {
            return [];
        }

        ODataDTO<TEntitySet>? result = null;

        try
        {
            result = await res.Content.ReadFromJsonAsync<ODataDTO<TEntitySet>>(new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                Converters = { new LocalDateTimeOffsetJsonConverter() }
            });
        }
        catch (Exception ex)
        {
            var resBody = await res.Content.ReadAsStringAsync();
            throw new Exception($"{(int)res.StatusCode} {res.StatusCode}", new Exception(resBody, ex));
        }

        if (result == null)
        {
            throw new Exception("Could not get response from server");
        }

        //if (result.Message != null)
        //{
        //    var parameters = new DialogParameters {
        //            { "Message", result.Message },
        //            { "Color", Color.Error },
        //            { "Icon", Icons.Material.Filled.Error },
        //        };

        //    await DialogService.ShowAsync<PopupMessage>("", parameters, new DialogOptions
        //    {
        //        MaxWidth = MaxWidth.ExtraSmall,
        //        NoHeader = true,
        //        CloseOnEscapeKey = false,
        //    });

        //    if (!res.IsSuccessStatusCode)
        //    {
        //        return null;
        //    }
        //}

        if (res.IsSuccessStatusCode)
        {
            var value = result.Value;
            await OnEntityResponse.InvokeAsync(value);
            return value;
        }

        throw new Exception($"{(int)res.StatusCode} {res.StatusCode}", new Exception(await res.Content.ReadAsStringAsync()));
    }

    // copy from EnityForm
    private string GetPath()
    {
        string? url = BaseUrl;

        if (url is null && BaseUrlKey is not null)
            url = SettingManager.Configuration.ExternalAddresses.TryGet(BaseUrlKey);

        if (url is null)
            return SettingManager.Configuration.BaseAddress;

        return url;
    }

    public async Task OpenDropdown(bool selectText = true)
    {
        if (selectText && _InputRef != null)
        {
            await _InputRef.SelectAsync();
        }
        await FetchItems();

        // If the dropdown is already open, do not call the event
        if (!IsDropdownOpen)
        {
            await OnDropdownStateChanged.InvokeAsync(true);
        }

        IsDropdownOpen = true;
    }

    public async Task CloseDropdown(bool clearText = true)
    {
        // If the dropdown is already closed, do not call the event
        if (IsDropdownOpen)
        {
            await OnDropdownStateChanged.InvokeAsync(false);
        }

        IsDropdownOpen = false;
        if (MultiSelect && clearText)
        {
            Text = string.Empty;
        }

        FetchToken.Cancel();
    }

    internal async Task InputFocusHandler()
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
            MessageService.Error("Could not select item");
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
                SelectedValues.Add(item);
                await SelectedValuesChanged.InvokeAsync(SelectedValues);
            }
        }
        else
        {
            await SetValue(item);
            await CloseDropdown();
        }
    }

    public async Task<bool> RemoveSelected(ShiftEntitySelectDTO item)
    {
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
        var dataValueField = QuickAddDataValueField ?? keyAndName?.Value ?? _DataValueField;
        var dataTextField = QuickAddDataTextField ?? keyAndName?.Text ?? _DataTextField;

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
            Data = result.Data,
        });
    }

    public ShiftEntitySelectDTO? ToSelectDTO(TEntitySet entity)
    {
        var id = GetProperty(entity, _DataValueField);
        var text = GetProperty(entity, _DataTextField);

        return new ShiftEntitySelectDTO
        {
            Value = id,
            Text = text,
            Data = entity,
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
                await OpenDropdown(false);
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
            await OpenDropdown();
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


            case "Tab":
                if (IsFocused)
                {
                    await CloseDropdown();
                }
                break;
            case "ArrowDown":
                if (IsFocused)
                {
                    if (IsDropdownOpen)
                    {
                        SelectDropdownItem(HighlightedListItemIndex + 1);
                    }
                    else
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
                    else
                    {
                        await OpenDropdown();
                    }
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

        if (OnInputKeyUp != null && await OnInputKeyUp.Invoke(args) == false)
        {
            return;
        }

        switch (args.Key)
        {
            case "Enter":
            case "NumpadEnter":
                // When trying fast and adding items,
                // the search might not be finished yet,
                // so we only select an item if search is finished.
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
            case "Escape":
            await CloseDropdown();
                break;
        }

    }

    private async Task ClearSelected()
    {
        if (MultiSelect)
        {
            SelectedValues.Clear();
            await SelectedValuesChanged.InvokeAsync(SelectedValues);
        }
        else
        {
            await SetValue(null);
        }

        await OnClearClick.InvokeAsync();
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

        var value = new ShiftEntitySelectDTO { Text = text };
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

    private async Task UpdateInitialValue()
    {
        var values = MultiSelect
            ? SelectedValues
            : Value != null ? [Value] : [];

        var itemsToLoad = values.Where(x => x.Value != null && x.Text == null);

        if (!itemsToLoad.Any())
        {
            return;
        }

        var ids = itemsToLoad.Select(x => x.Value).Distinct();

        IsIntitialValueLoading = true;
        StateHasChanged();

        try
        {
            var builder = OData
                .CreateNewQuery<TEntitySet>(EntitySet, GetPath());

            var url = builder.WhereQuery(x => ids.Contains(x.ID))
                    .ToString();

            using var res = await Http.GetAsync(url, FetchToken.Token);
            var items = await ParseEntityResponse(res);

            ShiftEntitySelectDTO? itemFound = null;
            foreach (var item in items)
            {
                var text = GetProperty(item, _DataTextField);
                var value = GetProperty(item, _DataValueField);

                itemFound = itemsToLoad.FirstOrDefault(x => x.Value == value);

                if (itemFound != null)
                {
                    itemFound.Text = text;
                    itemFound.Data = item;
                }

            }

            if (MultiSelect)
            {
                await SelectedValuesChanged.InvokeAsync(SelectedValues);
            }
            else
            {
                await SetValue(itemFound);
            }
        }
        catch (Exception e)
        {
            IsIntitialValueLoading = false;
            MessageService.Error("Failed to load initial data", "Failed to retrieve data", e.Message);
        }

        IsIntitialValueLoading = false;
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

        if (index >= 0 && index < SelectedValues.Count)
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
        if (index >= 0 && index < SelectedValues.Count)
        {
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
        if (!MultiSelect && SelectedValues.Count > 0)
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

        if (direction < 0 && SelectedValuesIndex > 0)
        {
            index = int.Min(SelectedValuesIndex, SelectedValues.Count) - 1;
        }
        else if (direction > 0 && SelectedValuesIndex < SelectedValues.Count)
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
        if (string.IsNullOrWhiteSpace(AdornmentIcon))
        {
            return Value == null || MultiSelect
                ? Icons.Material.Filled.AddCircle
                : Icons.Material.Filled.RemoveRedEye;
        }

        return AdornmentIcon;
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
        FetchToken.Dispose();
    }
}
