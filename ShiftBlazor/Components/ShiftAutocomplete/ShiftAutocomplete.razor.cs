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

namespace ShiftSoftware.ShiftBlazor.Components
{
    public partial class ShiftAutocomplete<T, TEntitySet> : MudAutocomplete<T>
        where T : ShiftEntitySelectDTO, new()
        where TEntitySet : ShiftEntityDTOBase
    {
        [Inject] private ODataQuery OData { get; set; } = default!;
        [Inject] private HttpClient Http { get; set; } = default!;
        [Inject] private MessageService Message { get; set; } = default!;

        /// <summary>
        ///     The OData EntitySet name.
        /// </summary>
        [Parameter]
        [EditorRequired]
        public string? EntitySet { get; set; }

        [Parameter]
        public Func<string, Expression<Func<TEntitySet, bool>>>? Where { get; set; }

        [Parameter]
        public bool Tags { get; set; }

        [Parameter, EditorRequired]
        public string DataValueField { get; set; }

        [Parameter, EditorRequired]
        public string DataTextField { get; set; }

        [Parameter]
        public RenderFragment<TEntitySet> ShiftItemTemplate { get; set; }

        [Parameter]
        public bool MultiSelect { get; set; }

        [Parameter]
        public List<T> SelectedValues { get; set; } = new List<T>();

        [Parameter]
        public EventCallback<List<T>> SelectedValuesChanged { get; set; }

        internal DataServiceQuery<TEntitySet> QueryBuilder { get; set; } = default!;
        internal string LastTypedValue = "";
        internal List<TEntitySet> Items = new();

        private string? _Placeholder = null;
        private string? _Class = null;
        private EventCallback<T>? _ValueChanged = null;
        private string MultiSelectClassName = "multi-select";

        protected override void OnInitialized()
        {
            if (EntitySet == null)
            {
                throw new ArgumentNullException(nameof(EntitySet));
            }

            if (ToStringFunc == null)
            {
                ToStringFunc = (e) => e?.Text ?? "";
            }

            QueryBuilder = OData.CreateQuery<TEntitySet>(EntitySet);

            SearchFuncWithCancel = Search;

            _ = UpdateInitialValue();

            if (MultiSelect)
            {
                if (_ValueChanged != null)
                {
                    throw new Exception($"{nameof(ValueChanged)} parameter cannot have a value when {nameof(MultiSelect)} is true");
                }

                OnKeyDown = new EventCallback<KeyboardEventArgs>(this, HandleKeyDown);

                ValueChanged = new EventCallback<T>(this, async () =>
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
                });
            }

            base.OnInitialized();
        }

        public override Task SetParametersAsync(ParameterView parameters)
        {
            parameters.TryGetValue(nameof(Placeholder), out _Placeholder);
            parameters.TryGetValue(nameof(Class), out _Class);
            parameters.TryGetValue(nameof(ValueChanged), out _ValueChanged);

            ResetValueOnEmptyText = true;
            ShowProgressIndicator = true;
            OnlyValidateIfDirty = true;
            Clearable = true;
            Strict = false;
            Variant = Variant.Text;

            if (parameters.TryGetValue(nameof(For), out Expression<Func<T>>? _For))
            {
                Required = FormHelper.IsRequired<T>(_For!);
            }

            return base.SetParametersAsync(parameters);
        }

        private async Task HandleKeyDown(KeyboardEventArgs args)
        {
            if (args.Code == "Backspace" && string.IsNullOrWhiteSpace(Text) && SelectedValues.Count > 0)
            {
                SelectedValues.Remove(SelectedValues.Last());
                await SelectedValuesChanged.InvokeAsync(SelectedValues);
            }
        }

        internal async Task UpdateInitialValue()
        {
            if (Value != null && !string.IsNullOrWhiteSpace(Value.Value) && string.IsNullOrWhiteSpace(Value.Text))
            {
                Placeholder = "Loading...";
                var url = "";

                try
                {
                    url = QueryBuilder.Where(x => 1 == 1 && x.ID == Value.Value).Take(1).ToString();
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

                Value = new T
                {
                    Value = Value.Value,
                    Text = text,
                };

                Placeholder = "";
                await ValueChanged.InvokeAsync(Value);
            }
        }

        internal async Task<IEnumerable<T>> Search(string val, CancellationToken token)
        {
            LastTypedValue = val;
            var url = GetODataUrl(val);
            return await GetODataResult(url, token);
        }

        internal string GetODataUrl(string q = "")
        {
            var url = QueryBuilder.AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                if (Where != null)
                {
                    url = QueryBuilder
                        .Where(Where(q));
                }
                else
                {
                    url = QueryBuilder
                        .AddQueryOption("$filter", $"contains({DataTextField},'{q}')");
                }
            }

            return url.Take(100).ToString()!;
        }

        internal async Task<List<T>> GetODataResult(string url, CancellationToken token = default)
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

                return odataResult.Select(x => new T
                {
                    Value = odataResultType.GetProperty(DataValueField)?.GetValue(x)?.ToString()!,
                    Text = odataResultType.GetProperty(DataTextField)?.GetValue(x)?.ToString(),
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
