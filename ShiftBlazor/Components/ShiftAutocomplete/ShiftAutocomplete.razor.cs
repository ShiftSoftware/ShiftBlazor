using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using ShiftSoftware.ShiftBlazor.Services;
using ShiftSoftware.ShiftBlazor.Enums;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using System.Net.Http.Json;
using Microsoft.OData.Client;
using Microsoft.AspNetCore.Components.Web;
using ShiftSoftware.ShiftBlazor.Utils;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftBlazor.Interfaces;
using System;
using ShiftSoftware.ShiftBlazor.Extensions;

namespace ShiftSoftware.ShiftBlazor.Components
{
    public partial class ShiftAutocomplete<TEntitySet> : MudAutocomplete<ShiftEntitySelectDTO>, IODataComponent, IFilterableComponent
        where TEntitySet : ShiftEntityDTOBase
    {
        [Inject] private ODataQuery OData { get; set; } = default!;
        [Inject] private HttpClient Http { get; set; } = default!;
        [Inject] private MessageService Message { get; set; } = default!;
        [Inject] private SettingManager SettingManager { get; set; } = default!;

        [Parameter]
        [EditorRequired]
        public string EntitySet { get; set; }

        [Parameter]
        public string? BaseUrl { get; set; }

        [Parameter]
        public string? BaseUrlKey { get; set; }
        [Parameter]
        public string ODataPath { get; set; } = "odata";

        [Parameter]
        public string? DataValueField { get; set; }
        [Parameter]
        public string? DataTextField { get; set; }

        [Parameter]
        public Func<string, List<string>>? FilterFunc { get; set; }

        [Parameter]
        public bool Tags { get; set; }

        [Parameter]
        public RenderFragment<TEntitySet> ShiftItemTemplate { get; set; }

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

        internal string LastTypedValue = "";
        internal List<TEntitySet> Items = new();

        private string? _Placeholder = null;
        private string? _Class = null;
        private EventCallback<ShiftEntitySelectDTO>? _ValueChanged = null;
        private const string MultiSelectClassName = "multi-select";
        internal string _DataValueField = string.Empty;
        internal string _DataTextField = string.Empty;
        private ODataFilterGenerator Filters = new ODataFilterGenerator(true);
        private string PreviousFilters = string.Empty;
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

            ToStringFunc ??= (e) => e?.Text ?? "";
            SearchFunc = Search;

            _ = UpdateInitialValue();

            base.OnInitialized();
        }

        protected override void OnParametersSet()
        {
            if (MultiSelect)
            {
                OnKeyDown = new EventCallback<KeyboardEventArgs>(this, HandleKeyDown);
                ValueChanged = new EventCallback<ShiftEntitySelectDTO>(this, async delegate (ShiftEntitySelectDTO value)
                {
                    await HandleValueChanged();
                    if (_ValueChanged?.HasDelegate == true)
                    {
                        await _ValueChanged.Value.InvokeAsync(value);
                    }
                });
            }


            if (Filter != null)
            {
                var filter = new ODataFilterGenerator(true, Id);
                Filter.Invoke(filter);
                Filters.Add(filter);
            }

            if (Filters.ToString() != PreviousFilters)
            {
                PreviousFilters = Filters.ToString();
                ResetValueAsync();
            }
        }

        public void AddFilter(Guid id, string field, ODataOperator op = ODataOperator.Equal, object? value = null)
        {
            Filters.Add(field, op, value, id);
        }

        private async Task HandleKeyDown(KeyboardEventArgs args)
        {
            if (args.Code == "Backspace" && string.IsNullOrWhiteSpace(Text) && SelectedValues.Count > 0)
            {
                SelectedValues.Remove(SelectedValues.Last());
                await SelectedValuesChanged.InvokeAsync(SelectedValues);
            }
        }

        private async Task HandleValueChanged()
        {
            if (Value == null)
            {
                return;
            }

            var findMatch = SelectedValues.FirstOrDefault(x => x.Value == Value.Value);
            if (findMatch == null)
            {
                SelectedValues.Add(Value);
            }
            else
            {
                SelectedValues.Remove(findMatch);
            }
            Value = new();
            await Clear();
            await SelectedValuesChanged.InvokeAsync(SelectedValues);
        }

        internal async Task UpdateInitialValue()
        {
            if (Value != null && !string.IsNullOrWhiteSpace(Value.Value) && string.IsNullOrWhiteSpace(Value.Text))
            {
                Placeholder = "Loading...";
                var url = "";

                try
                {
                    string? baseUrl = BaseUrl ?? SettingManager.Configuration.ExternalAddresses.TryGet(BaseUrlKey ?? "");
                    
                    baseUrl = baseUrl?.AddUrlPath(this.ODataPath);

                    url = OData.CreateNewQuery<TEntitySet>(EntitySet, baseUrl)
                            .AddQueryOption("$select", $"{_DataValueField},{_DataTextField}")
                            .WhereQuery(x => 1 == 1 && x.ID == Value.Value)
                            .Take(1)
                            .ToString();
                }
                catch (Exception e)
                {
                    Message.Error("Failed to retrieve data", "Failed to retrieve data", e.Message);
                }

                var value = await GetODataResult(url!);
                var text = value.First().Text;

                if (string.IsNullOrWhiteSpace(text))
                {
                    return;
                }

                Value = new ShiftEntitySelectDTO
                {
                    Value = Value.Value,
                    Text = text,
                };

                Placeholder = "";
                await ValueChanged.InvokeAsync(Value);
            }
        }

        internal async Task<IEnumerable<ShiftEntitySelectDTO>> Search(string val, CancellationToken token)
        {
            LastTypedValue = val;
            var url = GetODataUrl(val);
            return await GetODataResult(url, token);
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
                }).Where(x => !string.IsNullOrWhiteSpace(x.Value)).ToList();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public RenderFragment RenderItem(string id)
        {
            var item = Items.First(x => x.ID == id);

            return ShiftItemTemplate(item);
        }
    }
}
