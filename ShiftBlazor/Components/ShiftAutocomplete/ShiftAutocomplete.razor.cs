using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using ShiftSoftware.ShiftBlazor.Services;
using ShiftSoftware.ShiftBlazor.Enums;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using System.Net.Http.Json;
using Microsoft.OData.Client;
using Microsoft.AspNetCore.Components.Web;
using ShiftSoftware.ShiftBlazor.Utils;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftBlazor.Interfaces;
using ShiftSoftware.ShiftBlazor.Extensions;
using Microsoft.JSInterop;
using ShiftSoftware.ShiftEntity.Core.Extensions;

namespace ShiftSoftware.ShiftBlazor.Components
{
    public partial class ShiftAutocomplete<TEntitySet> : MudAutocomplete<ShiftEntitySelectDTO>, IODataComponent, IFilterableComponent
        where TEntitySet : ShiftEntityDTOBase
    {
        [Inject] private ODataQuery OData { get; set; } = default!;
        [Inject] private HttpClient Http { get; set; } = default!;
        [Inject] private MessageService Message { get; set; } = default!;
        [Inject] private SettingManager SettingManager { get; set; } = default!;
        [Inject] IJSRuntime JsRuntime { get; set; } = default!;

        [CascadingParameter]
        public FormModes Mode { get; set; }

        [Parameter]
        [EditorRequired]
        public string EntitySet { get; set; }

        [Parameter]
        public string? BaseUrl { get; set; }

        [Parameter]
        public string? BaseUrlKey { get; set; }
        [Parameter]
        public string ODataPath { get; set; } = "api";

        [Parameter]
        public string? DataValueField { get; set; }
        [Parameter]
        public string? DataTextField { get; set; }

        [Parameter]
        public Func<string, List<string>>? FilterFunc { get; set; }

        [Parameter]
        public bool Tags { get; set; }

        [Parameter]
        public RenderFragment<TEntitySet?> ShiftItemTemplate { get; set; }

        [Parameter]
        public bool MultiSelect { get; set; }

        [Parameter]
        public List<ShiftEntitySelectDTO> SelectedValues { get; set; } = new List<ShiftEntitySelectDTO>();

        [Parameter]
        public EventCallback<List<ShiftEntitySelectDTO>> SelectedValuesChanged { get; set; }

        [Parameter]
        public bool MinResponseContent { get; set; }

        [Parameter]
        public Action<ODataFilterGenerator>? Filter { get; set; }

        [Parameter]
        public RenderFragment? ChildContent { get; set; }

        [Parameter]
        public bool FreeInput { get; set; }

        [Parameter]
        public RenderFragment<ShiftEntitySelectDTO>? SelectedValueTemplate { get; set; }

        internal string LastTypedValue = "";
        internal List<TEntitySet> Items = new();

        private string? _Placeholder = null;
        private string? _Class = null;
        private EventCallback<ShiftEntitySelectDTO>? _ValueChanged = null;
        private const string MultiSelectClassName = "multi-select";
        internal string _DataValueField = string.Empty;
        internal string _DataTextField = string.Empty;
        private ODataFilterGenerator Filters = new ODataFilterGenerator(true);
        private string? PreviousFilters;
        private int DropdownItemCount = 0;
        private bool ShrinkTags = false;
        public Guid Id { get; private set; } = Guid.NewGuid();

        public override Task SetParametersAsync(ParameterView parameters)
        {
            parameters.TryGetValue(nameof(Placeholder), out _Placeholder);
            parameters.TryGetValue(nameof(Class), out _Class);
            parameters.TryGetValue(nameof(ValueChanged), out _ValueChanged);

            MaxItems = 100;
            ResetValueOnEmptyText = true;
            ShowProgressIndicator = true;
            OnlyValidateIfDirty = true;
            Clearable = true;
            Strict = false;
            Variant = Variant.Text;

            if (parameters.TryGetValue(nameof(For), out Expression<Func<ShiftEntitySelectDTO>>? _For))
            {
                Required = FormHelper.IsRequired<ShiftEntitySelectDTO>(_For!);
            }

            return base.SetParametersAsync(parameters);
        }

        protected override void OnInitialized()
        {
            if (string.IsNullOrWhiteSpace(EntitySet))
                throw new ArgumentNullException(nameof(EntitySet));

            var shiftEntityKeyAndNameAttribute = Misc.GetAttribute<TEntitySet, ShiftEntityKeyAndNameAttribute>();
            _DataValueField = DataValueField ?? shiftEntityKeyAndNameAttribute?.Value ?? "";
            _DataTextField = DataTextField ?? shiftEntityKeyAndNameAttribute?.Text ?? "";

            if (string.IsNullOrWhiteSpace(_DataValueField))
                throw new ArgumentNullException(nameof(DataValueField));

            if (string.IsNullOrWhiteSpace(_DataTextField))
                throw new ArgumentNullException(nameof(DataTextField));

            Converter = ValueConverter;
            SearchFunc = Search;
            _ = UpdateInitialValue();

            base.OnInitialized();
        }

        protected override void OnParametersSet()
        {
            ReturnedItemsCountChanged = new EventCallback<int>(this, delegate (int itemCount)
            {
                DropdownItemCount = itemCount;
            });

            OnKeyDown = new EventCallback<KeyboardEventArgs>(this, HandleKeyDown);

            if (FreeInput)
            {
                OnBlur = new EventCallback<FocusEventArgs>(this, async delegate ()
                {
                    if (Value?.Text != Text)
                        await SelectFreeInputValue();
                });
            }

            if (MultiSelect)
            {
                ValueChanged = new EventCallback<ShiftEntitySelectDTO>(this, HandleValueChanged);
            }

            if (Filter != null)
            {
                var filter = new ODataFilterGenerator(true, Id);
                Filter.Invoke(filter);
                Filters.Add(filter);
            }

            if (Filters.ToString() != PreviousFilters)
            {
                // We need to check null here otherwise the value will be reset on initialized
                if (PreviousFilters != null && Mode >= FormModes.Edit)
                    ResetValueAsync();

                PreviousFilters = Filters.ToString();
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (MultiSelect && SelectedValueTemplate != null)
            {
                // Add text indent to the input based on the width of the tags.
                // Then return the size of the tags compared to the parent element.
                var size = await JsRuntime.InvokeAsync<float>("fixAutocompleteIndent", $"Input-{Id}");

                var preVal = ShrinkTags;
                ShrinkTags = size > 0.8;

                // We need to force rerender the component to shrink the tags
                // Otherwise it will wait until next render
                if (preVal != ShrinkTags)
                {
                    StateHasChanged();
                }
            }
        }

        public void AddFilter(Guid id, string field, ODataOperator op = ODataOperator.Equal, object? value = null)
        {
            Filters.Add(field, op, value, id);
        }

        private async Task HandleKeyDown(KeyboardEventArgs args)
        {
            if (args.Code == "Enter" && FreeInput)
            {
                // Add FreeInput value only when there are no results in search (dropdown items).
                if (DropdownItemCount == 0)
                {
                    await SelectFreeInputValue();
                }
            }

            if (MultiSelect && args.Code == "Backspace" && string.IsNullOrWhiteSpace(Text) && SelectedValues.Count > 0)
            {
                SelectedValues.Remove(SelectedValues.Last());
                await SelectedValuesChanged.InvokeAsync(SelectedValues);
            }
        }

        private async Task HandleValueChanged(ShiftEntitySelectDTO value)
        {
            if (value == null || (value.Value == null && string.IsNullOrWhiteSpace(value.Text)))
            {
                return;
            }

            var findMatch = SelectedValues.FirstOrDefault(x =>
            {
                if (value.Value == null)
                    return x.Text == value.Text;
                return x.Value == value.Value;
            });

            if (findMatch == null)
            {
                SelectedValues.Add(value);
            }
            else
            {
                SelectedValues.Remove(findMatch);
            }

            await ClearAsync();
            await SelectedValuesChanged.InvokeAsync(SelectedValues);
        }

        internal async Task UpdateInitialValue()
        {
            var values = SelectedValues.ToList();

            if (Value != null)
                values.Add(Value);

            var valuesToLoad = new List<string>();

            foreach (var value in values)
            {
                if (value != null && !string.IsNullOrWhiteSpace(value.Value) && string.IsNullOrWhiteSpace(value.Text))
                {
                    valuesToLoad.Add(value.Value);
                }
            }

            if (valuesToLoad.Count > 0)
            {
                Placeholder = "Loading...";
                var url = "";

                try
                {
                    string? baseUrl = BaseUrl ?? SettingManager.Configuration.ExternalAddresses.TryGet(BaseUrlKey ?? "");

                    baseUrl = baseUrl?.AddUrlPath(this.ODataPath);

                    url = OData.CreateNewQuery<TEntitySet>(EntitySet, baseUrl)
                            //.AddQueryOption("$select", $"{_DataValueField},{_DataTextField}")
                            .WhereQuery(x => valuesToLoad.AsEnumerable().Contains(x.ID))
                            //.Take(1)
                            .ToString();
                }
                catch (Exception e)
                {
                    Message.Error("Failed to retrieve data", "Failed to retrieve data", e.Message);
                }

                var loadedValues = await GetODataResult(url!);

                Placeholder = "";

                if (MultiSelect)
                {
                    foreach (var value in loadedValues)
                    {
                        var text = value.Text;
                        var data = value.Data;

                        var match = SelectedValues.Where(x => x.Value == value.Value).FirstOrDefault();

                        if (match is not null)
                        {
                            match.Text = text;
                            match.Data = data;
                        }
                    }

                    await SelectedValuesChanged.InvokeAsync(SelectedValues);
                }
                else
                {
                    var firstValue = loadedValues.First();

                    var text = firstValue.Text;
                    var data = firstValue.Data;

                    Console.WriteLine("value is: " + firstValue);

                    if (string.IsNullOrWhiteSpace(text))
                    {
                        await ValueChanged.InvokeAsync(Value);
                        return;
                    }

                    Value = new ShiftEntitySelectDTO
                    {
                        Value = Value.Value,
                        Text = text,
                        Data = data,
                    };

                    await ValueChanged.InvokeAsync(Value);
                }
            }
        }

        internal async Task<IEnumerable<ShiftEntitySelectDTO>> Search(string val, CancellationToken token)
        {
            LastTypedValue = val;
            var url = GetODataUrl(val);
            var DropdownValues = await GetODataResult(url, token);

            if (MultiSelect && SelectedValues != null)
            {
                DropdownValues.AddRange(SelectedValues.Where(x => x.Value == null && (val == null || x.Text?.Contains(val) == true)));
            }

            return DropdownValues;
        }

        internal string GetODataUrl(string q = "")
        {
            string? url = BaseUrl ?? SettingManager.Configuration.ExternalAddresses.TryGet(BaseUrlKey ?? "");

            url = url?.AddUrlPath(this.ODataPath);

            var builder = OData
                .CreateNewQuery<TEntitySet>(EntitySet, url)
                .AddQueryOptionIf("$select", $"{_DataValueField},{_DataTextField}", MinResponseContent);

            var filters = new List<string>();

            if (Filters.Count > 0)
            {
                filters.Add(Filters.ToString());
            }

            if (!string.IsNullOrWhiteSpace(q))
            {
                if (FilterFunc == null)
                {
                    filters.Add($"contains({_DataTextField},'{q}')");
                }
                else
                {
                    filters.Add(string.Join(" and ", FilterFunc.Invoke(q)));
                }
            }

            if (filters.Count > 0)
            {
                builder = builder.AddQueryOption("$filter", string.Join(" and ", filters));
            }

            return builder.Take(MaxItems ?? 100).ToString()!;
        }

        internal async Task<List<ShiftEntitySelectDTO>> GetODataResult(string url, CancellationToken token = default)
        {
            try
            {
                var res = await Http.GetFromJsonAsync<ODataDTO<TEntitySet>>(url, token);

                if (res == null)
                {
                    throw new Exception("Could not get OData response");
                }

                var odataResult = res.Value;
                Items = odataResult;

                var odataResultType = typeof(TEntitySet);

                return odataResult.Select(x => new ShiftEntitySelectDTO
                {
                    Value = odataResultType.GetProperty(_DataValueField)?.GetValue(x)?.ToString()!,
                    Text = odataResultType.GetProperty(_DataTextField)?.GetValue(x)?.ToString(),
                    Data = x,
                }).Where(x => !string.IsNullOrWhiteSpace(x.Value)).ToList();
            }
            catch (Exception)
            {
                throw;
            }
        }

        private async Task SelectFreeInputValue()
        {
            if (string.IsNullOrWhiteSpace(Text))
            {
                return;
            }

            var value = new ShiftEntitySelectDTO { Text = Text };

            if (MultiSelect)
            {
                await HandleValueChanged(value);
            }
            else
            {
                Value = value;
                await ValueChanged.InvokeAsync(value);
            }
        }

        public RenderFragment RenderItem(string id)
        {
            return ShiftItemTemplate(Items.FirstOrDefault(x => x.ID == id));
        }

        private readonly Converter<ShiftEntitySelectDTO> ValueConverter = new()
        {
            SetFunc = value => value?.Text,
            GetFunc = text => new ShiftEntitySelectDTO() { Text = text },
        };
    }
}
