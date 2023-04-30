using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using ShiftSoftware.ShiftBlazor.Services;
using ShiftSoftware.ShiftBlazor.Enums;

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

        ///// <summary>
        /////     Name of the column to filter when user types in the input field.
        ///// </summary>
        //[Parameter]
        //public string FilterFieldName { get; set; } = "Name";

        [CascadingParameter]
        public FormModes? Mode { get; set; }

        [CascadingParameter]
        public FormTasks? TaskInProgress { get; set; }

        internal IQueryable<T> QueryBuilder { get; set; } = default!;

        [Parameter]
        public Func<string, Expression<Func<T, bool>>> Where { get; set; }

        public ShiftAutocomplete ()
        {
            OnlyValidateIfDirty = true;
            ResetValueOnEmptyText = true;
            Variant = Variant.Text;
        }

        protected override void OnInitialized()
        {
            if (EntitySet == null)
            {
                throw new ArgumentNullException(nameof(EntitySet));
            }

            QueryBuilder = OData.CreateQuery<T>(EntitySet);

            SearchFunc = Search;
        }

        internal async Task<IEnumerable<T>> Search(string val)
        {
            var url = GetODataUrl(val);
            return await GetODataResult(url);
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

        internal async Task<List<T>> GetODataResult(string url)
        {
            try
            {
                var text = await Http.GetStringAsync(url);
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
