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
        [Inject] private ShiftModal ShiftModal { get; set; } = default!;

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

        [Parameter]
        public Type? QuickAddComponentType { get; set; }
        [Parameter]
        public string? QuickAddParameterName { get; set; }

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

        public override Task SetParametersAsync(ParameterView parameters)
        {
            Type? type;
            parameters.TryGetValue(nameof(QuickAddComponentType), out type);

            if (type == null)
            {
                return base.SetParametersAsync(parameters);
            }

            string? adornmentIcon;
            string? adornmentAriaLabel;
            EventCallback<MouseEventArgs>? onAdornmentClick;

            parameters.TryGetValue(nameof(AdornmentIcon), out adornmentIcon);
            parameters.TryGetValue(nameof(AdornmentAriaLabel), out adornmentAriaLabel);
            parameters.TryGetValue(nameof(OnAdornmentClick), out onAdornmentClick);

            if (adornmentIcon == null)
            {
                AdornmentIcon = Icons.Material.Filled.AddCircle;
            }
            if (adornmentAriaLabel == null)
            {
                AdornmentAriaLabel = "Add new item";
            }
            if (onAdornmentClick == null)
            {
                OnAdornmentClick = new EventCallback<MouseEventArgs>(this, AddNewItem);
            }

            return base.SetParametersAsync(parameters);
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

        internal async Task AddNewItem(MouseEventArgs args)
        {

            if (QuickAddComponentType == null)
            {
                return;
            }

            Dictionary<string, string>? parameters = null;

            if (QuickAddParameterName != null)
            {
                parameters = new Dictionary<string, string>
                {
                    {QuickAddParameterName, LastTypedValue }
                };
            }

            var result = await ShiftModal.Open(QuickAddComponentType, null, ModalOpenMode.Popup, parameters);
            if (result?.Canceled != true)
            {
                // selected the new item
            }
        }

    }
}
