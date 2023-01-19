using Microsoft.AspNetCore.Components;
using ShiftSoftware.ShiftBlazor.Services;
using ShiftSoftware.ShiftBlazor.Utils;
using System.Text.Json;
using Microsoft.OData.Client;
using MudBlazor;
using System.Data.Common;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components.Forms;
using System.ComponentModel.DataAnnotations;
using static MudBlazor.Colors;

namespace ShiftSoftware.ShiftBlazor.Components
{
    public class ShiftAutocompleteClass<T> : MudAutocomplete<T>
    {
        [Inject] ODataQuery OData { get; set; } = default!;
        [Inject] HttpClient Http { get; set; } = default!;

        [Parameter] public EventCallback<Form.States> StateChanged { get; set; }
        [EditorRequired]
        [Parameter] public string? EntitySet { get; set; }
        [Parameter] public string FilterFieldName { get; set; } = "Name";
        [CascadingParameter] public Form.States? State { get; set; }

        private DataServiceQuery<T> QueryBuilder { get; set; } = default!;

        public ShiftAutocompleteClass ()
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

            base.SearchFunc = Search;
        }

        protected override void OnAfterRender(bool firstRender)
        {
            base.ReadOnly = State == Form.States.View;
            base.Disabled = State == Form.States.Saving;
            base.OnAfterRender(firstRender);
        }

        private async Task<IEnumerable<T>> Search(string val)
        {
            var url = QueryBuilder.AsQueryable();

            if (val != null)
            {
                url = QueryBuilder
                    .AddQueryOption("$filter", $"contains(tolower({FilterFieldName}),'{val}')")
                    .Take(100);
            }

            List<T> Item;

            try
            {
                var text = await Http.GetStringAsync(url.ToString());
                var json = System.Text.Json.Nodes.JsonNode.Parse(text);
                Item = json?["value"].Deserialize<List<T>>() ?? new List<T>();
            }
            catch (Exception)
            {
                throw;
            }

            return Item;
        }
    }
}
