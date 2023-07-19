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

namespace ShiftSoftware.ShiftBlazor.Components
{
    public partial class ShiftAutocomplete<T, TEntitySet> : MudAutocomplete<T>
        where T : ShiftEntitySelectDTO, new()
        where TEntitySet : ShiftEntityDTOBase
    {
        [Inject] private ODataQuery OData { get; set; } = default!;
        [Inject] private HttpClient Http { get; set; } = default!;

        /// <summary>
        ///     The OData EntitySet name.
        /// </summary>
        [Parameter]
        [EditorRequired]
        public string? EntitySet { get; set; }

        [CascadingParameter]
        public FormModes? Mode { get; set; }

        [CascadingParameter]
        public FormTasks? TaskInProgress { get; set; }

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

        internal DataServiceQuery<TEntitySet> QueryBuilder { get; set; } = default!;
        internal string LastTypedValue = "";
        internal List<TEntitySet> Items = new();

        public ShiftAutocomplete ()
        {
            OnlyValidateIfDirty = true;
            ResetValueOnEmptyText = true;
            Strict = false;
            Clearable = true;
            Variant = Variant.Text;
            ShowProgressIndicator = true;
        }

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

            base.OnInitialized();
        }

        internal async Task UpdateInitialValue()
        {
            if (Value != null && !string.IsNullOrWhiteSpace(Value.Value) && string.IsNullOrWhiteSpace(Value.Text))
            {
                Placeholder = "Loading...";
                var url = QueryBuilder.Where(x => x.ID == Value.Value).Take(1);
                var value = await GetODataResult(url.ToString()!);
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

            if (Where != null)
            {
                url = QueryBuilder
                    .Where(Where(q));
            }
            else if (!string.IsNullOrWhiteSpace(q))
            {
                url = QueryBuilder
                    .AddQueryOption("$filter", $"contains(tolower({DataTextField}),'{q}')");
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
