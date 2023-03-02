using Microsoft.AspNetCore.Components;
using ShiftSoftware.ShiftBlazor.Services;
using ShiftSoftware.ShiftBlazor.Utils;
using System.Text.Json;
using Microsoft.OData.Client;
using MudBlazor;

namespace ShiftSoftware.ShiftBlazor.Components
{
    public partial class ShiftAutocomplete<T> : MudAutocomplete<T>
    {
        [Inject] ODataQuery OData { get; set; } = default!;
        [Inject] HttpClient Http { get; set; } = default!;

        /// <summary>
        /// The OData EntitySet name.
        /// </summary>
        [Parameter, EditorRequired]
        public string? EntitySet { get; set; }

        /// <summary>
        /// Name of the column to filter when user types in the input field.
        /// </summary>
        [Parameter]
        public string FilterFieldName { get; set; } = "Name";
        
        [CascadingParameter]
        public Form.Modes? Mode { get; set; }

        [CascadingParameter]
        public Form.Tasks? TaskInProgress { get; set; }

        internal DataServiceQuery<T> QueryBuilder { get; set; } = default!;

        public ShiftAutocomplete ()
        {
            OnlyValidateIfDirty = true;
            ResetValueOnEmptyText = true;
            Variant = Variant.Text;
        }

        //public override async Task SetParametersAsync(ParameterView parameters)
        //{
        //    Func<T, string> value = default;

        //    if (parameters.TryGetValue(nameof(ToStringFunc), out value))
        //    {

        //    }
        //    else
        //    {
        //        var prop = typeof(T).GetProperty(FilterFieldName);
        //        if (prop != null)
        //        {
        //            ToStringFunc = e => e == null ? null : $"{prop.GetValue(Value)}";
        //        }
        //    }

        //    await base.SetParametersAsync(parameters);
        //}

        protected override void OnInitialized()
        {
            if (EntitySet == null)
            {
                throw new Exception("EntitySet is required");
            }

            QueryBuilder = OData.CreateQuery<T>(EntitySet);

            SearchFunc = Search;
        }

        protected override void OnAfterRender(bool firstRender)
        {
            ReadOnly = Mode == Form.Modes.View;
            Disabled = TaskInProgress != null && TaskInProgress != Form.Tasks.None;
            base.OnAfterRender(firstRender);
        }

        internal async Task<IEnumerable<T>> Search(string val)
        {
            var url = GetODataUrl(val);
            return await GetODataResult(url);
        }

        internal string GetODataUrl(string q)
        {
            var url = QueryBuilder.AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                url = QueryBuilder
                    .AddQueryOption("$filter", $"contains(tolower({FilterFieldName}),'{q}')")
                    .Take(100);
            }
            return url.ToString()!;
        }

        internal async Task<List<T>> GetODataResult(string url)
        {
            try
            {
                var text = await Http.GetStringAsync(url);
                var json = System.Text.Json.Nodes.JsonNode.Parse(text);
                return json?["value"].Deserialize<List<T>>() ?? new List<T>();
            }
            catch (Exception)
            {
                throw;
            }

        }
    }
}
