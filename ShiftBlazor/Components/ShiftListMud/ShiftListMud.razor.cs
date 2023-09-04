using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.OData.Client;
using MudBlazor;
using ShiftSoftware.ShiftBlazor.Enums;
using ShiftSoftware.ShiftBlazor.Services;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using System.Net.Http.Json;
using System.Reflection;
using static MudBlazor.CategoryTypes;

namespace ShiftSoftware.ShiftBlazor.Components
{
    public partial class ShiftListMud<T> where T : ShiftEntityDTOBase, new()
    {
        [Inject] ODataQuery OData { get; set; } = default!;
        [Inject] HttpClient HttpClient { get; set; } = default!;
        [Inject] ShiftModal ShiftModal { get; set; } = default!;
        [Inject] IStringLocalizer<Resources.Components.ShiftList> Loc { get; set; } = default!;


        /// <summary>
        ///     The current fetched items, this will be fetched from the OData API endpoint that is provided in the Action paramater.
        /// </summary>
        [Parameter]
        public List<T>? Values { get; set; }

        /// <summary>
        ///     An event triggered when the state of Values has changed.
        /// </summary>
        [Parameter]
        public EventCallback<T> ValuesChanged { get; set; }

        /// <summary>
        ///     The URL endpoint that processes the CRUD operations.
        /// </summary>
        [Parameter]
        public string? Action { get; set; }

        [Parameter]
        public string? EntitySet { get; set; }

        /// <summary>
        ///     A list of columns names to hide them in the UI.
        /// </summary>
        [Parameter]
        public List<string> ExcludedColumns { get; set; } = new();

        /// <summary>
        ///     The type of the component to open when clicking on Add or the Action button.
        ///     If empty, Add and Action button column will be hidden.
        /// </summary>
        [Parameter]
        public Type? ComponentType { get; set; }

        /// <summary>
        ///     To pass additional parameters to the ShiftFormContainer componenet.
        /// </summary>
        [Parameter]
        public Dictionary<string, string>? AddDialogParameters { get; set; }

        /// <summary>
        ///     Enable select
        /// </summary>
        [Parameter]
        public bool EnableSelection { get; set; }

        internal DataServiceQuery<T> QueryBuilder { get; set; } = default!;
        internal List<ListColumn> GeneratedColumns = new();
        internal readonly List<string> DefaultExcludedColumns = new() { nameof(ShiftEntityDTOBase.ID), nameof(ShiftEntityDTOBase.IsDeleted), "Revisions" };

        public List<FilterDefinition<Element>> FilterDefinition => GeneratedColumns.Select(x => new FilterDefinition<Element>()).ToList();
        public record Element(string Name);
        protected override void OnInitialized()
        {
            GenerateColumns();
        }

        protected override async Task OnInitializedAsync()
        {
            QueryBuilder = OData.CreateQuery<T>(EntitySet);
            var url = QueryBuilder.ToString();
            var res = await HttpClient.GetFromJsonAsync<ODataDTO<T>>(url);
            Elements = res?.Value;
        }

        internal void GenerateColumns()
        {
            var properties = typeof(T)
                .GetProperties()
                .Where(x => !DefaultExcludedColumns.Contains(x.Name, StringComparer.CurrentCultureIgnoreCase))
                .Where(x => !ExcludedColumns.Contains(x.Name, StringComparer.CurrentCultureIgnoreCase));

            var complexColumns = new List<string>();

            foreach (var prop in properties)
            {
                var column = new ListColumn();

                column.Label = prop.Name;
                column.Field = GetFieldName(prop);

                if (!IsSystemType(prop.PropertyType) && prop.PropertyType.IsClass)
                {
                    complexColumns.Add(prop.Name);
                    column.IsComplex = true;
                }

                GeneratedColumns.Add(column);
            }

            //GridQuery?.Expand(complexColumns);
        }

        internal string GetFieldName(PropertyInfo property)
        {
            var field = property.Name;
            // try getting the complex field name when field is complex type
            if (!IsSystemType(property.PropertyType) && property.PropertyType.IsClass)
            {
                var childProp = property
                    .PropertyType
                    .GetProperties()
                    .FirstOrDefault(x => x.PropertyType.Name == "String" && x.Name != nameof(ShiftEntityDTOBase.ID));

                if (!string.IsNullOrWhiteSpace(childProp?.Name))
                {
                    field = $"{property.Name}.{childProp.Name}";
                }
            }
            return field;
        }

        internal static bool IsSystemType(Type type)
        {
            return type.Namespace == "System";
        }

        public async Task ViewAddItem(object? key = null)
        {
            if (ComponentType != null)
            {
                var result = await OpenDialog(ComponentType, key, ModalOpenMode.Popup, this.AddDialogParameters);
                //await OnFormClosed.InvokeAsync(result?.Data);
            }
        }

        public async Task<DialogResult?> OpenDialog(Type ComponentType, object? key = null, ModalOpenMode openMode = ModalOpenMode.Popup, Dictionary<string, string>? parameters = null)
        {
            var result = await ShiftModal.Open(ComponentType, key, openMode, parameters);
            if (result != null && result.Canceled != true)
            {
                //await Grid.Refresh();
            }
            return result;
        }
    }
}
