using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using ShiftSoftware.ShiftBlazor.Services;
using ShiftSoftware.ShiftBlazor.Enums;
using Microsoft.AspNetCore.Components.Web;

namespace ShiftSoftware.ShiftBlazor.Components
{
    public partial class ShiftAutocomplete<T> : MudAutocomplete<T>
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
        public Func<string, Expression<Func<T, bool>>> Where { get; set; }

        internal IQueryable<T> QueryBuilder { get; set; } = default!;
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

            QueryBuilder = OData.CreateQuery<T>(EntitySet);

            SearchFuncWithCancel = Search;
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

            //if (!string.IsNullOrWhiteSpace(q))
            {
                url = QueryBuilder
                    //.AddQueryOption("$filter", $"contains(tolower({FilterFieldName}),'{q}')")
                    .Where(Where(q ?? ""))
                    .Take(100);
            }
            return url.ToString()!;
        }

        internal async Task<List<T>> GetODataResult(string url, CancellationToken token)
        {
            try
            {
                var text = await Http.GetStringAsync(url, token);
                var json = JsonNode.Parse(text);
                return json?["value"].Deserialize<List<T>>() ?? new List<T>();
            }
            catch (Exception)
            {
                throw;
            }
        }

    }
}
