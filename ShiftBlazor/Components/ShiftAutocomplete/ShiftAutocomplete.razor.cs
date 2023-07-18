using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using ShiftSoftware.ShiftBlazor.Services;
using ShiftSoftware.ShiftBlazor.Enums;
using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace ShiftSoftware.ShiftBlazor.Components
{
    public partial class ShiftAutocomplete<T, TEntitySet> : MudAutocomplete<T>
        where T : ShiftEntitySelectDTO, new()
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

        internal IQueryable<TEntitySet> QueryBuilder { get; set; } = default!;
        internal string LastTypedValue = "";

        public ShiftAutocomplete ()
        {
            OnlyValidateIfDirty = true;
            ResetValueOnEmptyText = true;
            Strict = false;
            Clearable = true;
            Variant = Variant.Text;
        }

        protected override void OnInitialized()
        {
            if (EntitySet == null)
            {
                throw new ArgumentNullException(nameof(EntitySet));
            }

            QueryBuilder = OData.CreateQuery<TEntitySet>(EntitySet);

            SearchFuncWithCancel = Search;
        }

        protected override void OnParametersSet()
        {
            if (ToStringFunc == null)
            {
                ToStringFunc = (e) => e?.Text;
            }
        }

        internal async Task<IEnumerable<T>> Search(string val, CancellationToken token)
        {
            LastTypedValue = val;
            var url = GetODataUrl(val);
            return await GetODataResult(url, token);
        }

        internal string GetODataUrl(string q)
        {
            var url = QueryBuilder.AsQueryable();

            if (Where != null)
            {
                url = QueryBuilder
                    .Where(Where(q ?? ""));
            }

            return url.Take(100).ToString()!;
        }

        internal async Task<List<T>> GetODataResult(string url, CancellationToken token)
        {
            try
            {
                var text = await Http.GetStringAsync(url, token);
                var json = JsonNode.Parse(text);

                var odataResult = json?["value"].Deserialize<List<TEntitySet>>() ?? new List<TEntitySet>();

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
    }
}
